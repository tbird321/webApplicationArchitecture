// CloudFront Function (viewer-request) for the PUBLIC static site distribution
// (www.{domain}). Two jobs:
//
// 1. BACKWARD COMPAT: 301-redirect legacy SPA deep links so old bookmarks, shared links,
//    and already-indexed URLs keep working and pass their SEO value to the new clean URL:
//      /?page=Temple-And-Masonry  -> 301 /temple-and-masonry/
//
// 2. Map clean, path-based URLs to the pre-rendered index.html objects:
//      /                        -> /index.html            (static home)
//      /temple-and-masonry      -> /temple-and-masonry/index.html
//      /temple-and-masonry/     -> /temple-and-masonry/index.html
//      /sitemap.xml             -> unchanged (has an extension)
//      /assets/x.css            -> unchanged (has an extension)
//
// Deploy: create a CloudFront Function, associate it with the PUBLIC distribution's
// default cache behavior on the "viewer request" event. Do NOT attach it to the admin
// distribution (that one serves the SPA and should keep SPA fallback routing). Attach it
// after the static pages have been backfilled, so rewritten paths resolve.
//
// The slug transform must match StaticPageRenderer.Slug (lowercase, non-alphanumerics
// collapsed to single hyphens, trimmed).
function handler(event) {
    var request = event.request;
    var uri = request.uri;
    var qs = request.querystring;

    // 1) Legacy ?page=Slug -> 301 to the clean path.
    if (qs && qs.page && qs.page.value) {
        var slug = qs.page.value
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '');
        return {
            statusCode: 301,
            statusDescription: 'Moved Permanently',
            headers: { 'location': { value: '/' + slug + '/' } }
        };
    }

    // 2) Clean path -> index.html object.
    if (uri.endsWith('/')) {
        request.uri = uri + 'index.html';
    } else {
        var lastSegment = uri.substring(uri.lastIndexOf('/') + 1);
        if (lastSegment.indexOf('.') === -1) {
            request.uri = uri + '/index.html';
        }
    }
    return request;
}
