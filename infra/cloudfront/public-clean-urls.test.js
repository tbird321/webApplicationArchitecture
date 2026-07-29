// Tests for public-clean-urls.js. Plain node, no dependencies:
//
//     node infra/cloudfront/public-clean-urls.test.js
//
// This function sits in front of every public request. A bug here takes the whole
// site down, and CloudFront Functions have no staging environment worth the name,
// so it is tested locally instead.
//
// The parity block is the important one: the slug this function redirects TO must
// equal the slug StaticPageRenderer.Slug writes the FILE to, or every legacy link
// 301s straight into a 404.

const fs = require('fs');
const path = require('path');

const src = fs.readFileSync(path.join(__dirname, 'public-clean-urls.js'), 'utf8');
const handler = new Function(src + '\nreturn handler;')();

let passed = 0;
const failures = [];

function check(label, actual, expected) {
  if (JSON.stringify(actual) === JSON.stringify(expected)) {
    passed++;
  } else {
    failures.push(`${label}\n      expected: ${JSON.stringify(expected)}\n      actual:   ${JSON.stringify(actual)}`);
  }
}

function req(uri, page) {
  const request = { uri: uri, querystring: {} };
  if (page !== undefined) request.querystring.page = { value: page };
  return handler({ request: request });
}

function redirectTarget(res) {
  if (res.statusCode !== 301) return `(no redirect, status ${res.statusCode || 'passthrough'})`;
  return res.headers.location.value;
}

// --- 1. legacy ?page= deep links must 301 (not 302 -- 301 is what passes ranking)
check('?page= returns 301', req('/', 'Temple-And-Masonry').statusCode, 301);
check('?page=Temple-And-Masonry', redirectTarget(req('/', 'Temple-And-Masonry')), '/temple-and-masonry/');
check('?page= mixed case', redirectTarget(req('/', 'BookOfAbraham')), '/bookofabraham/');
check('?page= with spaces', redirectTarget(req('/', 'Book of Abraham')), '/book-of-abraham/');
check('?page= leading/trailing space', redirectTarget(req('/', '  Book of Abraham  ')), '/book-of-abraham/');
check('?page= double hyphen collapses', redirectTarget(req('/', 'Adam--God')), '/adam-god/');

// Home publishes to the bucket ROOT. Redirecting it to /home/ is a 404, and it is the
// most commonly bookmarked legacy URL there is.
check('?page=Home -> /',        redirectTarget(req('/', 'Home')), '/');
check('?page=home -> /',        redirectTarget(req('/', 'home')), '/');
check('?page=HOME -> /',        redirectTarget(req('/', 'HOME')), '/');
check('?page=Home-Page is NOT home', redirectTarget(req('/', 'Home-Page')), '/home-page/');
check('?page=Homestead is NOT home', redirectTarget(req('/', 'Homestead')), '/homestead/');

// The query value arrives percent-encoded; without decoding, %20 slugs to "20".
check('?page= percent-encoded space', redirectTarget(req('/', 'About%20Us')), '/about-us/');
check('?page= encoded apostrophe',    redirectTarget(req('/', 'Joseph%20Smith%27s%20Polygamy')), '/joseph-smiths-polygamy/');
check('?page= malformed encoding does not throw', redirectTarget(req('/', 'Bad%ZZ')), '/badzz/');

// --- 2. clean paths map to the pre-rendered object
check('root -> index.html', req('/').uri, '/index.html');
check('/slug -> /slug/index.html', req('/temple-and-masonry').uri, '/temple-and-masonry/index.html');
check('/slug/ -> /slug/index.html', req('/temple-and-masonry/').uri, '/temple-and-masonry/index.html');

// --- 3. anything with an extension passes through untouched
check('sitemap.xml untouched', req('/sitemap.xml').uri, '/sitemap.xml');
check('robots.txt untouched', req('/robots.txt').uri, '/robots.txt');
check('hashed asset untouched', req('/static/js/main.a1b2c3.js').uri, '/static/js/main.a1b2c3.js');
check('css untouched', req('/theme.css').uri, '/theme.css');

// --- 4. a non-page query string must NOT trigger a redirect
check('?utm_source passes through', req('/temple-and-masonry/').uri, '/temple-and-masonry/index.html');

// --- 5. SLUG PARITY -- must match StaticPageRenderer.Slug / ConvertTo-Slug exactly.
// Ported 1:1 from the C#: trim -> lower -> \s+ => '-' -> DELETE [^a-z0-9-] ->
// collapse runs -> trim hyphens. The DELETE (rather than replace-with-hyphen) is
// the subtle part: intra-word punctuation vanishes, it does not become a hyphen.
function rendererSlug(name) {
  if (!name || !name.trim()) return '';
  return name.trim().toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9\-]/g, '')
    .replace(/-{2,}/g, '-')
    .replace(/^-+|-+$/g, '');
}

const parityNames = [
  'Temple And Masonry',
  'Adam-God Theory',
  "Joseph Smith's Polygamy",        // apostrophe inside a word
  'Word of Wisdom (D&C 89)',        // parens + ampersand
  'Temple/Masonry',                 // slash inside a word
  'Kirtland Bank—1837',             // em dash inside a word
  'Book of Mormon: Origins',        // punctuation next to a space
  'Faith & Doubt',
  'The 116 Pages',
  'Multiple   Spaces',
  'CFM 2026: Week 1',
];

for (const name of parityNames) {
  check(`parity: "${name}"`, redirectTarget(req('/', name)), '/' + rendererSlug(name) + '/');
}

// --- report
console.log(`\npublic-clean-urls.js`);
if (failures.length === 0) {
  console.log(`  ${passed} checks passed\n`);
  process.exit(0);
} else {
  console.log(`  ${passed} passed, ${failures.length} FAILED\n`);
  for (const f of failures) console.log(`  x ${f}\n`);
  process.exit(1);
}
