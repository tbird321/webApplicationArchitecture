# Static Prerender Rollout — Plan of Record

Branch: `feature/static-prerender`

## Why

The public sites run as a client-rendered React SPA. Every URL returns the same near-empty shell at
`?page=Slug`, so crawlers, social scrapers, and AI bots see no content. `site:ldsapologetics.com`
returns only the homepage.

## End state

When this is finished, **every site** is:

| Layer | What it is |
|---|---|
| Public site | Pre-rendered static HTML in S3 at clean paths `/slug/`, no JavaScript required to read it |
| Admin site | React SPA on `admin.{domain}`, Cognito-gated, `noindex` |
| API | Lambda (still needed — auth boundary, save hook, static re-render) |
| Data | **No database.** Content indexes as JSON in S3; article HTML as files in S3 |

Lambda stays. The database goes.

---

## Stages

| Stage | Scope | Risk | When |
|---|---|---|---|
| **0** | Local verification | none | Done — see below |
| **1** | Deploy API + admin subdomains | low | First AWS session |
| **2** | Per-site static cutover | **medium — this is the live one** | **One site at a time** |
| **3** | Inline `reactcomponents` into the admin app | low | After stage 2 is stable |
| **4** | Remove the database | medium | After stage 3 |
| **5** | N-depth menu nesting | low | Last |

Stages 1–2 run **per site**. Stages 3–5 are done once, globally.

### The one-site-at-a-time rule

Migrate one site, run `-Phase validate`, do the manual checks, **then** start the next. A systemic
mistake then costs one site instead of six. `validate` exits non-zero if anything fails, so it is a
real gate and not a suggestion.

---

## Stage 0 — Local verification (complete)

Everything checkable without AWS has been checked:

| Check | Result |
|---|---|
| `dotnet build` (API) | **0 errors**, 117 pre-existing nullable warnings |
| `dotnet test` | **24/24 pass** (was broken before — see #6) |
| React admin SPA `npm run build` | **exit 0** |
| CloudFront function unit tests | **25/25 pass** (`node infra/cloudfront/public-clean-urls.test.js`) |
| Slug parity across **all 867 live page names** | **0 mismatches** |
| End-to-end local render of a real site | 29 cesletter pages, output inspected |

### Six real bugs found and fixed

Every one of these was silent — the scripts reported success while producing broken output.

**1 · CloudFront 301s would have pointed at 404s (latent).**
The function's slug algorithm did not match `StaticPageRenderer.Slug`. The renderer **deletes**
unsafe characters; the function **replaced** them with a hyphen.

| Input | File written to | Function redirected to |
|---|---|---|
| `Joseph Smith's Polygamy` | `/joseph-smiths-polygamy/` | `/joseph-smith-s-polygamy/` |
| `Temple/Masonry` | `/templemasonry/` | `/temple-masonry/` |

Not triggered by any current page — all 867 names are letters, digits, spaces and hyphens, where the
two agree. It would have bitten on the first page titled with an apostrophe. Fixed; the test file
locks the rule in.

**2 · Every static page would have lost its layout and menu styling.**
The site is entirely CSS driven, and the rules that do the driving live in the **SPA bundle**:
`PageContainer.css` (`.articleContents` padding, all 20 `.layout-*` grids) and
`NestedDynamicMenu.css`. A static page never loads that bundle, and `theme.css` is only colours and
fonts. So every page would have rendered edge-to-edge with no padding, multi-article pages would
have lost their grids, and the nav would have been an unstyled bulleted list.

Fixed by extracting `infra/static/page-layout.css` as a **single source of truth**, embedded into
the C# renderer and read by the PowerShell backfill. Selectors were changed from `> div:nth-child()`
to `> :nth-child()` so they match the static renderer's semantic `<article>` children.

**3 · The backfill would have published every page with no header and no theme.**
`publish-static-pages.ps1` fetched `header.html` and `theme.css` over **unauthenticated HTTP** from
the content bucket. That bucket is not public — every request 403s — and the failure was swallowed,
returning empty strings. It now reads with AWS credentials, warns loudly, and **refuses to upload**
a site whose header or theme could not be read.

**4 · Every non-apologetics page would have had the wrong title.**
`$siteInfo.Title` was hard-coded to `'LDS Apologetics'` for all six sites, feeding `<title>`,
`og:site_name`, and the JSON-LD publisher. Now driven by the site table.

**5 · Google Analytics would have been dropped on four of six sites.**
Real GA4 ids exist in the React configs but were blank in both renderers:

| Site | GA4 id | Was |
|---|---|---|
| `cesletter.info` | `G-Z4XDMTTGRN` | missing |
| `ldsdoctrines.com` | `G-PY9E2KT5DC` | missing |
| `ldsdiscussions.info` | `G-G8VH9TBRNR` | missing |
| `ldsfaithincrisis.com` | `G-J6H714HFSM1` | missing |

Traffic would have stopped being recorded exactly when you most needed the numbers. Now synced
across the renderer, the backfill script, and `sites.json`, with a test asserting they match the
React configs.

> `ldsfaithincrisis` is `G-J6H714HFSM1` — the apologetics id with a `1` appended. Carried through
> as-is. **Worth confirming in the GA console that it is a real separate property and not a typo.**

**6 · The test project did not compile.** `MockWebsiteProcessing` was missing two interface members
added at some point. Pre-existing; fixed.

**7 · The per-site brand stylesheet was missing, and three sites have no theme at all.**
An audit of the content bucket found the real picture:

| Site | header.html (content) | theme.css | BaseStyles.css (public root) |
|---|---|---|---|
| `ldsfaithincrisis.com` | yes | 42 B | 850 B |
| `ldsdoctrines.com` | yes | **none** | 898 B |
| `reflectiverealizations.com` | yes | 5,753 B | — |
| `ldsapologetics.com` | yes | **none** | 898 B |
| `ldsdiscussions.info` | yes | 199 B | 2,664 B |
| `cesletter.info` | **none** | **none** | 4,981 B |

`BaseStyles.css` — body font, colour, line-height, `.headerContents`/`.menuContents` layout — is the
site's actual stylesheet, and it is compiled into the SPA bundle. For **ldsapologetics, ldsdoctrines
and cesletter it is the only site-wide CSS that exists**, since they have no `theme.css`.

`cesletter.info` also has no `header.html` in the content bucket; its header lives at the public
bucket root, published by the SPA build, which is why the renderer found nothing.

Both are now read from the public bucket root (where the SPA build puts them), with the header
falling back there when the content bucket has none. Cascade order is
`page-layout.css` → `BaseStyles.css` → `theme.css`, least specific first.

> **Operational consequence:** changing `BaseStyles.css` no longer propagates by itself, because it
> is inlined at render time. Re-render the site (`RegenerateAllStaticPages`) after a change.

The upload guard was also relaxed accordingly — a missing `theme.css` is normal and no longer blocks
a site; only a missing header, or *no* site CSS at all, does.

**8 · Preflight crashed on a one-page site.** `@($pages) | Where-Object {...}` array-wraps the
*input*, not the pipeline *result*, so a site with a single page returned a scalar and `.Count`
threw. It hit `reflectiverealizations` — which is first in the recommended migration order, so it
would have failed on the very first site. Fixed in three places (preflight coverage, migrate
`verify`, migrate `validate`).

### Preflight run across all six sites

| Site | Pages | Slug parity | Bucket + hosting | Distribution | Verdict |
|---|---|---|---|---|---|
| `ldsfaithincrisis` | 3 | 3/3 | ok | `E2WKUF9TS5EYS1` | GO |
| `ldsdoctrines` | 471 | 471/471 | ok | `E2DNZBB98HPVUX` | GO |
| `reflectiverealizations` | 1 | 1/1 | ok | `E12GCWKN2ZM95M` | GO |
| `ldsapologetics` | 142 | 142/142 | ok | `E31V7XOW14TK38` | GO |
| `ldsdiscussions` | 221 | 221/221 | ok | `E3VCKB95QSPEA6` | GO |
| `cesletter` | 29 | 29/29 | ok | `E156MTQRSC8VG6` | GO |

All six: website hosting enabled with `index.html`, distribution found, no CloudFront function
attached yet, **0 pages backfilled** (expected at this stage), root `index.html` still the ~0.7 KB
SPA shell.

Site-specific notes:

- **`ldsapologetics`** has an ACM certificate for `admin.ldsapologetics.com` in
  `PENDING_VALIDATION`, created 2026-07-28. Not stale and not a problem — `deploy-admin-subdomain.ps1`
  finds it, adds the validation CNAME via the GoDaddy PAT, and waits. ACM allows 72h.
- **`reflectiverealizations`** has exactly one page (Home), no `sitemenu.json`, and no React config
  set. Its root `index.html` is 8.3 KB — already substantive rather than an SPA shell. Effectively a
  one-page site; treat it as a smoke test, not a representative one.
- **Every** public distribution has **no custom error responses** configured. Missing paths fall
  through to the bucket's own error document. Unchanged by this migration, just worth knowing.

### Validated on the test bucket

The full 29-page cesletter render was published to `www.testwebsite.com` and served over its S3
website endpoint. **Clean URLs resolve with no CloudFront function attached:**

| URL | Result |
|---|---|
| `/` | 200 — static home |
| `/about/` | 200 — 30,707 bytes, fully styled |
| `/about` | 302 → `/about/` (S3 does this natively) |
| `/nonexistent-page/` | 404 |

This is direct proof of the property the zero-downtime plan rests on: after `backfill`, the new URLs
work in production while every old `?page=` link still works. The CloudFront function's remaining job
is the legacy `?page=` 301s, plus turning that 302 hop into a redirect-free internal rewrite.

**Also fixed:** the PowerShell backfill *linked* `theme.css` externally while the C# renderer
*inlines* it. A page would have visibly changed the first time an edit triggered the save-hook
re-render. Both now inline.

---

## Before you start — gather credentials

Do this **first**, in daylight. Every one of these has stopped a cutover mid-flight for someone.

| What | Why | How to get it |
|---|---|---|
| **AWS session** (`tbirdcontractinggmailcom`) | Everything — S3, CloudFront, ACM, and the Lambda deploy | Re-auth. Sessions expire mid-run; scripts are idempotent, so just re-run. It is the default everywhere, so `--profile` should never be needed |
| **`GODADDY_PAT`** | Automates the ACM validation CNAME and the `admin` CNAME | **Personal Access Token** from <https://developer.godaddy.com/keys>, **production** (not OTE), scope **`domains.dns:update`**. Without it the scripts print each record and wait while you add it by hand — workable, but slow at 2am, and you need two records per site × six sites |
| **`LAMBDA_API_BASE_URL`** | All content scripts | Stack output — see `CLAUDE.md` |
| **`MCP_API_KEY`** | All content scripts | Lambda env var — see `CLAUDE.md` |
| **GoDaddy account login** | Fallback if the token is refused | GoDaddy restricts API access on some account tiers; a `403` means manual mode |
| **Google Search Console access** | Submit sitemaps after each site | Confirm you can sign in for all six properties |
| **Bing Webmaster Tools access** | Same | |
| **Google Analytics access** | Verify the `ldsfaithincrisis` id (see bug 5) | |

Set the GoDaddy token once, permanently:

```powershell
[Environment]::SetEnvironmentVariable('GODADDY_PAT','<your token>','User')
$env:GODADDY_PAT = '<your token>'    # current shell too, so you need not restart
```

> A legacy `GODADDY_API_KEY`/`GODADDY_API_SECRET` pair still works — these scripts only call v1
> endpoints — but `sso-key` is discontinued after 2026. Prefer the PAT; if both are set it wins.

Then verify everything at once — this touches nothing and takes seconds:

```powershell
./scripts/preflight-cutover.ps1 -Site ldsapologetics
```

It reports tooling, credentials, CMS reachability, slug parity across every live page name, function
unit tests, bucket and distribution state, static page coverage, and admin subdomain status, then
prints **GO** or **NO-GO**.

---

## Stage 1 — API and admin subdomains

Nothing here is visible to visitors. The admin subdomain is **new** infrastructure; the public sites
are untouched.

### 1.1 Deploy the API

```powershell
cd api/WebApplicationArch
dotnet lambda deploy-serverless
```

Ships the on-save re-render hook and the clean-URL sitemap generator.

> **Risk: low.** The save hook is best-effort and never fails a save. Rollback is redeploying the
> previous commit.

### 1.2 Stand up each admin subdomain

```powershell
./scripts/deploy-admin-subdomain.ps1 -Site ldsapologetics
./scripts/deploy-admin-spa.ps1       -Site ldsapologetics
```

Creates a private bucket, ACM cert, OAC, noindex policy, CloudFront distribution, GoDaddy CNAME,
then builds and uploads the SPA. Both are idempotent.

**Confirm you can log in at `https://admin.{domain}/` before going near stage 2.**

> **Risk: low** — all new resources, nothing existing is modified.
> **Watch for:** ACM validation needs a DNS record. With `GODADDY_API_KEY`/`GODADDY_API_SECRET` set
> it is automatic; otherwise the script prints the record and waits. Certificates usually issue in
> 1–5 minutes.

---

## Stage 2 — Per-site static cutover

Driven by one script, in phases. **Each phase that changes live behaviour has a rollback.**

```powershell
./scripts/migrate-site-to-static.ps1 -Site ldsapologetics -Phase <phase>
```

### The property that makes this zero-downtime

Your public buckets have **S3 static website hosting** enabled, and website hosting already resolves
`/slug/` → `/slug/index.html` on its own. So after the backfill, **the new URLs are live and
verifiable in production while every old `?page=` link still works exactly as before.** Both worlds
run at once. Nothing is switched until you have proven the new one is good.

### Phase order, risk, and rollback

| # | Phase | What it does | Visitor impact | Rollback |
|---|---|---|---|---|
| 1 | `preflight` | Read-only readiness report | none | n/a |
| 2 | `backfill` | Upload `/slug/index.html` for every page | **none** — additive | delete the keys (or leave them) |
| 3 | `verify` | Fetch every clean URL over HTTPS, assert 200 + real content | **none** — read-only | n/a |
| 4 | `root` | Replace root `index.html`: SPA shell → static home | **visible** | `-Phase root -Rollback` |
| 5 | `routing` | Attach CloudFront function: `?page=` → 301 | **visible** | `-Phase routing -Rollback` |
| 6 | `sitemap` | Regenerate sitemaps at clean URLs | crawlers only | regenerate |
| 7 | `validate` | **The gate.** Automated post-cutover health check | none — read-only | n/a |

### Phase notes and specific risks

**2 · `backfill`** — Additive only. Nothing routes to these objects yet.
> **Risk: none to visitors.** Worst case is a badly rendered page sitting in a bucket nobody reads yet.

**3 · `verify`** — Fetches every page and fails if any returns non-200 or suspiciously small HTML
(which would mean the SPA shell, not a pre-rendered page).
> **This is the gate.** If it fails, stop. Do not run `routing` — every legacy link would 301 into
> a broken page.

**4 · `root`** — The only destructive step. The script downloads the current root object to
`dist/rollback/{site}/root-index.html`, refuses to continue if the backup is empty, then uploads and
invalidates.
> **Risk: the homepage.** Deep pages are unaffected.
> **Hard guard:** the script refuses to run unless `https://admin.{domain}/` returns 200. Replacing
> the root removes the SPA from `www` entirely — without the admin site live first, you lock
> yourself out of your own CMS.
> **Invalidation matters here.** The old SPA shell may have no `Cache-Control` at all, meaning
> CloudFront's default TTL of up to 24h. The script always invalidates `/` and `/index.html`.

**5 · `routing`** — The moment old URLs change behaviour.
> **Risk: all legacy inbound links at once.** Mitigated by phase 3 having already proven every 301
> target exists.
> **Rollback is not instant** — a CloudFront distribution update takes 5–10 minutes to propagate.
> Budget for that. A faster emergency lever, if you ever need one, is publishing a pass-through
> version of the *function* (seconds) rather than detaching it from the distribution.

**6 · `sitemap`** — Last on purpose. A sitemap full of URLs that 404 teaches crawlers to distrust
the site.

**7 · `validate`** — Automated checks: homepage is pre-rendered (not the shell), `?page=` returns a
301, an extensionless `/slug` resolves, the sitemap has no `?page=` URLs, public `robots.txt` does
not disallow, the admin site is live and sends `noindex`, and the rollback backup exists on disk.
Exits non-zero on any failure.
> **This is the gate between sites.** Do not start the next site until it passes and you have done
> the manual checks it prints.

### Recommended sequence for the first site

```powershell
./scripts/migrate-site-to-static.ps1 -Site ldsapologetics -Phase preflight
./scripts/migrate-site-to-static.ps1 -Site ldsapologetics -Phase backfill
./scripts/migrate-site-to-static.ps1 -Site ldsapologetics -Phase verify
# --- stop and look at a few pages in a browser ---
./scripts/migrate-site-to-static.ps1 -Site ldsapologetics -Phase root
# --- check the homepage ---
./scripts/migrate-site-to-static.ps1 -Site ldsapologetics -Phase routing
# --- check an old ?page= link 301s correctly ---
./scripts/migrate-site-to-static.ps1 -Site ldsapologetics -Phase sitemap
```

Once one site has proven the process, the rest can run `-Phase all`, which walks the phases with a
confirmation before each visible one.

### Site order

Smallest blast radius first:

| Order | Site | Pages | Why |
|---|---|---|---|
| 1 | `reflectiverealizations` | 1 | Trivial. Proves the pipeline end-to-end. **No config set yet — see below.** |
| 2 | `ldsfaithincrisis` | 3 | Still tiny |
| 3 | `cesletter` | 29 | Small, self-contained |
| 4 | `ldsapologetics` | 142 | The one you care most about; do it while you are fresh |
| 5 | `ldsdiscussions` | 221 | |
| 6 | `ldsdoctrines` | 471 | Largest; do it last with the process fully proven |

> `reflectiverealizations` has **no config set** in `react/baseProject/configs/`, so its
> `configPrefix` is `null` and `deploy-admin-spa.ps1` will refuse to build it. Either create the
> config files first or start at `ldsfaithincrisis`.

### After each site

Submit the sitemap in Google Search Console and Bing Webmaster Tools; URL-inspect one page to
confirm the rendered `<head>`; request indexing on the top pages.

---

## Stage 3 — Inline `reactcomponents` into the admin app

`@tbirdcomponents/reactcomponents@0.1.96` is a private npm package whose source already lives in
this repo at `react/reactcomponents/`. Publishing a package you will never reuse adds a release step
between editing a component and seeing it in the app, for no benefit.

**Scope, measured:**

| | |
|---|---|
| Source files in the library | 155 |
| Import sites in `baseProject/src` | 23 |
| Distinct symbols used | ~12 — `SearchGrid`, `HtmlEditor`, `HtmlContentRenderer`, `ModalDialog`, `ArticleModal`, `CreateEditPage`, `PageFormFields`, `NestedDynamicMenu`, `TreeView`, `ThemeBuilder`, `Login`/`SignUp`/`ForgotPassword*`/`VerificationCode`/`ResetPassword`, `blueTheme`, `availableCSSAttributes`, `isPromise`, `MENU_CONTEXT_TYPE` |

**Approach:** vendor `reactcomponents/src/components` into `baseProject/src/components/lib/`, rewrite
the 23 imports, move the library's dependencies into `baseProject/package.json`, drop the
`@tbirdcomponents` dependency, delete the build/publish step.

> **Risk: low but broad.** No behaviour change intended; the risk is a missed import or a dependency
> that only existed transitively via the package. `npm run build` catches nearly all of it.
> **Do this after stage 2 is stable.** It has no user-visible benefit, so it does not belong on the
> cutover's critical path.

---

## Stage 4 — Remove the database

### What the DB still does after stage 2

Nothing on the read path. The public sites are static files. MySQL now serves only the admin SPA and
the MCP tools, holding: `website`, `page`, `article` (metadata — the HTML is already in S3),
`keyword`, `topic`, `layout`, `collection`, plus relationship tables.

That is a few hundred rows per site, single-writer, read-mostly, sitting on a server billed by the
hour.

### Target shape

Article HTML stays exactly where it is. The indexes move to S3 alongside it:

```
s3://www-websitecontent/public/websites/{domain}/
    articles/{path}            <- unchanged, the actual HTML
    _cms/site.json             <- the website record
    _cms/pages.json            <- page records + article links
    _cms/articles.json         <- article metadata
    _cms/taxonomy.json         <- keywords + topics
    _cms/collections.json
    _cms/layouts.json
```

Lambda stays — it is still the auth boundary, the API for the admin SPA and MCP, and the host of the
on-save static re-render hook. **"No database" does not mean "no Lambda."**

### The one thing that must be solved first

**Concurrent writes.** You run parallel agents that create pages in bulk — 77 CFM pages in a single
pass. A naive read-modify-write on `pages.json` is last-write-wins: two agents writing at once
silently lose each other's records. Two ways to make it safe:

1. **S3 conditional writes.** `PutObject` with `If-Match: {etag}` gives compare-and-swap; on a 412,
   re-read and retry. No new infrastructure.
2. **DynamoDB for the indexes.** On-demand pricing is effectively free at this volume, conditional
   writes are built in, there is no idle cost, no connection pooling (a genuine Lambda/MySQL pain),
   and no publicly reachable database.

Recommendation: **DynamoDB.** It removes the RDS bill and the concurrency question in one move, and
costs near nothing at 867 records.

### Sequence

| Step | Action | Risk |
|---|---|---|
| 4.1 | Introduce an `IContentStore` interface behind the existing DAOs | none — refactor only |
| 4.2 | Implement the S3/DynamoDB store alongside MySQL | none — not wired up |
| 4.3 | One-shot export of all six sites; diff against MySQL until identical | none — read-only |
| 4.4 | Flip reads to the new store behind an env var; keep writing to both | **reversible** — flip the var back |
| 4.5 | Run dual for about a week, watching for drift | low |
| 4.6 | Stop writing to MySQL | reversible while the instance exists |
| 4.7 | Final snapshot, then **delete** the RDS instance | snapshot is the rollback |

> **Do not "stop" the RDS instance as your pause** — AWS auto-restarts a stopped instance after 7
> days, and you will be billed again without noticing. Take a final snapshot and delete the
> instance; snapshots cost almost nothing and are a real rollback.

### The alternative worth weighing

`e:\dev\webappSimple\` is already a DB-free, S3-native CMS — Node.js Lambda, React admin, static
publisher. Stage 4 is, in effect, rebuilding what that project already is.

Before committing to steps 4.1–4.7, compare honestly: **migrate content into `webappSimple` and
retire this stack**, versus **strip the DAL out of this one**. The second keeps the C# renderer,
the MCP server, and the admin UI you already know. The first is less total code to own. That is a
real fork in the road and worth an explicit decision rather than drifting into one.

---

## Stage 5 — N-depth menu nesting

**Public renderers done 2026-08-12 (not yet deployed). Admin UI deliberately deferred.**

The data model never had a depth limit: `sitemenu.json` items carry a `parent` id, so the tree has
always been fully general. Only the renderers were flat.

### What changed

| Piece | Change |
|---|---|
| `StaticPageRenderer.BuildNav` | Now a recursive walk over a parent→children map, public so it can be tested directly. `MaxNavDepth = 5`. Every id renders at most once, so a self-parenting or duplicated item is skipped rather than recursed into |
| `Build-MenuNav` (PowerShell) | Split into `Build-MenuNav` + recursive `Build-MenuLevel`, byte-identical output to the C# renderer. `Get-MenuParentId` was added so an item written with no `parent` property is top-level in *both* renderers |
| `page-layout.css` | Depth-agnostic submenu rules (`.menuContents ul ul`, `li:hover > ul`). Level 1 drops below its parent; level 2+ flies out sideways. A `:has(> ul)` chevron marks parents that have a flyout |
| `navigation.js` (MCP) | `MAX_MENU_DEPTH` matching the renderer. Refuses a placement the renderer would truncate, an item parented to itself, and a move under an own descendant. Moving a subtree is measured by its **deepest** descendant |
| `StaticRenderNavTests` | 22 tests: depth, ordering, cycles, duplicate ids, malformed JSON, plus drift checks pinning the PowerShell cap and the MCP cap to `MaxNavDepth` |

Two bugs fell out of the old two-loop version and are fixed with it:

- A depth-1 item without a `pageName` was dropped **along with everything beneath it**. Group
  headings are exactly that shape, so most three-level menus would have lost a whole branch. An
  item is now kept if it has a page *or* has children.
- A top-level item's grandchildren were dropped silently — no log, no error, nothing to notice.

### Still flat, on purpose

The **admin UI** was left at two levels: `DynamicMenu` renders leaves without recursing, and
`TreeView`'s "Add Menu Item" parents a new node to the *selected node's parent* when the selection
is itself a child, so depth 3 is reachable only by dragging. The consequence is limited to the
admin's own preview — every public site serves prerendered HTML. Menus are administered through the
MCP tools, which nest to the full five levels.

### Known landmine, untouched

`PageContainer.handleSaveMenu` writes the **nested** `menuItems` tree straight back to
`sitemenu.json` with no flatten step, so items saved from the in-page menu editor lose their
`parent` field entirely. `BuildNav` reads `parent ?? "0"`, so on the next render every item becomes
a top-level section and every child disappears. This predates the depth work and is unrelated to it,
but going three levels deep makes the blast radius bigger. Fix before that editor is used again.

> **Risk: low, but sitewide.** Every page carries the nav.
> **Not yet deployed.** Deploying the Lambda re-renders on the next `sitemenu.json` write; edit one
> site's menu and eyeball it before touching the rest.
> **Rollback:** redeploy the previous commit; the menu files themselves are unchanged.

---

## Rollback summary

| If this breaks | Do this | Time to recover |
|---|---|---|
| A backfilled page renders wrong | Fix content, re-run `backfill` | minutes |
| Homepage looks wrong after `root` | `-Phase root -Rollback` | ~2 min (invalidation) |
| Legacy links broken after `routing` | `-Phase routing -Rollback` | 5–10 min (CloudFront) |
| Admin site broken | Re-run `deploy-admin-spa.ps1` | ~5 min |
| API deploy bad | Redeploy previous commit | ~5 min |
| DB migration wrong (stage 4) | Flip the env var back; restore snapshot | minutes / hours |

---

## Reference

- [`scripts/README.md`](scripts/README.md) — full script documentation, prerequisites, troubleshooting
- [`scripts/sites.json`](scripts/sites.json) — the site registry
- [`ApplicationDocumentation.txt`](ApplicationDocumentation.txt) — original manual public-site setup
- [`CLAUDE.md`](CLAUDE.md) — MCP setup, per-site article styling, `WEBSITE_ID` table

### Still open

- The SPA emits `?page=Slug` for internal links. On `admin.{domain}` those keep the reader inside
  the admin app instead of sending them to the public clean URL. Point the React nav and canonical
  links at `https://www.{domain}/{slug}/`.
- No script yet for creating the **public** side of a brand-new site (buckets, website hosting,
  policy, CORS, cert, distribution, DNS). Still the manual process in `ApplicationDocumentation.txt`.
- `reflectiverealizations` has no React config set.
