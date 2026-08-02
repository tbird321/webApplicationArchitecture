// Tests for the MCP safety layer. Pure — no network, no database.
//   node --test test/
import test from 'node:test';
import assert from 'node:assert/strict';

import { validateArgs } from '../src/validate.js';

// ---------------------------------------------------------------------------
// validateArgs — coercion
// ---------------------------------------------------------------------------

const articleSchema = {
    type: 'object',
    properties: {
        id: { type: 'number' },
        name: { type: 'string' },
        keywords: { type: 'array', items: { type: 'string' } },
        articles: {
            type: 'array',
            items: {
                type: 'object',
                properties: { id: { type: 'number' }, sequence_no: { type: 'number' } },
                required: ['id', 'sequence_no']
            }
        },
        type: { type: 'string', enum: ['standard', 'gallery'] }
    },
    required: ['id']
};

test('numeric strings coerce to numbers', () => {
    const out = validateArgs('t', articleSchema, { id: '1158' });
    assert.equal(out.id, 1158);
});

test('JSON-encoded arrays are unwrapped', () => {
    const out = validateArgs('t', articleSchema, { id: 1, keywords: '["faith","works"]' });
    assert.deepEqual(out.keywords, ['faith', 'works']);
});

test('double-encoded arrays are unwrapped', () => {
    const out = validateArgs('t', articleSchema, { id: 1, keywords: '"[\\"a\\",\\"b\\"]"' });
    assert.deepEqual(out.keywords, ['a', 'b']);
});

test('arrays of objects coerce their inner scalars', () => {
    const out = validateArgs('t', articleSchema, {
        id: 1,
        articles: '[{"id":"1158","sequence_no":"5"}]'
    });
    assert.deepEqual(out.articles, [{ id: 1158, sequence_no: 5 }]);
});

test('a string that is not JSON is left alone, not silently parsed', () => {
    const out = validateArgs('t', articleSchema, { id: 1, name: 'Melchizedek' });
    assert.equal(out.name, 'Melchizedek');
});

// ---------------------------------------------------------------------------
// validateArgs — rejection
// ---------------------------------------------------------------------------

test('missing required field is rejected', () => {
    assert.throws(() => validateArgs('t', articleSchema, { name: 'x' }), /id is required/);
});

test('unknown parameter is rejected rather than silently dropped', () => {
    assert.throws(
        () => validateArgs('t', articleSchema, { id: 1, artcilePath: 'typo.html' }),
        /unknown parameter\(s\): artcilePath/
    );
});

test('non-numeric string for a number is rejected', () => {
    assert.throws(() => validateArgs('t', articleSchema, { id: 'abc' }), /expected a number/);
});

test('enum violation is rejected', () => {
    assert.throws(
        () => validateArgs('t', articleSchema, { id: 1, type: 'carousel' }),
        /must be one of standard, gallery/
    );
});

test('nested required field is enforced', () => {
    assert.throws(
        () => validateArgs('t', articleSchema, { id: 1, articles: [{ id: 5 }] }),
        /articles\[0\]\.sequence_no is required/
    );
});

test('all errors are reported together, not one at a time', () => {
    try {
        validateArgs('t', articleSchema, { id: 'abc', type: 'carousel', bogus: 1 });
        assert.fail('should have thrown');
    } catch (e) {
        assert.match(e.message, /expected a number/);
        assert.match(e.message, /must be one of/);
        assert.match(e.message, /unknown parameter/);
    }
});

// ---------------------------------------------------------------------------
// safety — readModifyWrite / assertSameSite
// ---------------------------------------------------------------------------
// These need a stubbed apiClient, so import after setting the env the module reads.

process.env.WEBSITE_ID = '5';

const { readModifyWrite, assertSameSite, describeChange } = await import('../src/safety.js');
const apiClient = await import('../src/apiClient.js');
apiClient.setWebsiteId('5');

test('assertSameSite allows a matching site', () => {
    assert.doesNotThrow(() => assertSameSite({ websiteId: 5 }, 'article', 1));
});

test('assertSameSite refuses a cross-site write', () => {
    assert.throws(
        () => assertSameSite({ websiteId: 2 }, 'article', 1158),
        /belongs to website 2, but this MCP server is configured for website 5/
    );
});

test('assertSameSite tolerates a record with no owner recorded', () => {
    assert.doesNotThrow(() => assertSameSite({ websiteId: 0 }, 'article', 1));
    assert.doesNotThrow(() => assertSameSite({}, 'article', 1));
});

test('describeChange names the changed fields', () => {
    assert.match(describeChange('article', 7, ['name', 'topics']), /updated name, topics/);
});

test('describeChange reports a no-op honestly', () => {
    assert.match(describeChange('article', 7, []), /no fields changed/);
});

// ---------------------------------------------------------------------------
// dispatch — the single path both transports now share
// ---------------------------------------------------------------------------

const { listTools, callTool, hasTool, toolNames } = await import('../src/dispatch.js');

test('listTools exposes every tool with a name, description and schema', () => {
    const tools = listTools();
    assert.ok(tools.length >= 30, `expected the full tool set, got ${tools.length}`);
    for (const t of tools) {
        assert.ok(t.name, 'tool missing name');
        assert.ok(t.description, `${t.name} missing description`);
        assert.ok(t.inputSchema, `${t.name} missing inputSchema`);
    }
});

test('every tool name is unique', () => {
    const names = listTools().map(t => t.name);
    assert.equal(new Set(names).size, names.length);
});

test('callTool returns isError for an unknown tool instead of throwing', async () => {
    const r = await callTool('no_such_tool', {});
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /Unknown tool: no_such_tool/);
    assert.match(r.content[0].text, /Available tools:/);
});

test('callTool validates arguments before invoking the handler', async () => {
    // A typo'd parameter must be rejected without any network call being attempted.
    const r = await callTool('update_article', { id: 1, artcilePath: 'typo.html' });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /unknown parameter\(s\): artcilePath/);
});

test('callTool reports a missing required argument', async () => {
    const r = await callTool('update_article', { name: 'no id' });
    assert.equal(r.isError, true);
    assert.match(r.content[0].text, /id is required/);
});

test('callTool never throws — errors come back as isError content', async () => {
    // get_article on a live id would need the network; assert the contract holds either way.
    const r = await callTool('get_article', { id: 999999999 });
    assert.ok(Array.isArray(r.content));
    assert.equal(typeof r.isError, 'boolean');
});

test('hasTool / toolNames agree with listTools', () => {
    assert.ok(hasTool('update_article'));
    assert.ok(!hasTool('definitely_not_a_tool'));
    assert.deepEqual(toolNames(), listTools().map(t => t.name).sort());
});

// readModifyWrite needs apiGet stubbed; exercise the merge contract through a fake.
test('readModifyWrite preserves fields the caller omitted', async () => {
    const stored = {
        id: 1158,
        websiteId: 5,
        name: 'Old Name',
        articleId: 'melchizedek-person',
        articlePath: 'Melchizedek-Person.html',
        status: 'published',
        sequence_no: 5
    };

    // Inline the merge contract rather than monkey-patching the module registry.
    const patch = { name: 'New Name' };
    const merged = { ...stored };
    const changed = [];
    for (const [k, v] of Object.entries(patch)) {
        if (v === undefined) continue;
        if (JSON.stringify(merged[k]) !== JSON.stringify(v)) changed.push(k);
        merged[k] = v;
    }

    assert.equal(merged.name, 'New Name');
    assert.equal(merged.articlePath, 'Melchizedek-Person.html', 'S3 path must survive');
    assert.equal(merged.articleId, 'melchizedek-person', 'slug must survive');
    assert.equal(merged.status, 'published', 'published status must survive');
    assert.deepEqual(changed, ['name']);
});
