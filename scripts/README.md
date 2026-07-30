# Site Setup & Deployment Scripts

Everything needed to stand up a new site, or to migrate an existing one from the
client-rendered SPA to pre-rendered static HTML with a separate admin subdomain.

All scripts are PowerShell, live in this folder, and are **idempotent** — every
resource is looked up before it is created, so a script that fails halfway (an
expired token, a DNS record you had not added yet) can simply be re-run. Nothing
is destroyed on a re-run.

---

# ▶ RUN THIS — migrating a site

**Once, ever** — already done as of 2026-07-29, listed here so a fresh clone can repeat it.
This wires the *bucket* to the render Lambda, which is what makes an admin edit re-publish
the public page. It is bucket-wide, so it covers every site at once and is **not** repeated
per site:

```powershell
./scripts/deploy-lambda-no-rotate.ps1  -ProfileName tbirdcontractinggmailcom
./scripts/configure-render-trigger.ps1
```

> Use `deploy-lambda-no-rotate.ps1`, **not** `dotnet lambda deploy-serverless` — only the
> script passes the template parameters (`StaticPublishSites`, `RdsSecretReadPolicyArn`).
> A bare `deploy-serverless` reuses the previous values, so it will not pick up a change
> you just made to `$env:STATIC_PUBLISH_SITES`. And never
> `deploy-lambda-with-tokens.ps1` for this — it **rotates TOKEN_SECRET/TOKEN_IV** and logs
> every admin session out.

**Then per site.** Copy the block, set `$S` once, run the lines top to bottom.
**Stop and look at the site after each numbered comment.**

```powershell
$S = 'reflectiverealizations'     

# 1. Is this site ready?  (read-only, changes nothing)
./scripts/preflight-cutover.ps1 -Site $S

# 2. Admin site FIRST -- step 6 refuses to run without it
./scripts/deploy-admin-subdomain.ps1 -Site $S
./scripts/deploy-admin-spa.ps1       -Site $S
#    >> open https://admin.<domain>/ and log in before going on <<

# 3. Upload the static pages  (additive -- live site untouched)
./scripts/migrate-site-to-static.ps1 -Site $S -Phase backfill

# 4. Prove every new URL works  (read-only)
./scripts/migrate-site-to-static.ps1 -Site $S -Phase verify

# 5. Swap the homepage  (FIRST VISIBLE CHANGE -- backs up the old one)
./scripts/migrate-site-to-static.ps1 -Site $S -Phase root
#    >> check https://www.<domain>/ <<

# 6. Turn on clean URLs + legacy ?page= 301s  (VISIBLE)
./scripts/migrate-site-to-static.ps1 -Site $S -Phase routing
#    >> check an old ?page= link redirects <<

# 7. Sitemap  (last, on purpose)
./scripts/migrate-site-to-static.ps1 -Site $S -Phase sitemap

# 8. Sign off before starting the next site
./scripts/migrate-site-to-static.ps1 -Site $S -Phase validate

# 9. Let this site publish. The S3 trigger already fires for EVERY site's article
#    writes -- the Lambda then skips any site not named here. Until this runs, an
#    edit updates the CMS and the admin, but NOT the live page. No error either way.
#    The list is cumulative -- include every site migrated so far, comma-separated.
#    Write the list ONCE into a variable, then set both scopes from it. Setting the two
#    lines separately is how ldsdiscussions got left out: the User value said '1,8,5'
#    while the shell said '1,8,5,6', the deploy took the User one, and the site was
#    migrated-but-frozen with nothing reporting a problem.
$PUBLISH = '1,8,5,6,2,4'    # <-- ADD this site's id.  1=faithincrisis 8=cesletter
                        #     5=apologetics 6=discussions 2=doctrines 4=reflective
[Environment]::SetEnvironmentVariable('STATIC_PUBLISH_SITES', $PUBLISH, 'User')
$env:STATIC_PUBLISH_SITES = $PUBLISH
./scripts/deploy-lambda-no-rotate.ps1 -ProfileName tbirdcontractinggmailcom
#    >> confirm the deploy printed:  PublishSites= '1,8,5,6'  <<
#    >> then edit an article in the admin, wait ~5 min, confirm the public page updates <<
```

> **Step 9 is not optional, and it is easy to forget.** Without it the site is fully
> migrated but frozen: every future content edit silently fails to reach the public
> page. `STATIC_PUBLISH_SITES` is a CloudFormation **template parameter**, so it is
> re-applied on every deploy — if it is unset in your shell when you deploy, publishing
> turns back **off for every site at once**. That is why it is set as a User environment
> variable above and not just in the shell.
>
> **Deploy the API before enabling the site, not after.** The re-render runs the C#
> renderer; if that build is older than the PowerShell renderer used for the backfill,
> the first edit silently reverts that page to whatever the old renderer produced.
> `deploy-lambda-no-rotate.ps1` does both in one step, which is why it is the command here.
>
> **The public page lags by up to 5 minutes** — static pages carry
> `Cache-Control: max-age=300`. The re-render itself takes about a second. An edit that
> "did not work" has almost always already rendered; check the S3 object's `LastModified`
> before assuming the trigger failed. See *How soon an edit appears* in the root `README.md`.

**Something looks wrong?**

```powershell
./scripts/migrate-site-to-static.ps1 -Site $S -Phase root    -Rollback   # ~2 min
./scripts/migrate-site-to-static.ps1 -Site $S -Phase routing -Rollback   # 5-10 min
```

**Rules:** one site at a time, fully validated before the next · steps 1–4 cannot
break the live site · never run `publish-static-pages.ps1 -Upload` by hand without
`-ExcludeHome` (it overwrites your homepage) · sitemap always last.

**All five applicable sites are migrated as of 2026-07-29:** `ldsfaithincrisis` (3 pages),
`cesletter` (29), `ldsapologetics` (140), `ldsdiscussions` (88), `ldsdoctrines` (471).
The order was smallest-blast-radius-first, and each site taught the preflight a new check.

**`reflectiverealizations.com` is deliberately NOT migrated** and has been removed from the
registry. It is a hand-built static site, not the React SPA — no script tags, its own fonts and
stylesheets, a hand-authored homepage — so it is already fully crawlable and the migration has
nothing to offer it. Its CMS record holds a single 2 KB article, so `-Phase root` would overwrite
the 8.5 KB live homepage with that render and destroy the design. The scripts now refuse the key
with that reason; see `"excluded"` in [`sites.json`](sites.json).

Why this order, and what each phase risks: [run order](#migrating-an-existing-site-to-static--admin).

---

## Contents

**Start here for a migration:** [`preflight-cutover.ps1`](preflight-cutover.ps1) then
[`migrate-site-to-static.ps1`](migrate-site-to-static.ps1) — see
[run order](#migrating-an-existing-site-to-static--admin) below.

| Script | Run it | What it does |
|---|---|---|
| [`preflight-cutover.ps1`](preflight-cutover.ps1) | before every cutover | **Read-only** go/no-go report: credentials, CMS, slug parity, function tests, bucket, distribution, page coverage, admin status. Changes nothing |
| [`migrate-site-to-static.ps1`](migrate-site-to-static.ps1) | the cutover itself | Orchestrates the phased migration — `backfill` → `verify` → `root` → `routing` → `sitemap` → `validate`, with a rollback for each visible phase |
| [`deploy-admin-subdomain.ps1`](deploy-admin-subdomain.ps1) | once per site | Bucket, ACM cert, OAC, noindex policy, CloudFront distribution, GoDaddy CNAME for `admin.{domain}` |
| [`deploy-admin-spa.ps1`](deploy-admin-spa.ps1) | every admin release | Stages the site's config, builds the React app, uploads with correct cache headers, invalidates |
| [`deploy-cloudfront-function.ps1`](deploy-cloudfront-function.ps1) | once per site | Publishes + attaches the clean-URL rewrite and legacy `?page=` 301 redirects. Called for you by `-Phase routing` |
| [`publish-static-pages.ps1`](publish-static-pages.ps1) | via `-Phase backfill` | Pre-renders every CMS page to static HTML and uploads it. **Calling it directly? Pass `-ExcludeHome`** — see Trap 3 |
| [`regenerate-sitemaps.ps1`](regenerate-sitemaps.ps1) | via `-Phase sitemap` | Writes clean-URL sitemaps |
| [`deploy-lambda-with-tokens.ps1`](deploy-lambda-with-tokens.ps1) | API changes | Deploys the .NET Lambda stack |
| [`generate-mcp-api-key.ps1`](generate-mcp-api-key.ps1) | rarely | Rotates the MCP API key |

| Support file | Purpose |
|---|---|
| [`sites.json`](sites.json) | The site registry. **Adding a new site means adding one entry here.** |
| [`lib/SiteInfra.ps1`](lib/SiteInfra.ps1) | Shared get-or-create helpers for S3 / ACM / CloudFront |
| [`lib/GoDaddyDns.ps1`](lib/GoDaddyDns.ps1) | DNS record creation and propagation polling at GoDaddy |

---

## Prerequisites

**AWS CLI v2**, authenticated as **`tbirdcontractinggmailcom`** — one profile for
everything: S3, CloudFront, ACM, and the Lambda deploy. It is the default in every
script here, and in `api/WebApplicationArch/aws-lambda-tools-defaults.json`, so you
should not need to pass `--profile` at all. Every script still accepts
`-AwsProfile <name>` if you want to override it.

If a script fails immediately with *"session expired"*, re-auth and run it again —
it will pick up wherever it stopped.

**Environment variables** (the content scripts talk to the CMS API):

```powershell
$env:LAMBDA_API_BASE_URL   # e.g. https://xxxx.execute-api.us-west-2.amazonaws.com/prod
$env:MCP_API_KEY           # the CMS API key
```

See the root [`CLAUDE.md`](../CLAUDE.md) for how to retrieve both from the deployed stack.

**GoDaddy credentials** (optional but recommended — see [DNS](#dns-at-godaddy)):

```powershell
[Environment]::SetEnvironmentVariable('GODADDY_PAT','<your token>','User')
$env:GODADDY_PAT = '<your token>'    # so the current shell has it too
```

Use a **Personal Access Token** from <https://developer.godaddy.com/keys>. It needs
the **`domains.dns:update`** scope, and must be a **production** token (not OTE/test).

> A legacy `GODADDY_API_KEY` + `GODADDY_API_SECRET` pair is also accepted — these
> scripts only call v1 endpoints, where it still works. But GoDaddy has the
> `sso-key` scheme scheduled for discontinuation after 2026 and it does not work
> with v3 at all, so prefer the PAT. If both are set, the PAT wins.

Without either, the scripts print each DNS record and wait while you add it by hand
— everything still works, it is just slower.

Verify the token before you need it:

```powershell
./scripts/preflight-cutover.ps1 -Site ldsapologetics
```

It does a **read-only** call against the domain and reports whether the credential
can actually reach it. A wrong scope, an OTE token, and an ineligible account tier
all fail identically mid-cutover — much better to find out now.

**Node.js + npm** for the React build, **.NET 8 SDK** for the API.

---

## Architecture: two very different sites per domain

| | Public site | Admin site |
|---|---|---|
| Host | `www.{domain}` | `admin.{domain}` |
| Content | Pre-rendered static HTML at `/slug/` | React SPA, client-rendered |
| Bucket | `www.{domain}` — **public**, static website hosting on | `admin-{key}` — **private**, no website hosting |
| CloudFront origin | The S3 *website endpoint* (a custom origin, HTTP) | The S3 *REST endpoint* via Origin Access Control (HTTPS) |
| Indexing | Crawlable; that is the whole point | `robots.txt` + `X-Robots-Tag: noindex, nofollow` |
| Auth | None | Cognito |
| Routing | `/slug` → `/slug/index.html`; `?page=X` → 301 | 403/404 → `/index.html` with a 200 (SPA fallback) |

### Why the admin bucket has no dots in its name

The public buckets are named `www.{domain}` and that is fine, because CloudFront
reaches them through the **S3 website endpoint**, which is a *custom* origin spoken
to over plain HTTP — no certificate to match.

The admin bucket is private and read through the **S3 REST endpoint over HTTPS**.
The `*.s3.{region}.amazonaws.com` wildcard certificate does not match a
multi-label host, so a bucket literally named `admin.example.com` cannot be used
as a CloudFront origin at all. Hence `admin-{key}`.

The bucket-name-must-equal-the-domain rule only ever applied to S3 static website
hosting. A private bucket behind CloudFront can be called anything.

---

## Setting up a brand-new site

### 1. Register the domain and add it to the registry

Add an entry to [`sites.json`](sites.json):

```json
{
  "key": "newsite",
  "id": 9,
  "domain": "newsite.com",
  "title": "New Site",
  "analytics": "",
  "dns": "godaddy",
  "configPrefix": "newsite"
}
```

- `key` — short slug used on the command line and in bucket names. **No dots.**
- `id` — `WEBSITE_ID` in the CMS. Must match the `website` table row and the
  `websiteId` in the site's config JSON.
- `configPrefix` — filename prefix in `react/baseProject/configs/`. This is **not
  always the same as `key`** (`ldsfaithincrisis` builds from `faithInCrisis-*`).
  Use `null` if no config set exists yet.

### 2. Create the site's React config set

In `react/baseProject/configs/`, create the files the build stages into `public/`:

| File | Required | Notes |
|---|---|---|
| `{prefix}-Prod.json` | yes | Copy `config.example.json`. Set `siteName`, `websiteId`, `analyticsTag` |
| `{prefix}-BaseStyles.css` | | |
| `{prefix}-favicon.ico` | | |
| `{prefix}-header.html` | | |
| `{prefix}-initialRender.html` | | |
| `{prefix}-headerImage.jpg` | | Not every site has one |

The app **fetches `/config.json` at runtime** (`src/hooks/configuration/useConfig.js`),
so this is a real file served from the web root, not a build-time constant.
`deploy-admin-spa.ps1` copies `{prefix}-Prod.json` → `public/config.json` before
building. `public/config.json` is gitignored because it is per-site scratch.

### 3. Create the public site

The public side is still the manual process documented in
[`ApplicationDocumentation.txt`](../ApplicationDocumentation.txt): public buckets
with static website hosting, bucket policy, CORS, Amplify IAM role, ACM cert,
CloudFront distribution, GoDaddy CNAME. There is no script for this yet.

### 4. Create the database rows

A `website` row whose `siteName` matches the domain, and a `page` row for `Home`.
The `website.id` becomes the `websiteId` in the config JSON and the `id` in
`sites.json`. See `ApplicationDocumentation.txt` §7.

### 5. Stand up the admin subdomain

```powershell
./scripts/deploy-admin-subdomain.ps1 -Site newsite
./scripts/deploy-admin-spa.ps1       -Site newsite
```

You can run the first one before the site is in `sites.json`, using
`-Domain newsite.com` — handy when you want the infrastructure up first.

### 6. Publish content and enable clean URLs

A brand-new site has no live SPA to protect, so there is nothing to cut over — but
use the same phased tooling anyway, because it carries the ordering and the checks:

```powershell
./scripts/preflight-cutover.ps1        -Site newsite
./scripts/migrate-site-to-static.ps1   -Site newsite -Phase all
```

If you would rather drive the underlying scripts yourself, the order is backfill →
routing → sitemap, and `publish-static-pages.ps1` needs `-ExcludeHome` unless you
intend to write the site root:

```powershell
./scripts/publish-static-pages.ps1       -Site newsite -Upload -ExcludeHome
./scripts/publish-static-pages.ps1       -Site newsite -Upload -Slug Home   # the root
./scripts/deploy-cloudfront-function.ps1 -Site newsite
./scripts/regenerate-sitemaps.ps1        -Site newsite
```

**Order matters.** See the traps in the next section.

---

## Migrating an existing site to static + admin

**Run order.** Do **one site at a time**, validated before starting the next.
[`STATIC-PRERENDER-ROLLOUT.md`](../STATIC-PRERENDER-ROLLOUT.md) has the per-phase
risk and rollback detail.

### Step 0 — once, globally

```powershell
cd api/WebApplicationArch; dotnet lambda deploy-serverless; cd ../..
```

Ships the on-save re-render hook, the clean-URL sitemap, and the `BaseStyles.css`
fix. Do this before editing any article, or the edit re-renders without the site's
brand stylesheet.

### Then, per site — in this order

| # | Command | Visitor impact | Reversible |
|---|---|---|---|
| 1 | `./scripts/preflight-cutover.ps1 -Site X` | none — read-only | n/a |
| 2 | `./scripts/deploy-admin-subdomain.ps1 -Site X` | none — new infra | n/a |
| 3 | `./scripts/deploy-admin-spa.ps1 -Site X` | none — new infra | re-run |
| 4 | `./scripts/migrate-site-to-static.ps1 -Site X -Phase backfill` | **none** — additive | delete keys |
| 5 | `./scripts/migrate-site-to-static.ps1 -Site X -Phase verify` | none — read-only | n/a |
| 6 | `./scripts/migrate-site-to-static.ps1 -Site X -Phase root` | **visible** | `-Phase root -Rollback` |
| 7 | `./scripts/migrate-site-to-static.ps1 -Site X -Phase routing` | **visible** | `-Phase routing -Rollback` |
| 8 | `./scripts/migrate-site-to-static.ps1 -Site X -Phase sitemap` | crawlers only | regenerate |
| 9 | `./scripts/migrate-site-to-static.ps1 -Site X -Phase validate` | none — read-only | n/a |

Steps 4–9 are also available as `-Phase all`, which walks them in order and prompts
before each visible one. Use that only after the first site has proven the process.

### Why this order — four traps

> **Trap 1 — the admin site must exist before `root` (steps 2–3 before step 6).**
> Replacing the root `index.html` removes the SPA from `www.{domain}` entirely, so
> there is no longer any way to reach the CMS on the public host. `-Phase root`
> hard-refuses unless `https://admin.{domain}/` returns 200. Standing the admin site
> up early also gets the ACM DNS validation wait out of the way.
>
> **Trap 2 — content before routing (step 4 before step 7).** The CloudFront function
> 301s `?page=X` to `/x/`. Attach it before the backfill and every legacy link
> redirects to an object that does not exist.
>
> **Trap 3 — never backfill Home with the raw script.** `publish-static-pages.ps1`
> renders Home to the bucket **root** `index.html` — the live SPA shell. Running it
> without `-ExcludeHome` replaces your homepage immediately, with no backup taken.
> The `backfill` phase passes `-ExcludeHome` for you and handles Home in `root`,
> which backs up the current object first. If you call the script directly, pass
> `-ExcludeHome` yourself.
>
> **Trap 4 — sitemap last (step 8).** A sitemap full of URLs that 404 teaches
> crawlers to distrust the site.
>
> **Trap 5 — a newly created page does not render until the article is saved twice.**
> The MCP `create_page_with_article` tool writes the article HTML to S3 *before* the
> page link exists, so the render trigger fires with no slug to resolve and skips the
> page. It reports success, `regenerate_sitemap` happily lists the new URL, and the
> live URL 404s. Nothing logs an error. Fix by calling `set_article_content` on the
> same article id a second time with identical HTML, or by running
> `publish-static-pages.ps1 -Site X -Upload -ExcludeHome`. **A correct sitemap is not
> evidence that a page rendered** — those are separate code paths. Always spot-check
> one live URL after a batch of new pages. Found on cesletter.info, 2026-07-29.
>
> **Trap 6 — a menu change is a whole-site re-render, not a one-file edit.** The nav
> is defined once in `sitemenu.json`, but the prerenderer **bakes a copy of it into
> every page's HTML** at render time. Adding, removing, renaming, or reordering a menu
> item therefore leaves every already-rendered page carrying the old nav — the new
> entry is unreachable from anywhere except a direct URL or the sitemap, and nothing
> reports a problem. Found on cesletter.info, 2026-07-29.
>
> This is now handled automatically — a `sitemenu.json` write triggers a whole-site
> re-render — but **only once the pending deploy below has run.** Until then, do it by
> hand after any menu change:
>
> ```powershell
> ./scripts/publish-static-pages.ps1 -Site X -Upload -ExcludeHome
> ./scripts/publish-static-pages.ps1 -Site X -Upload -Slug Home     # the root
> ```
>
> Then invalidate. Either way, on a large site (`ldsdoctrines`, 471 pages) a single
> menu edit costs a full re-render — so batch menu changes rather than making them one
> at a time.

### Pending: automatic re-render on menu change

Written and unit-tested, **not yet deployed** as of 2026-07-30. Two commands, in this
order:

```powershell
# STATIC_PUBLISH_SITES is a template parameter re-applied on every deploy. If it is
# unset in the shell, publishing turns OFF for every site at once -- set it first.
$env:STATIC_PUBLISH_SITES = [Environment]::GetEnvironmentVariable('STATIC_PUBLISH_SITES','User')
$env:STATIC_PUBLISH_SITES        # expect: 1,8,5,6,2,4  -- do not proceed if empty
./scripts/deploy-lambda-no-rotate.ps1 -ProfileName tbirdcontractinggmailcom
./scripts/configure-render-trigger.ps1
```

`configure-render-trigger.ps1` now writes **two** notifications — one for articles
(`.html`) and one for the menu (`sitemenu.json`). S3 allows a single prefix and suffix
per entry, so the menu cannot share the article filter. Both target the same Lambda;
`ApiStaticRenderFunctions.TryParseMenuKey` tells them apart.

The render Lambda moves to **1024 MB / 900 s** to cover the worst-case whole-site
render, and renders pages 8-at-a-time (`MenuRenderConcurrency`) since the work is
I/O bound. Verify after deploying:

```powershell
./scripts/configure-render-trigger.ps1 -WhatIf     # shows both entries, writes nothing
aws logs tail /aws/lambda/<fn> --follow --profile tbirdcontractinggmailcom --region us-west-2
```

Then change one menu item and confirm an unrelated page picks up the new nav.

### Rolling back

```powershell
./scripts/migrate-site-to-static.ps1 -Site X -Phase root    -Rollback   # ~2 min
./scripts/migrate-site-to-static.ps1 -Site X -Phase routing -Rollback   # 5-10 min
```

`root -Rollback` restores the SPA shell from `dist/rollback/{site}/root-index.html`
and invalidates. `routing -Rollback` detaches the CloudFront function; that is a
distribution update, so budget 5–10 minutes for it to propagate.

---

## Script reference

### `deploy-admin-subdomain.ps1`

Creates, in order: private bucket → ACM cert (us-east-1, DNS-validated) → Origin
Access Control → noindex response-headers policy → CloudFront distribution →
bucket policy scoped to that distribution's ARN → GoDaddy CNAME.

```powershell
./scripts/deploy-admin-subdomain.ps1 -Site ldsapologetics
./scripts/deploy-admin-subdomain.ps1 -Domain brandnew.com     # not yet in sites.json
./scripts/deploy-admin-subdomain.ps1 -Site x -SkipDns         # AWS resources only
./scripts/deploy-admin-subdomain.ps1 -Site x -PriceClass PriceClass_All
```

The bucket policy is written *after* the distribution because it is scoped by
`AWS:SourceArn` — that ARN does not exist until the distribution does. This is the
one genuinely awkward ordering in the whole flow; the console hides it behind a
"copy policy" button.

Two settings in the distribution matter and are easy to get wrong:

- **403 *and* 404 both map to `/index.html` with a `200`.** A private bucket answers
  a missing key with 403, not 404, so mapping only 404 leaves every SPA deep link
  showing an AccessDenied page. The `200` matters too — returning the shell with a
  403/404 status makes the router and the browser treat it as an error.
- **`S3OriginConfig.OriginAccessIdentity` is empty *and* `OriginAccessControlId` is
  set.** The empty OAI means "not the legacy mechanism"; the OAC id is what grants
  access.

### `deploy-admin-spa.ps1`

```powershell
./scripts/deploy-admin-spa.ps1 -Site ldsapologetics
./scripts/deploy-admin-spa.ps1 -Site ldsapologetics -SkipBuild   # upload existing build/
```

Uploads in three passes because the file types need opposite caching:

| Pass | Files | `Cache-Control` |
|---|---|---|
| 1 | content-hashed assets (`/static/*`, images) | `public,max-age=31536000,immutable` |
| 2 | `*.html` | `no-cache` |
| 3 | `config.json`, `sitemenu.json` | `no-cache` |

Getting this backwards is the classic SPA deploy bug: `index.html` gets cached at
the edge, so users keep loading an old shell referencing JS bundles that no longer
exist. `config.json` and `sitemenu.json` are in the same category — fetched by a
fixed name at runtime and not content-hashed, so an immutable header would pin the
site to a stale config until the next invalidation.

Requires the distribution to already exist; it will tell you to run
`deploy-admin-subdomain.ps1` if it does not.

### `deploy-cloudfront-function.ps1`

Publishes [`infra/cloudfront/public-clean-urls.js`](../infra/cloudfront/public-clean-urls.js)
and attaches it to the **public** distribution's viewer-request event. This is
what turns on both behaviours:

1. `/?page=Temple-And-Masonry` → **301** → `/temple-and-masonry/`. A 301 (not a
   302) is what passes ranking to the new URL, so old bookmarks, shared links, and
   everything already in Google's index keep working *and* keep their link equity.
2. `/slug` → `/slug/index.html`.

The function body has no site-specific values, so one function is created and
attached to every public distribution.

```powershell
./scripts/deploy-cloudfront-function.ps1 -Site ldsapologetics
./scripts/deploy-cloudfront-function.ps1 -Site x -PublishOnly   # stage code, do not attach
./scripts/deploy-cloudfront-function.ps1 -Site x -Detach        # roll back
./scripts/deploy-cloudfront-function.ps1 -DistributionId E123   # bypass alias lookup
```

It refuses to attach to a distribution whose alias starts with `admin.` — those
need SPA fallback routing, which these rewrites would break.

Verify after it deploys:

```powershell
curl -sI "https://www.ldsapologetics.com/?page=Temple-And-Masonry"   # 301 -> /temple-and-masonry/
curl -sI "https://www.ldsapologetics.com/temple-and-masonry/"        # 200
```

---

## DNS at GoDaddy

All domains are registered **and resolved** at GoDaddy — there are no Route 53
hosted zones. [`lib/GoDaddyDns.ps1`](lib/GoDaddyDns.ps1) handles this two ways:

- **API mode** — with `GODADDY_PAT` set (or a legacy key/secret pair), records are
  created for you. On failure the script explains *which* failure it was — 401 bad
  token, 403 missing scope or ineligible tier, 404 domain not in this account — and
  falls back to manual mode rather than stopping.
- **Manual mode** — prints the exact Type / Name / Value to add in the GoDaddy DNS
  panel, waits for you to add it, then polls `8.8.8.8` until the record resolves
  and carries on. A public resolver is used deliberately so a stale local cache
  cannot report a false negative.

Records created for an admin subdomain:

| Type | Name | Value | Purpose |
|---|---|---|---|
| CNAME | `_xxxx.admin` | `_yyyy.acm-validations.aws` | ACM certificate validation |
| CNAME | `admin` | `dxxxxx.cloudfront.net` | Point the subdomain at CloudFront |

### The apex limitation

A **CNAME works for `admin.{domain}` and `www.{domain}`** because they are
subdomains. The **apex** (`example.com` with no host) is the case GoDaddy cannot
serve — DNS forbids a CNAME at the zone apex, and GoDaddy has no ALIAS/ANAME
record type that can target CloudFront. If a public site ever needs to answer at
the bare domain rather than `www`, that requires either GoDaddy domain forwarding
or moving the zone to Route 53. Nothing in these scripts needs it today.

---

## Troubleshooting

**"session expired" / `NoRegion`**
Re-auth the profile. Every script is idempotent, so just run it again.

**Certificate stuck in `PENDING_VALIDATION`**
The validation CNAME is almost always the cause. Check it exists at GoDaddy and
that the *name* is the relative form (`_xxxx.admin`, not the full
`_xxxx.admin.example.com`). ACM gives up after 72 hours; past that, request a new
cert. Re-running the script resumes the wait.

**`InvalidViewerCertificate` when creating the distribution**
The certificate must be in **us-east-1** regardless of where the bucket lives, and
must be `ISSUED`, and must cover the exact alias.

**Admin site shows AccessDenied on a deep link, but `/` works**
The 403 → `/index.html` custom error response is missing or was not saved.

**Admin site loads an old version after a deploy**
`index.html` was cached. Check its `Cache-Control` is `no-cache` and that the
invalidation completed.

**Admin site loads the wrong site's branding**
`public/config.json` is stale from a previous build. Re-run `deploy-admin-spa.ps1`
for the right `-Site`; it re-stages every asset before building.

**Public clean URLs return 404 after attaching the function**
The static pages were not backfilled first. Run `migrate-site-to-static.ps1 -Phase backfill`,
or `-Detach` the function until they are ready.

**A content edit is not showing on the public site**
Almost always the CloudFront cache, not the render. Static pages carry `max-age=300`, so
give it five minutes; check the S3 object's `LastModified` before assuming anything broke.
To force it, call the `invalidate_cache` MCP tool (no arguments = whole site). If S3 itself
is stale, the site is missing from `STATIC_PUBLISH_SITES` — see step 9.

**`Site 'x' has no configPrefix in sites.json`**
That site has no config set in `react/baseProject/configs/` yet. `reflectiverealizations`
is currently in this state. Create the config files (step 2 above) and set
`configPrefix`.

---

## Related documentation

- [`STATIC-PRERENDER-ROLLOUT.md`](../STATIC-PRERENDER-ROLLOUT.md) — the full migration runbook and target architecture
- [`ApplicationDocumentation.txt`](../ApplicationDocumentation.txt) — the original manual public-site setup notes (still the reference for the public side)
- [`CLAUDE.md`](../CLAUDE.md) — MCP server setup, per-site article styling guides, `WEBSITE_ID` table
- [`SECURITYISSUES.md`](../SECURITYISSUES.md) — outstanding security items
