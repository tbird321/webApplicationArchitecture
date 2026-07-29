# Web Application Architecture

This repository contains a full-stack web application platform with:
- an AWS Lambda backend written in .NET
- a React-based frontend
- an optional MCP agent support layer
- deployment automation via AWS SAM and Lambda tools

## Repository structure

- `api/`
  - `WebApplicationArch/` - Lambda backend project, AWS SAM template, and Lambda deployment defaults
    - `content/StaticPageRenderer.cs` - assembles a page into a complete static document; the render source of truth
    - `content/StaticLayoutCss.cs` - loads the shared structural stylesheet (embedded resource)
    - `ApiStaticRenderFunctions.cs` - S3-triggered Lambda that re-renders pages when article HTML changes
  - `mcp/` - MCP Lambda handler (Node.js, deployed as part of the SAM stack)
  - `MySQLConnector/` - data access layer for MySQL, DAO classes, and models
  - `WebApplicationArch.Tests/` - backend unit and integration tests

- `react/`
  - `baseProject/` - React application shell, admin UI, site content runtime, and Amplify integration
  - `reactcomponents/` - reusable React components library
  - `WebTemplates/` - legacy site templates and web configuration assets

- `scripts/` - site setup and deployment automation. See [`scripts/README.md`](scripts/README.md)
  for the site-setup guide: creating a new site, the admin subdomain, clean URLs and 301
  redirects, GoDaddy DNS, and troubleshooting.
  - `sites.json` - the site registry; adding a site starts with one entry here
  - `lib/` - shared PowerShell helpers (AWS wrappers, GoDaddy DNS)
- `infra/`
  - `static/page-layout.css` - structural CSS inlined into every static page (shared with the C# renderer)
  - `cloudfront/public-clean-urls.js` - clean-URL rewrites + legacy `?page=` 301s, with tests
  - `admin/robots.txt` - `Disallow: /` for admin subdomains

- `ApplicationDocumentation.txt` - original manual public-site hosting notes (S3/CloudFront/DNS)
- `STATIC-PRERENDER-ROLLOUT.md` - migration runbook: SPA to pre-rendered static HTML + admin subdomain
- `PLAN.md` - project plan, feature progress, and implementation summary
- `SECURITYISSUES.md` - security issues and remediation checklist

## What this application does

This project supports a website CMS architecture with:
- serverless API endpoints for pages, articles, keywords, layout, topics, and websites
- MySQL-backed content storage configured through AWS Secrets Manager
- a React admin UI with page/article management, publish workflow, and site navigation
- support for publishing static website assets to S3 and CloudFront

---

## Architecture

Each domain runs as **two separate sites**. A site is migrated to this shape one at a time —
see [`scripts/README.md`](scripts/README.md) for the run order and
[`STATIC-PRERENDER-ROLLOUT.md`](STATIC-PRERENDER-ROLLOUT.md) for the plan and per-phase risk.

| | Public site | Admin site |
|---|---|---|
| Host | `www.{domain}` | `admin.{domain}` |
| Content | pre-rendered static HTML at `/slug/` | React SPA, client-rendered |
| Bucket | `www.{domain}` — public, S3 website hosting | `admin-{key}` — private, no website hosting |
| CloudFront origin | S3 **website** endpoint (custom origin, HTTP) | S3 **REST** endpoint via OAC (HTTPS) |
| Auth | none | Cognito |
| Indexing | crawlable — the entire point | `robots.txt` + `X-Robots-Tag: noindex` |
| JavaScript | not required to read the content | required |

### Why

The public sites used to be the SPA: every URL returned the same near-empty shell at
`?page=Slug`, so crawlers, social scrapers, and AI bots saw no content and almost nothing was
indexed. Pre-rendering puts the real article text, title, meta description, canonical, Open
Graph and JSON-LD in the HTML itself.

### Request flow (public site)

```
visitor → CloudFront → [CloudFront Function: public-clean-urls] → S3 www.{domain}
                         ?page=Slug  → 301 /slug/      (legacy links keep their ranking)
                         ?page=Home  → 301 /
                         /slug       → /slug/index.html (rewrite, no redirect hop)
```

S3 static website hosting already resolves `/slug/` → `/slug/index.html` on its own. That is
what makes the migration zero-downtime: after the backfill the new URLs are live in production
while every old `?page=` link still works, so both worlds run at once and the cutover is
proven before anything is switched.

### Content flow (an edit reaching the public site)

```
admin SPA ──Amplify Storage (Cognito creds)──► s3://www-websitecontent/public/websites/{domain}/articles/{path}
MCP tools ──CMS API──────────────────────────►                    │
                                                                  │ S3 ObjectCreated
                                                                  ▼
                                              StaticRenderOnUploadFunction (Lambda)
                                                  1. parse {domain} + {articlePath} from the key
                                                  2. resolve websiteId
                                                  3. gate on STATIC_PUBLISH_SITES
                                                  4. find pages embedding that article
                                                  5. render each → s3://www.{domain}/{slug}/index.html
```

**The trigger is on the bucket, not the API, and that is deliberate.** The admin does not save
article content through this API — `FileProcessing.saveFileData` calls Amplify `Storage.put`
and writes to S3 directly. A save hook on an API endpoint therefore never fires for a normal
edit: the article updates and the public page silently goes stale. Driving the render off
`s3:ObjectCreated` catches every writer — admin, MCP, scripts — without each having to
remember to call something.

`STATIC_PUBLISH_SITES` (a CloudFormation template parameter, comma-separated website ids or
`all`, **empty by default**) controls which sites the renderer may publish for. A site that has
not been cut over is skipped — important because a page named `Home` renders to the bucket
**root**, i.e. straight over that site's live SPA shell.

### How soon an edit appears

| Where | Delay | Why |
|---|---|---|
| Admin site | **immediate** | the SPA fetches the article HTML from S3 on every load |
| Public site | **up to 5 minutes** | static pages are written with `Cache-Control: public, max-age=300`, so CloudFront serves the previous copy until the TTL expires |

The re-render itself takes about a second — the Lambda fires on the S3 write and the new object
is in the bucket immediately. The wait is purely the CloudFront edge cache.

This is deliberate, and the usual confusion is worth naming: **an edit that "did not work" has
almost always already rendered.** Check the S3 object's `LastModified` before assuming the
trigger failed; if S3 is fresh and the browser is not, it is the cache.

**To skip the wait**, use the `invalidate_cache` MCP tool — it clears the current site's
CloudFront cache without opening the console:

```
invalidate_cache                              # whole site; the usual choice
invalidate_cache paths=["/about-us/"]         # just these
```

It backs onto `POST /cache/invalidate?websiteId={id}` (`ApiCacheFunctions.cs`), which resolves
the distribution by its `www.{domain}` alias. CloudFront takes 1–3 minutes to complete one.
Counter-intuitively **`/*` is the cheap option** — invalidation is billed per *path* after 1,000
a month, and `/*` counts as one no matter how much it clears, so listing pages individually is
what runs up a bill.

The endpoint requires the `X-API-Key` header, unlike the rest of this API. It is the one endpoint
that costs money per call, so it is not left open even though its neighbours are
(see `SECURITYISSUES.md`).

Two alternatives considered for making the delay disappear entirely, and not taken:

- have the render Lambda invalidate on every write — instant, but spends the free-tier quota on
  edits that would have appeared by themselves within five minutes
- shorten `max-age` toward 60s — faster, no invalidation cost, a little more origin traffic

Left as-is on purpose: content edits are reviewed in the admin, which is always current, so the
public delay costs nothing — and when it does matter, the manual flush above is one call.

### Rendering

Two renderers must produce identical output:

| Renderer | Used for |
|---|---|
| `api/WebApplicationArch/content/StaticPageRenderer.cs` | the S3 trigger — ongoing edits |
| `scripts/publish-static-pages.ps1` | bulk backfill and local preview |

A page is assembled from the CMS record (name, description, layout, ordered article list),
the article HTML, `header.html`, `sitemenu.json`, and three stylesheets inlined in cascade
order — least specific first:

1. **`infra/static/page-layout.css`** — document baseline, `.articleContents` padding, every
   `.layout-*` grid, and the nav. Shared single source of truth: embedded in the C# assembly,
   read from disk by the PowerShell script.
2. **`BaseStyles.css`** — the per-site brand sheet, read from the public bucket root where the
   SPA build publishes it.
3. **`theme.css`** — ThemeBuilder output. Genuinely absent for several sites.

All three are inlined because the SPA bundle — which is where these rules normally live — is
never loaded by a static page. Content authored with inline styles still wins over all of them.

> Changing `page-layout.css` or a site's `BaseStyles.css` does **not** propagate to
> already-rendered pages; they are baked in at render time. Re-render the site to pick it up.

### Invariants worth knowing

- **Slug parity.** The CloudFront Function's 301 target must equal the path the renderer
  writes to, or every legacy link redirects into a 404. Both use: trim → lowercase →
  whitespace to `-` → **delete** remaining unsafe characters → collapse → trim.
  `infra/cloudfront/public-clean-urls.test.js` locks it in.
- **`Home` lives at the bucket root**, not `/home/`. Applies to the renderer, the nav builder,
  and the 301s.
- **The admin bucket name has no dots.** CloudFront reaches an S3 REST origin over HTTPS and
  the `*.s3.{region}.amazonaws.com` wildcard certificate does not match a multi-label host.
  The name-equals-domain rule only ever applied to S3 website hosting.
- **Admin distributions map 403 *and* 404 → `/index.html` with a 200.** A private bucket
  answers a missing key with 403, so mapping only 404 breaks every SPA deep link.

## Backend deployment

The backend is deployed from `api/WebApplicationArch/`.

Defaults are stored in `api/WebApplicationArch/aws-lambda-tools-defaults.json`:
- `stack-name`: `webapplicationarch`
- `s3-bucket`: `webapparchdeploy`

A SAM template is available at `api/WebApplicationArch/serverless.template`.

### Deploy with SAM

From `api/WebApplicationArch/`:

```powershell
sam build --template-file serverless.template
sam deploy --template-file .aws-sam\build\template.yaml --stack-name webapplicationarch --s3-bucket webapparchdeploy --capabilities CAPABILITY_IAM --parameter-overrides TokenSecret="<your-secret>" TokenIV="<your-16-char-iv>"
```

The repository also includes PowerShell helpers in `scripts/` that build before deploy and verify Lambda env vars.

### Deploy with .NET Lambda tools

From `api/WebApplicationArch/`:

```powershell
dotnet lambda deploy-serverless
```

## Frontend

The main React application lives in `react/baseProject/`.

- run `npm install` inside `react/baseProject/`
- build the app or deploy it to a static site host
- `react/baseProject/.gitignore` excludes generated config and Amplify files

The component library is in `react/reactcomponents/`.

## MCP / agent tools

The MCP server is deployed as a Lambda function (`McpFunction`) in the same SAM stack as the backend API. It exposes tools for pages, articles, collections, and metadata to any MCP-compatible AI agent (Claude Code, Claude Desktop, etc.).

### Deploy

The MCP Lambda is included in the normal backend deploy. You need one additional parameter:

- `MCP_API_KEY` — a long random string the agent sends as `x-api-key` to call the MCP endpoint. Generate and persist it once using the helper script, then the deploy scripts pick it up automatically.

```powershell
# Run once — generates the key and saves it to your user environment permanently
.\scripts\generate-mcp-api-key.ps1

# Then deploy as normal
.\scripts\deploy-lambda-with-tokens.ps1 -ProfileName <your-aws-profile>
```

### Get the API URL after deploy

```powershell
aws cloudformation describe-stacks --stack-name webapplicationarch --profile <your-aws-profile> --region us-west-2 --query "Stacks[0].Outputs[?OutputKey=='ApiURL'].OutputValue" --output text
```

### Configure local MCP server for Claude Code

The repo includes a local stdio MCP server at `api/mcp/src/index.js`. Claude Code reads `.mcp.json` at the repo root and launches it automatically when you open Claude Code from this directory.

**One-time setup — set these as persistent user environment variables:**

```powershell
$profile = '<your-aws-profile>'

# Retrieve MCP_API_KEY from the deployed Lambda
$mcpFn  = (aws cloudformation list-stack-resources --stack-name webapplicationarch --profile $profile --region us-west-2 --query "StackResourceSummaries[?LogicalResourceId=='McpFunction'].PhysicalResourceId" --output text)
$mcpKey = (aws lambda get-function-configuration --function-name $mcpFn --profile $profile --region us-west-2 --query "Environment.Variables.MCP_API_KEY" --output text)
[System.Environment]::SetEnvironmentVariable('MCP_API_KEY', $mcpKey, 'User')
$env:MCP_API_KEY = $mcpKey

# Retrieve the API base URL — fix casing (/Prod -> /prod)
$url = (aws cloudformation describe-stacks --stack-name webapplicationarch --profile $profile --region us-west-2 --query "Stacks[0].Outputs[?OutputKey=='ApiURL'].OutputValue" --output text).TrimEnd('/') -replace '/Prod$', '/prod'
[System.Environment]::SetEnvironmentVariable('LAMBDA_API_BASE_URL', $url, 'User')
$env:LAMBDA_API_BASE_URL = $url
```

**Set WEBSITE_ID to the site you want the agent to manage:**

| Site | URL | WEBSITE_ID |
|------|-----|------------|
| Faith In Crisis | ldsfaithincrisis.com | 1 |
| LDS Doctrines | ldsdoctrines.com | 2 |
| LDS Apologetics | ldsapologetics.com | 5 |

```powershell
[System.Environment]::SetEnvironmentVariable('WEBSITE_ID', '1', 'User')  # ldsfaithincrisis.com
[System.Environment]::SetEnvironmentVariable('WEBSITE_ID', '2', 'User')  # ldsdoctrines.com
[System.Environment]::SetEnvironmentVariable('WEBSITE_ID', '5', 'User')  # ldsapologetics.com
```

**Install dependencies (run once after cloning):**

```powershell
cd api/mcp
npm install
```

Once env vars are set, open Claude Code from the repo root — the `webcms` MCP tools (search_pages, create_page, publish_page, create_article, etc.) will be available automatically.

### Configure in Claude Code / Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "webcms": {
      "type": "http",
      "url": "https://<api-id>.execute-api.us-west-2.amazonaws.com/prod/mcp?websiteId=<your-website-id>",
      "headers": {
        "x-api-key": "<your-mcp-api-key>"
      }
    }
  }
}
```

- Replace `<api-id>` with the value from the stack output above
- Replace `<your-website-id>` with the numeric ID of the site you want the agent to manage
- Replace `<your-mcp-api-key>` with the value you set for `MCP_API_KEY` during deploy
- The `TOKEN_SECRET` is reused as the endpoint key — no extra secret to manage

### Retrieve current token values (if you need to redeploy without rotating)

```powershell
$profile = '<your-aws-profile>'
$fn = (aws cloudformation list-stack-resources --stack-name webapplicationarch --profile $profile --region us-west-2 --query "StackResourceSummaries[?LogicalResourceId=='GetWebsites'].PhysicalResourceId" --output text)
$env:TOKEN_SECRET = (aws lambda get-function-configuration --function-name $fn --profile $profile --region us-west-2 --query "Environment.Variables.TOKEN_SECRET" --output text)
$env:TOKEN_IV     = (aws lambda get-function-configuration --function-name $fn --profile $profile --region us-west-2 --query "Environment.Variables.TOKEN_IV" --output text)
```

Then run `deploy-lambda-no-rotate.ps1`.

## Local development notes

- `api/WebApplicationArch/security/UserSecurity.cs` reads `TOKEN_SECRET` and `TOKEN_IV` from environment variables
- local development can set these values in PowerShell or an IDE launch profile
- database connection details are loaded from AWS Secrets Manager through `ConnectionManager.cs`

## Code quality & security

Pre-commit hooks run automatically before each commit to catch issues early:

**Install (run once after cloning):**
```powershell
pip install --user pre-commit
pre-commit install
```

**What the hooks check:**
- `detect-secrets` — block commits containing AWS keys, tokens, or other secrets
- `trailing-whitespace` and `end-of-file-fixer` — enforce whitespace and file formatting
- `check-yaml` and `check-json` — validate YAML and JSON syntax
- `dotnet format --verify-no-changes` — enforce .NET code formatting

**Run manually (all files):**
```bash
pre-commit run --all-files
```

**Bypass hooks (not recommended):**
```bash
git commit --no-verify
```

## Useful files

- [`scripts/README.md`](scripts/README.md) — **site setup guide**: new sites, admin subdomains, clean URLs, 301 redirects, GoDaddy DNS, troubleshooting
- `scripts/sites.json` — the site registry; adding a new site starts with one entry here
- `STATIC-PRERENDER-ROLLOUT.md` — SPA → static HTML migration runbook
- `scripts/preflight-cutover.ps1` — read-only go/no-go report for a site
- `scripts/migrate-site-to-static.ps1` — the phased cutover, with a rollback per visible phase
- `scripts/configure-render-trigger.ps1` — wires the S3 → re-render Lambda notification
- `infra/static/page-layout.css` — structural CSS for static pages; edit here, nowhere else
- `infra/cloudfront/public-clean-urls.js` + `.test.js` — public routing, run the tests with `node`
- `ApplicationDocumentation.txt` — original manual public-site bucket/DNS/CloudFront notes
- `PLAN.md` — feature plan, progress tracking, and implementation summary
- `api/WebApplicationArch/aws-lambda-tools-defaults.json` — default Lambda stack and bucket settings
- `api/WebApplicationArch/serverless.template` — SAM function and API definitions
- `.pre-commit-config.yaml` — pre-commit hook configuration

## TODO

- **Lock down Lambda API endpoints with `X-Api-Key` validation** — the `/mcp` entry point is protected, but the underlying API endpoints (`/page`, `/article`, `/menu`, etc.) currently accept requests without key validation. `ValidateApiKey()` is already implemented in `ApiBaseFunctions` and wired into the new menu functions. To extend it to all existing handlers, the React admin frontend must first be updated to send `X-Api-Key` on every request — otherwise existing sites will break. See `SECURITYISSUES.md` item #6 for full details.

## Notes

This README is informational and focused on application structure and deployment. For security-specific guidance, see `SECURITYISSUES.md`.
