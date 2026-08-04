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
$PUBLISH = 'all'        # <-- the live value. Now that every applicable site is
                        #     migrated, "all" is the normal setting and the
                        #     per-site list below is only an escape hatch.
                        #     1=faithincrisis 8=cesletter 5=apologetics
                        #     6=discussions 2=doctrines 4=reflective
                        # Maintaining the list BY HAND is itself the hazard -- a site
                        # left off is migrated-but-frozen with nothing reporting it.
                        # "all" cannot destroy reflectiverealizations: id 4 is in the
                        # server-side NeverPublish deny list, which outranks this var.
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

**`ldsgospeldoctrine` (id 9, 143 pages) was migrated 2026-08-04** and did *not* follow this
run book, because it was never the React SPA — it was a hand-rolled static site edited in the
browser by TinyMCE. See [Migrating a non-SPA site](#migrating-a-non-spa-site) below.

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

## Migrating a non-SPA site

`ldsgospeldoctrine.info` (id 9) was the first site migrated that was **not** the React SPA, and
four of its differences are worth knowing before another one like it turns up.

**1. There is no `?page=` to redirect — but every old URL still moves.** The old site served
`/section/name.html`; the platform serves flat `/slug/`, and the slug comes from the page
**title**, not the filename, so only 79 of 143 could be derived mechanically.

**2. The redirect table went in S3, not the CloudFront function.** 163 entries came to 12.7 KB
against the function's hard **10 KB source limit** — and that function is shared by every public
distribution, so a per-site table there is a blast-radius problem as well as a size one. Instead
each old key became a zero-byte object carrying `x-amz-website-redirect-location`, which the S3
*website* endpoint answers with a real 301. No size limit, no shared code:

```powershell
aws s3 cp empty.bin "s3://www.{domain}/old/path.html" --website-redirect "/new-slug/"
```

> Two traps. **Git Bash rewrites the leading `/`** in `--website-redirect` into a Windows path
> and the call fails with `InvalidRedirectLocation` — set `MSYS_NO_PATHCONV=1`. And this only
> works once the distribution uses the **website** endpoint (`set-public-origin.ps1`); at the
> REST endpoint the metadata is ignored.

**3. Query-string redirects need the cache policy changed.** The bucket's 49
`?PageId=LessonN` rules are S3 *routing rules* (a query string cannot be an object key). They
fired at the origin but returned 404 through CloudFront, because `Managed-CachingOptimized`
strips query strings before the origin ever sees them. Fixed with a cache policy that
whitelists just `PageId` (`ldsgospeldoctrine-pageid-qs`) — whitelisting rather than forwarding
all query strings keeps `utm_*` tags from fragmenting the cache. Set `HostName` + `Protocol`
on each rule too, or the 301 leaks the raw `s3-website-…amazonaws.com` host over http.

**4. `-Phase root` has no SPA to protect, but still needs the admin site first.** Same rule,
different reason: once the root `index.html` is the Home render, the old in-page editor is gone
and `admin.{domain}` is the only way back into the content.

**Content import.** The article body is the inner HTML of the old page's content container;
everything else (header, nav, analytics, editor) is chrome the platform now supplies. Two
things that are easy to get wrong, both found here:

- **Headings may not be headings.** This site's CSS forced `#content h1…h6` to `18px`, so
  `<h1>` was used as a bold tag ~20× per page, and most section headings were
  `<p><strong>…</strong></p>` or a bare `<div><strong>…</strong></div>` with no heading tag at
  all. Rebuilding a real outline is inference, not a regex — pilot one section and read the
  output before running the rest.
- **A page's own index may live in the chrome.** `#left-sidebar` is a duplicate nav on article
  pages (drop it) but on a section table-of-contents page it *is* the content — the Book of
  Mormon index kept all 41 lesson links there and nowhere else.

**Gate every page on content preservation.** Compare the source and output text with all
whitespace stripped and refuse any page that shrinks. That catches a transform bug on page 3
instead of page 140.

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
> **Trap 5 — order of writes decides whether a new page renders at all.** *(Fixed
> 2026-07-30 — kept because the constraint still governs any code that creates pages.)*
> Writing article HTML to S3 is what fires the render trigger, and the handler only
> re-renders **served pages that already reference that article path**. So the page must
> exist, be linked, and be published *before* the content upload. `create_page_with_article`
> used to upload content second of five steps — the trigger fired against a page that did
> not exist yet, found nothing, and skipped. The tool reported success,
> `regenerate_sitemap` listed the new URL, and the live URL 404'd until someone happened
> to save the article again. Nothing errored.
>
> The tool now uploads content **last**; see the comment block in
> `api/mcp/src/tools/composite.js`, which explains why the order is load-bearing.
> If you write any other page-creation path, preserve that order.
>
> **A correct sitemap is still not evidence that a page rendered** — those are separate
> code paths. Spot-check one live URL after a batch of new pages.

> **Trap 6 — a sitewide file change is a whole-site re-render, not a one-file edit.**
> The nav is defined once in `sitemenu.json`, but the prerenderer **bakes a copy of it
> into every page's HTML** at render time. Adding, removing, renaming, or reordering a
> menu item therefore leaves every already-rendered page carrying the old nav — the new
> entry is unreachable from anywhere except a direct URL or the sitemap, and nothing
> reports a problem. Found on cesletter.info, 2026-07-29.
>
> **The same is true of the header, the site metadata, and the theme** — all four are
> inlined into every page. This is now handled automatically: a write to any of them
> triggers a whole-site re-render. See the section below for the full list.
>
> `header.html` was the nastiest of the four: its writes **already reached the render
> Lambda** via the article notification's `.html` suffix, but `TryParseArticleKey`
> rejected the key and the Lambda logged "skipping non-article key" — so a header edit
> looked saved and changed nothing. Found on cesletter.info 2026-07-30.

### Sitewide files: a write to any of these re-renders the whole site

Four files are baked into **every page** at render time, so changing one and doing
nothing else leaves every already-rendered page carrying the old copy — silently.
Each has an S3 notification pointing at the render Lambda:

| File | Location | What it controls |
|---|---|---|
| `sitemenu.json` | `public/websites/{domain}/` | the nav on every page |
| `header.html` | `public/websites/{domain}/` | the header banner |
| `site-meta.json` | `public/websites/{domain}/` | public title + GA measurement id |
| `theme.css` | `public/assets/{domain}/themes/` | ThemeBuilder colours/fonts |

`ApiStaticRenderFunctions.TryParseSiteAssetKey` / `TryParseThemeKey` recognise these
and route them to a whole-site render; `TryParseArticleKey` deliberately **rejects**
them, since an article write is a one-page render. To add another sitewide file, put
its name in the `SiteWideAssets` set — and check S3 actually delivers it, because
filter rules allow one prefix and one suffix each.

> **`BaseStyles.css` is deliberately NOT on this list.** It is inlined too, but it
> lives in the *public* bucket — the same bucket the renderer writes into — so a
> notification there is one careless suffix edit away from infinite recursion, and it
> would fire on every SPA deploy. After editing it, re-render by hand:
> `RegenerateAllStaticPages`, or `publish-static-pages.ps1 -Site X -Upload`.

Cost: a whole-site render is one read and one write per page. The sitewide files are
loaded **once per site**, not once per page — before that fix a single menu edit on
`ldsdoctrines` did ~2,355 redundant S3 reads of five identical objects. It is still
471 pages of work, so batch sitewide edits rather than making them one at a time.

Deployed 2026-07-30. To re-apply after a change:

```powershell
# STATIC_PUBLISH_SITES is a template parameter re-applied on every deploy. If it is
# unset in the shell, publishing turns OFF for every site at once -- set it first.
$env:STATIC_PUBLISH_SITES = [Environment]::GetEnvironmentVariable('STATIC_PUBLISH_SITES','User')
$env:STATIC_PUBLISH_SITES        # currently 'all' -- do not proceed if empty
./scripts/deploy-lambda-no-rotate.ps1 -ProfileName tbirdcontractinggmailcom
./scripts/configure-render-trigger.ps1
```

> **A sitewide edit rewrites the bucket root `index.html`**, because Home is one of the
> pages re-rendered. That is correct — post-migration the root *is* the Home render —
> but unlike `-Phase root` it takes no backup first. A broken Home article will
> therefore reach the live homepage via a menu edit. `reflectiverealizations` (id 4) is
> unaffected: it is in the server-side `NeverPublish` deny list, which outranks
> `STATIC_PUBLISH_SITES` even when that is set to `all`.

The render Lambda runs at **1024 MB / 900 s** to cover the worst-case whole-site
render, and renders pages 8-at-a-time (`SiteRenderConcurrency`) since the work is
I/O bound. Measured on cesletter.info: 33 pages in **7.3 s**, peak memory 157 MB —
extrapolating to roughly 100 s for ldsdoctrines at 471 pages, well inside the ceiling.
Memory does not grow with site size because concurrency is bounded; the 1024 MB is
bought for the CPU that comes with it, not the RAM. Verify:

```powershell
./scripts/configure-render-trigger.ps1 -WhatIf     # shows all four entries, writes nothing
aws logs tail /aws/lambda/<fn> --follow --profile tbirdcontractinggmailcom --region us-west-2
```

Then change one menu item and confirm an *unrelated* page picks up the new nav.

> `configure-render-trigger.ps1` only manages the four ids in its `$OurEntries` table
> and preserves anything else on the bucket verbatim. That is deliberate, but it means
> a notification added by hand will not be cleaned up by a re-run — which is how the
> environment drifted from the repo once already. Check `-WhatIf` output against the
> live config if the two ever look out of step.

The log line to look for is `'sitemenu.json' changed on {domain} -- re-rendering N page(s)`,
followed by `'sitemenu.json' re-render touched N/N`. A count short of N means individual
pages failed and the site is now half-updated — the per-page errors are logged above it.

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

### Checking apex / www / http behaviour

**Do not use `Invoke-WebRequest -MaximumRedirection 0` for this.** PowerShell 5.1 throws
`Operation is not valid due to the current state of the object` on a redirect, which is
indistinguishable from a TLS failure in the catch block — it produced a false "apex is
broken with a TLS error" report for `ldsdoctrines.com` when the apex was in fact
redirecting correctly. Use `curl`, which reports the status and `Location` plainly:

```bash
for d in ldsfaithincrisis.com ldsdoctrines.com ldsapologetics.com ldsdiscussions.info cesletter.info; do
  for u in "http://$d/" "https://$d/" "http://www.$d/" "https://www.$d/"; do
    curl -sS -o /dev/null -m 20 -w "$u %{http_code} %{redirect_url}\n" "$u"
  done
done
```

Note `curl -I` (HEAD) is misleading against GoDaddy forwarding, which answers **405** to
HEAD while answering 301 to GET. Always use GET for the apex.

Expected shape, verified 2026-07-30: `http://www` → 301 → `https://www` → 200 on every
site. The apex is a separate problem — see below.

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
