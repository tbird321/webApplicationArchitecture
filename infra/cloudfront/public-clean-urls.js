// CloudFront Function (viewer-request) for the PUBLIC static site distribution
// (www.{domain}). Maps clean, path-based URLs to the pre-rendered index.html objects
// so the static pages produced by StaticPageRenderer / publish-static-pages.ps1 resolve:
//
//   /                        -> /index.html            (static home)
//   /temple-and-masonry      -> /temple-and-masonry/index.html
//   /temple-and-masonry/     -> /temple-and-masonry/index.html
//   /sitemap.xml             -> unchanged (has an extension)
//   /assets/x.css            -> unchanged (has an extension)
//
// Deploy: create a CloudFront Function, associate it with the PUBLIC distribution's
// default cache behavior on the "viewer request" event. Do NOT attach it to the admin
// distribution (that one serves the SPA and should keep SPA fallback routing).
//
// Note: legacy ?page=Slug links still work — the SPA (now on the admin subdomain) is not
// what serves these, and the root static index can carry a tiny redirect if you want to
// forward old ?page= deep links to /slug/. Canonical tags already point at the clean URL.
function handler(event) {
    var request = event.request;
    var uri = request.uri;

    if (uri.endsWith('/')) {
        request.uri = uri + 'index.html';
    } else {
        // No file extension in the last path segment -> treat as a page directory.
        var lastSegment = uri.substring(uri.lastIndexOf('/') + 1);
        if (lastSegment.indexOf('.') === -1) {
            request.uri = uri + '/index.html';
        }
    }
    return request;
}
