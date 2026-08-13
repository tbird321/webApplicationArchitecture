// Guards on the menu tools, exercised against a stub of the C# API. No network.
//
// Menu items are addressed positionally (/menu/{websiteId}/item/{id}) so there is no owner
// field to compare — assertSameSite cannot protect them. The protection comes from the page
// each item references, which is what actually went wrong on 2026-07-29: an update meant for
// ldsdiscussions (6) ran against ldsapologetics (5) and repointed item 110 at a page that
// does not exist there. Note an item-existence check would NOT have caught that: item 110
// existed on site 5.

import test from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';

const posted = [];
const deleted = [];

// site 5 menu — mirrors the shape get_menu returns
const MENU_5 = [
    { id: 20, parent: 0,  droppable: true,  text: 'Church History' },
    { id: 110, parent: 20, droppable: false, text: "Joseph Smith's Legal Troubles", pageId: 900, pageName: 'Legal-Troubles' },
    { id: 30, parent: 0,  droppable: true,  text: 'Book of Mormon' },
    { id: 31, parent: 30, droppable: false, text: 'BOM Witnesses', pageId: 1058, pageName: 'BOM-Witnesses' },
    { id: 32, parent: 31, droppable: false, text: 'Nested Leaf', pageId: 1059, pageName: 'Nested' },

    // A chain that runs to the render limit: 40 is a top-level section (depth 0) and 44 is
    // depth 4, the deepest level BuildNav draws. Anything below 44 exists in the file and
    // appears in no page's navigation, which is what these tools refuse to create.
    { id: 40, parent: 0,  droppable: true,  text: 'D0' },
    { id: 41, parent: 40, droppable: true,  text: 'D1' },
    { id: 42, parent: 41, droppable: true,  text: 'D2' },
    { id: 43, parent: 42, droppable: true,  text: 'D3' },
    { id: 44, parent: 43, droppable: false, text: 'D4', pageId: 1058, pageName: 'BOM-Witnesses' }
];

// pages that exist on site 5 only
const PAGES_5 = {
    900:  { id: 900,  name: 'Legal-Troubles', websiteId: 5 },
    1058: { id: 1058, name: 'BOM-Witnesses',  websiteId: 5 },
    1057: { id: 1057, name: 'Source-Documents-Translation', websiteId: 5 }
};

const server = http.createServer((req, res) => {
    let body = '';
    req.on('data', c => (body += c));
    req.on('end', () => {
        res.setHeader('Content-Type', 'application/json');
        const url = req.url;

        if (req.method === 'GET' && url === '/website') {
            return res.end(JSON.stringify([
                { id: 2, name: 'ldsdoctrines.com' },
                { id: 5, name: 'ldsapologetics.com' }
            ]));
        }
        if (req.method === 'GET' && /^\/menu\/5$/.test(url)) return res.end(JSON.stringify(MENU_5));
        if (req.method === 'GET' && /^\/menu\/\d+$/.test(url)) return res.end(JSON.stringify([]));

        const pg = url.match(/^\/page\/(\d+)\/(\d+)$/);
        if (req.method === 'GET' && pg) {
            const [, pageId, site] = pg;
            const found = site === '5' ? PAGES_5[pageId] : null;
            return res.end(JSON.stringify(found || { id: null, websiteId: null, articles: [] }));
        }
        if (req.method === 'POST') { posted.push({ url, body: body ? JSON.parse(body) : {} }); return res.end('{}'); }
        if (req.method === 'DELETE') { deleted.push(url); return res.end('{}'); }

        res.statusCode = 404;
        res.end('{}');
    });
});

await new Promise(r => server.listen(0, '127.0.0.1', r));
server.unref();

process.env.LAMBDA_API_BASE_URL = `http://127.0.0.1:${server.address().port}`;
process.env.MCP_API_KEY = 'test';
process.env.WEBSITE_ID = '5';

const { setWebsiteId } = await import('../src/apiClient.js');
setWebsiteId('5');
const { callTool } = await import('../src/dispatch.js');

// --- the incident ------------------------------------------------------------------------

test('repointing an item at a page that is not on this site is refused', async () => {
    posted.length = 0;
    // Page 164 lives on site 2. This is the 2026-07-29 shape: right item id, wrong site.
    const r = await callTool('update_menu_item', { id: 110, pageId: 164 });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /page 164 does not exist on ldsapologetics\.com \(site 5\)/);
    assert.equal(posted.length, 0, 'nothing may be written when the page does not resolve');
});

test('adding an item linked to a foreign page is refused', async () => {
    posted.length = 0;
    const r = await callTool('add_menu_item', { parentId: 20, itemTitle: 'Whatever', pageId: 164 });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /does not exist on ldsapologetics\.com/);
    assert.equal(posted.length, 0);
});

// --- pageName is resolved, not trusted ---------------------------------------------------

test('pageName comes from the page record, not the caller', async () => {
    posted.length = 0;
    const r = await callTool('add_menu_item', {
        parentId: 20, itemTitle: 'Source Documents', pageId: 1057, pageName: 'WRONG-NAME'
    });
    assert.equal(r.isError, false, r.content[0].text);
    const sent = posted.find(p => p.url === '/menu/5/item');
    assert.equal(sent.body.pageName, 'Source-Documents-Translation');
});

// --- structural checks --------------------------------------------------------------------

test('adding under a parent that does not exist is refused', async () => {
    posted.length = 0;
    const r = await callTool('add_menu_item', { parentId: 999, itemTitle: 'Orphan', pageId: 1057 });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /no menu item 999/);
    assert.equal(posted.length, 0);
});

test('updating an item that does not exist on this site is refused', async () => {
    posted.length = 0;
    const r = await callTool('update_menu_item', { id: 4242, itemTitle: 'Nope' });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /no menu item 4242 on ldsapologetics\.com/);
    assert.equal(posted.length, 0);
});

// --- nesting depth --------------------------------------------------------------------------
//
// The renderer TRUNCATES a menu that is too deep; these tools REFUSE to create one. The
// difference matters: a truncated item is a real page, linked from sitemenu.json, that appears
// in the nav of no page at all — and nothing anywhere reports it.

test('nesting a section under another section is allowed', async () => {
    posted.length = 0;
    // Item 31 is already a child, so this lands at level 3 — the case the old two-level
    // renderer silently dropped.
    const r = await callTool('add_menu_item', { parentId: 31, itemTitle: 'Three Witnesses', pageId: 1057 });
    assert.equal(r.isError, false, r.content[0].text);
    assert.equal(posted.filter(p => p.url === '/menu/5/item').length, 1);
});

test('adding at the deepest rendered level is allowed', async () => {
    posted.length = 0;
    const r = await callTool('add_menu_item', { parentId: 43, itemTitle: 'Still Visible', pageId: 1057 });
    assert.equal(r.isError, false, r.content[0].text);
    assert.equal(posted.filter(p => p.url === '/menu/5/item').length, 1);
});

test('adding below the deepest rendered level is refused, and names the parent chain', async () => {
    posted.length = 0;
    const r = await callTool('add_menu_item', { parentId: 44, itemTitle: 'Invisible', pageId: 1057 });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /would sit at level 6/);
    assert.match(r.content[0].text, /D0 > D1 > D2 > D3 > D4/);
    assert.equal(posted.length, 0, 'nothing may be written that renders nowhere');
});

test('moving a subtree is measured by its DEEPEST item, not the item moved', async () => {
    posted.length = 0;
    // Item 30 carries 31 -> 32 beneath it. Landing 30 at level 4 would put 32 at level 6.
    const r = await callTool('update_menu_item', { id: 30, parentId: 42 });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /everything beneath it/);
    assert.equal(posted.length, 0);
});

test('moving that same subtree somewhere it fits is allowed', async () => {
    posted.length = 0;
    const r = await callTool('update_menu_item', { id: 30, parentId: 41 });
    assert.equal(r.isError, false, r.content[0].text);
    assert.equal(posted.filter(p => p.url === '/menu/5/item/30').length, 1);
});

// --- cycles ---------------------------------------------------------------------------------
//
// A cycle does not corrupt the file or throw — it quietly detaches the branch from the root,
// so every page beneath it disappears from the navigation of every page at once.

test('making an item its own parent is refused', async () => {
    posted.length = 0;
    const r = await callTool('update_menu_item', { id: 30, parentId: 30 });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /cannot be its own parent/);
    assert.equal(posted.length, 0);
});

test('moving an item under its own descendant is refused', async () => {
    posted.length = 0;
    const r = await callTool('update_menu_item', { id: 30, parentId: 32 });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /sits BENEATH item 30/);
    assert.equal(posted.length, 0);
});

// --- destructive delete -------------------------------------------------------------------

test('deleting a section with children is refused without includeChildren', async () => {
    deleted.length = 0;
    const r = await callTool('delete_menu_item', { id: 30 });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /2 item\(s\) beneath it/);      // 31 and its child 32
    assert.match(r.content[0].text, /BOM Witnesses/);
    assert.equal(deleted.length, 0, 'nothing may be deleted');
});

test('deleting a section with children succeeds when explicitly confirmed', async () => {
    deleted.length = 0;
    const r = await callTool('delete_menu_item', { id: 30, includeChildren: true });
    assert.equal(r.isError, false, r.content[0].text);
    assert.equal(deleted.length, 1);
    assert.match(JSON.parse(r.content[0].text).summary, /along with 2 item\(s\)/);
});

test('deleting a childless leaf needs no confirmation', async () => {
    deleted.length = 0;
    const r = await callTool('delete_menu_item', { id: 110 });
    assert.equal(r.isError, false, r.content[0].text);
    assert.equal(deleted.length, 1);
});

// --- results name the site ----------------------------------------------------------------

test('a successful update reports the site, and what the item was before', async () => {
    posted.length = 0;
    const r = await callTool('update_menu_item', { id: 110, pageId: 1057 });
    assert.equal(r.isError, false, r.content[0].text);
    const summary = JSON.parse(r.content[0].text).summary;
    assert.match(summary, /ldsapologetics\.com \(site 5\)/);
    assert.match(summary, /Was "Joseph Smith's Legal Troubles"/);
    assert.match(summary, /page 1057/);
});

test.after(() => server.close());
