// Integration test for update_page against a stub of the C# API.
//
// This exists because of a real, live data-corruption bug: saving a page RE-UPSERTS every
// article attached to it, and the article list nested inside a page read is a partial
// projection (it omits websiteId, and omitted status until 2026-08-02). Passing that
// projection back into the save rewrote each article with websiteId 0 and status 'draft',
// orphaning it from its site and unpublishing it.
//
// No network: LAMBDA_API_BASE_URL points at a throwaway localhost server, so this asserts on
// the exact body update_page would send to production.

import test from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';

// --- stub API ---------------------------------------------------------------------------

const posted = [];

// The nested projection a page read returns: NO websiteId, and historically no status.
const PAGE = {
    id: 164,
    websiteId: 2,
    name: 'Apostasy',
    description: 'The Path of Apostasy',
    status: 'published',
    style: 'Basic',
    layout: '2, 1 Grid',
    layoutid: 41,
    keywords: [],
    topics: [],
    articles: [
        { id: 185, sequence_no: 5,  name: 'Apostasy',         articlePath: 'Apostasy.html',         websiteId: 0, status: null },
        { id: 186, sequence_no: 10, name: 'Apostasy Text',    articlePath: 'Apostasy-Text.html',    websiteId: 0, status: null }
    ]
};

// The full records a direct article read returns — correct websiteId and status.
const ARTICLES = {
    185: { id: 185, sequence_no: 5,  articleId: 'apostasy',      name: 'Apostasy',      description: 'a', articlePath: 'Apostasy.html',      memeImagePath: '', websiteId: 2, status: 'published', keywords: [], topics: [] },
    186: { id: 186, sequence_no: 10, articleId: 'apostasy-text', name: 'Apostasy Text', description: 'b', articlePath: 'Apostasy-Text.html', memeImagePath: '', websiteId: 2, status: 'published', keywords: [], topics: [] }
};

const server = http.createServer((req, res) => {
    let body = '';
    req.on('data', c => (body += c));
    req.on('end', () => {
        const url = req.url;
        res.setHeader('Content-Type', 'application/json');

        if (req.method === 'GET' && /^\/page\/164\//.test(url)) {
            return res.end(JSON.stringify(PAGE));
        }
        const art = url.match(/^\/article\/(\d+)$/);
        if (req.method === 'GET' && art) {
            const a = ARTICLES[art[1]];
            return res.end(a ? JSON.stringify(a) : 'null');
        }
        if (req.method === 'POST') {
            posted.push({ url, body: body ? JSON.parse(body) : {} });
            if (url === '/page') return res.end(JSON.stringify({ ...PAGE, id: 164 }));
            return res.end('{}');
        }
        res.statusCode = 404;
        res.end('{}');
    });
});

await new Promise(r => server.listen(0, '127.0.0.1', r));
const port = server.address().port;
// Do not let the stub hold the event loop open once the assertions are done.
server.unref();

process.env.LAMBDA_API_BASE_URL = `http://127.0.0.1:${port}`;
process.env.MCP_API_KEY = 'test';
process.env.WEBSITE_ID = '2';

const { setWebsiteId } = await import('../src/apiClient.js');
setWebsiteId('2');
const { callTool } = await import('../src/dispatch.js');

// --- tests ------------------------------------------------------------------------------

test('update_page never sends the partial nested article projection back', async () => {
    posted.length = 0;
    const r = await callTool('update_page', { id: 164, description: 'changed' });
    assert.equal(r.isError, false, r.content?.[0]?.text);

    const save = posted.find(p => p.url === '/page');
    assert.ok(save, 'no page save was posted');

    for (const a of save.body.articles) {
        assert.equal(a.websiteId, 2, `article ${a.id} would have been written with websiteId ${a.websiteId}`);
        assert.equal(a.status, 'published', `article ${a.id} would have been written as ${a.status}`);
        assert.ok(a.articlePath, `article ${a.id} lost its articlePath`);
        assert.ok(a.articleId, `article ${a.id} lost its slug`);
    }
});

test('update_page preserves the page status rather than defaulting to draft', async () => {
    posted.length = 0;
    await callTool('update_page', { id: 164, description: 'changed again' });
    const save = posted.find(p => p.url === '/page');
    assert.equal(save.body.status, 'published');
});

test('update_page keeps article order and sequence numbers', async () => {
    posted.length = 0;
    await callTool('update_page', { id: 164, name: 'Apostasy' });
    const save = posted.find(p => p.url === '/page');
    assert.deepEqual(save.body.articles.map(a => [a.id, a.sequence_no]), [[185, 5], [186, 10]]);
});

test('caller-supplied article refs are expanded to full records', async () => {
    posted.length = 0;
    await callTool('update_page', { id: 164, articles: [{ id: 186, sequence_no: 99 }] });
    const save = posted.find(p => p.url === '/page');
    assert.equal(save.body.articles.length, 1);
    assert.equal(save.body.articles[0].id, 186);
    assert.equal(save.body.articles[0].sequence_no, 99, 'caller sequence_no should win');
    assert.equal(save.body.articles[0].websiteId, 2);
    assert.equal(save.body.articles[0].articlePath, 'Apostasy-Text.html');
});

test('update_page refuses rather than writing a partial record if an article re-read fails', async () => {
    posted.length = 0;
    const r = await callTool('update_page', { id: 164, articles: [{ id: 999, sequence_no: 5 }] });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /could not re-read article 999/);
    assert.equal(posted.find(p => p.url === '/page'), undefined, 'nothing should have been written');
});

test.after(() => server.close());
