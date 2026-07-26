# Static Prerender + Admin Subdomain — Rollout Runbook

Branch: `feature/static-prerender`

## Why
The public sites run as a client-rendered React SPA (create-react-app). Every URL returns the
same near-empty shell at `?page=Slug`, so crawlers, social scrapers, and AI bots see no content.
`site:ldsapologetics.com` returns only the homepage. The fix: serve **pre-rendered static HTML**
at clean paths from the public bucket, and move the React app to an **admin subdomain**.

## Target architecture
| | Admin site (React SPA) | Public site (static HTML) |
|---|---|---|
| Host | `admin.{domain}` (Cognito-gated, `noindex`) | `www.{domain}` / root |
| Content | client-rendered, talks to the API | pre-rendered, content in the HTML |
| URLs | its own routing | clean paths `/slug/` |

## What's already built (this branch)
- **`StaticPageRenderer.cs`** — assembles a full document (title, meta description, canonical,
  OG/Twitter, JSON-LD, inlined theme, clean-URL nav, article body). Single source of render truth.
- **On-save hook** in `ApiArticleFunctions.SetArticleContent` — re-renders every served page that
  references a saved article. Best-effort; never fails the save.
- **`RegenerateAllStaticPages`** — server-side bulk backfill method (not on a sync API route; 29s
  API Gateway cap. Use the PS script or a direct Lambda invoke).
- **Clean-URL sitemap** — `RegenerateSitemap` (C#) and `regenerate-sitemaps.ps1` emit `/slug/`.
- **`publish-static-pages.ps1`** — local reference renderer + bulk backfill (`-NoUpload` default).
- **`infra/cloudfront/public-clean-urls.js`** — CloudFront Function mapping `/slug` -> `/slug/index.html`.
- **`infra/admin/robots.txt`** — `Disallow: /` for the admin subdomain.

Verified locally: `dotnet build` = 0 errors; 140 ldsapologetics pages render.

## Decisions locked
- Theme is **inlined** into each static page (self-contained; no cross-bucket public asset).
- Admin lives on **`admin.{domain}`** (this decision).

## Box checklist (AWS — do in order)
1. **Re-auth AWS** for the deploy/upload profile.
2. **Deploy the API**: from `api/WebApplicationArch/` — `dotnet lambda deploy-serverless --profile Admin`.
   Ships the on-save hook and the clean-URL sitemap.
3. **Backfill static pages**: `./scripts/publish-static-pages.ps1 -Site ldsapologetics -Upload`.
   Spot-check `https://www.ldsapologetics.com/temple-and-masonry/`.
4. **Public distribution routing**: attach `infra/cloudfront/public-clean-urls.js` as a CloudFront
   Function on the PUBLIC distribution's default behavior (viewer-request). Confirm `/slug` resolves.
5. **Stand up the admin subdomain**:
   - S3 bucket + CloudFront distro for `admin.{domain}`; ACM cert (us-east-1) covering it; Route53
     alias.
   - Deploy the React SPA build to the admin bucket; upload `infra/admin/robots.txt` to its root and
     add an `X-Robots-Tag: noindex, nofollow` response-headers policy.
   - Cognito: add `https://admin.{domain}/...` to the app client's allowed callback + logout URLs.
   - Point the React config's public links (`?page=` deep links, canonical) at the clean public URLs.
6. **Swap the public root**: publish the static home (`Home` page) to the public bucket root and make
   the public distribution serve static (not the SPA fallback).
7. **Regenerate the sitemap LAST** (after pages are live) so crawlers don't hit 404s:
   `./scripts/regenerate-sitemaps.ps1 -Site ldsapologetics`.
8. **Search Console + Bing**: submit the sitemap, URL-inspect a page to confirm the rendered `<head>`,
   request indexing on top pages.

## Repeat for the other five sites
Same steps per `WEBSITE_ID` / domain (ids: 1 faithincrisis, 2 doctrines, 4 reflective, 5 apologetics,
6 discussions, 8 cesletter). `SiteMeta` in `StaticPageRenderer.cs` and the `$sites` table in the PS
scripts already list all six; fill in each site's public title/analytics tag.
