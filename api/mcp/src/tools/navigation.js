import { apiGet, apiPost, apiDelete, websiteId } from '../apiClient.js';

// WHY THESE TOOLS ARE GUARDED DIFFERENTLY
// ---------------------------------------
// Every other write tool protects itself with assertSameSite(): read the record, compare its
// websiteId to the site being written to, refuse on mismatch. Menu items cannot do that — they
// are addressed positionally as /menu/{websiteId}/item/{id}, so there is no owner field to
// compare. Whatever site is in the path IS the site that gets written.
//
// On 2026-07-29 that cost a live site: an update intended for ldsdiscussions (6) ran while the
// server was pointed at ldsapologetics (5) and repointed menu item 110 at a page that does not
// exist there. It reported success. It was found weeks later by a hand audit.
//
// An "does this item exist?" check would NOT have caught it — item 110 existed on site 5. The
// thing that was wrong was the PAGE the item referenced. So the guard is built on the
// reference: resolve the linked page on the target site, and refuse if it is not there.

/** Look up the site's display name so results can name it instead of just its id. */
async function siteLabel() {
    const id = websiteId();
    try {
        const sites = await apiGet('/website');
        const match = (Array.isArray(sites) ? sites : []).find(s => String(s.id) === String(id));
        const name = match?.name || match?.domain || match?.websiteName;
        if (name) return `${name} (site ${id})`;
    } catch (_) { /* naming is a convenience, never a reason to fail a call */ }
    return `site ${id}`;
}

/**
 * Resolve a pageId on the site currently being written to.
 * Throws — with both the page and the site named — if it does not live there.
 */
async function resolvePageOnThisSite(pageId, action) {
    let page = null;
    try {
        page = await apiGet(`/page/${pageId}/${websiteId()}`);
    } catch (_) { /* fall through to the refusal below */ }

    if (!page || page.id == null) {
        throw new Error(
            `Refused: page ${pageId} does not exist on ${await siteLabel()}, so ${action} would ` +
            `create a menu entry pointing at nothing. This is the check that catches a menu edit ` +
            `aimed at the wrong site — verify the site, or the page id.`
        );
    }
    return page;
}

async function fetchMenu() {
    const menu = await apiGet(`/menu/${websiteId()}`);
    return Array.isArray(menu) ? menu : [];
}

/** All descendants of an item, following parent links to any depth. */
function descendantsOf(menu, id) {
    const out = [];
    const seen = new Set();
    const walk = (parentId) => {
        if (seen.has(parentId)) return;   // a hand-edited menu can contain a cycle
        seen.add(parentId);
        for (const item of menu.filter(m => m && m.parent === parentId)) {
            out.push(item);
            walk(item.id);
        }
    };
    walk(id);
    return out;
}

// HOW DEEP A MENU MAY GO
// ----------------------
// The nav renders Section > Group > Subgroup > ... to this many levels and then stops:
// see StaticPageRenderer.MaxNavDepth, which this must equal. StaticRenderNavTests fails if
// the two drift.
//
// The renderer TRUNCATES what is too deep; these tools REFUSE to create it. That difference
// is the point -- a truncated item is a page that exists, is linked from the menu file, and
// appears in the nav on no page at all. Nothing reports it. Refusing at write time is the
// only moment the mistake is visible.
const MAX_MENU_DEPTH = 5;

/** Depth of an item: a top-level item (parent 0) is depth 0. Infinity if its chain cycles. */
function depthOf(menu, id) {
    const byId = new Map(menu.filter(Boolean).map(m => [m.id, m]));
    const seen = new Set();
    let depth = 0;
    let cur = byId.get(id);

    while (cur && cur.parent != null && cur.parent !== 0) {
        if (seen.has(cur.id)) return Infinity;
        seen.add(cur.id);
        const parent = byId.get(cur.parent);
        if (!parent) break;      // dangling parent -- the renderer cannot reach it either
        depth++;
        cur = parent;
    }
    return depth;
}

/** Levels of menu BELOW an item (0 for a leaf). Needed when moving a whole subtree. */
function heightOf(menu, id, seen = new Set()) {
    if (seen.has(id)) return 0;
    seen.add(id);
    const kids = menu.filter(m => m && m.parent === id);
    if (kids.length === 0) return 0;
    return 1 + Math.max(...kids.map(k => heightOf(menu, k.id, seen)));
}

/** Human-readable "Topics > Book of Mormon > Alma" for the item chain ending at id. */
function pathTo(menu, id) {
    const byId = new Map(menu.filter(Boolean).map(m => [m.id, m]));
    const parts = [];
    const seen = new Set();
    let cur = byId.get(id);

    while (cur && !seen.has(cur.id)) {
        seen.add(cur.id);
        parts.unshift(cur.text ?? `item ${cur.id}`);
        cur = (cur.parent != null && cur.parent !== 0) ? byId.get(cur.parent) : null;
    }
    return parts.join(' > ');
}

/**
 * Refuse a placement that the renderer would silently truncate.
 * `subtreeHeight` is 0 for a new leaf, or heightOf(item) when moving an existing subtree.
 */
async function assertDepthFits(menu, parentId, subtreeHeight, what) {
    if (parentId === 0) return;

    const parentDepth = depthOf(menu, parentId);
    if (parentDepth === Infinity) {
        throw new Error(
            `Refused: menu item ${parentId} on ${await siteLabel()} sits in a parent cycle, so it has ` +
            `no depth. Fix the menu structure before adding to it — call get_menu to inspect it.`
        );
    }

    const deepest = parentDepth + 1 + subtreeHeight;
    if (deepest > MAX_MENU_DEPTH - 1) {
        throw new Error(
            `Refused: ${what} would sit at level ${deepest + 1} of the menu on ${await siteLabel()}, ` +
            `and the nav renders only ${MAX_MENU_DEPTH} levels — it would exist in sitemenu.json but ` +
            `appear in the navigation of no page. Parent "${pathTo(menu, parentId)}" is already at ` +
            `level ${parentDepth + 1}. Attach it higher up, or flatten that branch.`
        );
    }
}

export const navigationTools = [
    {
        name: 'get_menu',
        description: 'Retrieve the full site menu structure as a flat array. Each item has: id, parent (0 = top level), droppable (true = section header that can have children), text (display label), and optionally pageId and pageName for leaf items. ' +
            'The array is FLAT but the menu is a TREE — rebuild it by following parent ids. Nesting is unlimited in the file and rendered to 5 levels, so an item\'s parent may itself be a child of another section. ' +
            'Sections are displayed alphabetically, but new items are appended — so after adding, check where the item actually sits.',
        inputSchema: { type: 'object', properties: {} },
        handler: async () => apiGet(`/menu/${websiteId()}`)
    },
    {
        name: 'add_menu_item',
        description:
            'Add a new menu item. For a leaf item linking to a page, provide parentId (the section id), itemTitle and pageId. ' +
            'For a new section header, provide itemTitle only and no pageId — parentId: 0 makes it top-level, or pass another section\'s id to nest it as a SUB-SECTION. ' +
            'Menus nest to 5 levels (Section > Subsection > Sub-subsection > ...), so build depth by adding a pageless section and then adding items under its id. ' +
            'A placement deeper than the nav renders is REFUSED rather than written, because such an item appears in the navigation of no page and nothing reports it. ' +
            'The page is verified to exist on the target site before anything is written, and pageName is taken from the page record rather than the caller. ' +
            'NOTE: a menu change re-renders the whole site, and the nav is baked into every prerendered page.',
        inputSchema: {
            type: 'object',
            properties: {
                parentId: { type: 'number', description: 'ID of the parent item. Use 0 for a top-level section, or any existing item id to nest beneath it.' },
                itemTitle: { type: 'string', description: 'Display text for the menu item.' },
                pageId: { type: 'number', description: 'Page ID to link to. Omit for section headers.' },
                pageName: { type: 'string', description: 'Ignored if pageId is given — the real page name is read from the page record.' }
            },
            required: ['parentId', 'itemTitle']
        },
        handler: async ({ parentId, itemTitle, pageId, pageName }) => {
            let resolvedName = pageName;

            if (pageId != null) {
                const page = await resolvePageOnThisSite(pageId, 'adding this menu item');
                // Trust the page record over the caller: a pageName that disagrees with pageId is
                // how a menu entry ends up labelled one thing and linking somewhere else.
                resolvedName = page.name || pageName;
            }

            // A leaf item under a real parent should have a parent that actually exists.
            if (parentId !== 0) {
                const menu = await fetchMenu();
                if (!menu.some(m => m && m.id === parentId)) {
                    throw new Error(
                        `Refused: no menu item ${parentId} on ${await siteLabel()} to add under. ` +
                        `Call get_menu to list the sections for this site.`
                    );
                }
                await assertDepthFits(menu, parentId, 0, `"${itemTitle}"`);
            }

            const result = await apiPost(`/menu/${websiteId()}/item`, {
                parentId, itemTitle, pageId, pageName: resolvedName
            });

            return {
                result,
                summary:
                    `Added "${itemTitle}" to the menu on ${await siteLabel()}` +
                    (pageId != null ? `, linking to page ${pageId} ("${resolvedName}")` : ' as a section header') +
                    `. New items are appended, so it will sit at the bottom of its section until reordered. ` +
                    `This triggers a full-site re-render.`
            };
        }
    },
    {
        name: 'update_menu_item',
        description:
            'Update an existing menu item\'s display text, linked page, or parent. Provide the item id and only the fields to change; anything omitted keeps its current value. ' +
            'Setting parentId MOVES the item and everything beneath it — use this to reorganise a flat menu into a nested one. ' +
            'The move is refused if it would put the item under itself or under one of its own descendants (which detaches the branch from the menu root), or if the deepest item in the branch would land below the 5 levels the nav renders. ' +
            'If pageId is given it is verified to exist on the target site first, and pageName is taken from the page record. ' +
            'NOTE: a menu change re-renders the whole site.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number', description: 'ID of the menu item to update.' },
                itemTitle: { type: 'string', description: 'New display text.' },
                pageId: { type: 'number', description: 'New page ID to link to.' },
                pageName: { type: 'string', description: 'Ignored if pageId is given — the real page name is read from the page record.' },
                parentId: { type: 'number', description: 'New parent ID to move item under.' }
            },
            required: ['id']
        },
        handler: async ({ id, itemTitle, pageId, pageName, parentId }) => {
            const menu = await fetchMenu();
            const existing = menu.find(m => m && m.id === id);

            if (!existing) {
                throw new Error(
                    `Refused: no menu item ${id} on ${await siteLabel()}. ` +
                    `Menu items are addressed per site — if you meant a different site, pass websiteId on this call.`
                );
            }

            let resolvedName = pageName;
            if (pageId != null) {
                const page = await resolvePageOnThisSite(pageId, 'repointing this menu item');
                resolvedName = page.name || pageName;
            }

            if (parentId != null && parentId !== 0) {
                if (!menu.some(m => m && m.id === parentId)) {
                    throw new Error(`Refused: no menu item ${parentId} on ${await siteLabel()} to move item ${id} under.`);
                }

                // Moving an item beneath itself detaches the whole branch from the root: the
                // items stay in sitemenu.json and vanish from every page's nav at once.
                if (parentId === id) {
                    throw new Error(
                        `Refused: menu item ${id} ("${existing.text}") cannot be its own parent. ` +
                        `The whole branch would disappear from the navigation while still existing in the menu file.`
                    );
                }
                if (descendantsOf(menu, id).some(d => d.id === parentId)) {
                    throw new Error(
                        `Refused: menu item ${parentId} sits BENEATH item ${id} ("${existing.text}") on ` +
                        `${await siteLabel()}, so moving ${id} under it would detach both from the menu root ` +
                        `and remove the whole branch from every page's navigation. Move it under a section ` +
                        `outside that branch, or move the child out first.`
                    );
                }

                // The item may be carrying a subtree, so it is the DEEPEST descendant that has
                // to fit, not the item itself.
                await assertDepthFits(menu, parentId, heightOf(menu, id), `item ${id} ("${existing.text}") and everything beneath it`);
            }

            const result = await apiPost(`/menu/${websiteId()}/item/${id}`, {
                itemTitle, pageId, pageName: resolvedName, parentId
            });

            // Say what it was as well as what it is — a wrong-site edit is obvious the moment the
            // "before" text is not what you expected to be changing.
            const before = `"${existing.text}"` + (existing.pageId != null ? ` -> page ${existing.pageId}` : '');
            const changed = [
                itemTitle != null && `text to "${itemTitle}"`,
                pageId != null && `link to page ${pageId} ("${resolvedName}")`,
                parentId != null && `parent to ${parentId}`
            ].filter(Boolean);

            return {
                result,
                summary:
                    `Updated menu item ${id} on ${await siteLabel()}. Was ${before}. ` +
                    (changed.length ? `Changed ${changed.join(', ')}.` : 'No fields were supplied to change.') +
                    ` This triggers a full-site re-render.`
            };
        }
    },
    {
        name: 'replace_menu',
        description:
            'Replace the ENTIRE site menu in a single write. Use this for a restructure — regrouping sections, nesting, or reordering — instead of many add/update calls. ' +
            'WHY: every single-item change re-renders EVERY page on the site, so moving 140 items one at a time costs ~140 whole-site renders; this costs one. It is also the ONLY way to control sibling ORDER, because order is array order and add_menu_item can only append. ' +
            'Pass the COMPLETE menu as items — anything omitted is deleted. Call get_menu first and modify what it returns. ' +
            'The server refuses the write, changing nothing, unless: every id is unique, every parent exists, every item is reachable from the top level (no cycles or detached branches), nothing sits deeper than the nav renders, every pageName is a published page ON THIS SITE, and no page that is linked today would silently stop being linked (override that last one with allowUnlink). ' +
            'Pages are never deleted by this call — unlinking only removes them from the navigation.',
        inputSchema: {
            type: 'object',
            properties: {
                items: {
                    type: 'array',
                    description: 'The COMPLETE menu as a flat array, in the order it should render. Each item: id (unique number), parent (0 for top level, else another item id), text (label), and for a page link pageId and pageName. Section headers have no pageId/pageName. Sibling order is array order.',
                    items: { type: 'object' }
                },
                allowUnlink: {
                    type: 'boolean',
                    description: 'Permit pages that are in the menu today to be absent from the new one. Default false, because dropping a page out of the nav is usually accidental and silent.'
                }
            },
            required: ['items']
        },
        handler: async ({ items, allowUnlink }) => {
            if (!Array.isArray(items) || items.length === 0) {
                throw new Error(
                    'Refused: items must be the COMPLETE menu as a non-empty array. ' +
                    'An empty or partial list erases the navigation on every page of the site. Call get_menu and edit what it returns.'
                );
            }

            const before = await fetchMenu();
            const result = await apiPost(`/menu/${websiteId()}/replace`, { items, allowUnlink: allowUnlink === true });

            const beforeLinked = before.filter(m => m && m.pageName).length;
            const afterLinked = items.filter(m => m && m.pageName).length;

            return {
                result,
                summary:
                    `Replaced the whole menu on ${await siteLabel()}: ${before.length} items -> ${items.length}, ` +
                    `${beforeLinked} linked pages -> ${afterLinked}. ` +
                    `This is ONE site-wide re-render; the nav is baked into every page, so the change is not visible until it finishes.`
            };
        }
    },
    {
        name: 'delete_menu_item',
        description:
            'Remove a menu item by id. If the item is a section header this also removes every item beneath it — so the tool REFUSES when the item has children unless includeChildren is true, and names what would go. ' +
            'Deleting a menu item does not delete the page it points at.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number', description: 'ID of the menu item to delete.' },
                includeChildren: {
                    type: 'boolean',
                    description: 'Required (true) to delete a section header that still has items beneath it.'
                }
            },
            required: ['id']
        },
        handler: async ({ id, includeChildren }) => {
            const menu = await fetchMenu();
            const existing = menu.find(m => m && m.id === id);

            if (!existing) {
                throw new Error(
                    `Refused: no menu item ${id} on ${await siteLabel()}. ` +
                    `Menu items are addressed per site — if you meant a different site, pass websiteId on this call.`
                );
            }

            const children = descendantsOf(menu, id);
            if (children.length > 0 && includeChildren !== true) {
                const list = children.slice(0, 10).map(c => `${c.id} ("${c.text}")`).join(', ');
                throw new Error(
                    `Refused: menu item ${id} ("${existing.text}") on ${await siteLabel()} has ` +
                    `${children.length} item(s) beneath it, and deleting it removes them all: ${list}` +
                    `${children.length > 10 ? ', …' : ''}. ` +
                    `Pass includeChildren: true if that is genuinely intended.`
                );
            }

            const result = await apiDelete(`/menu/${websiteId()}/item/${id}`);

            return {
                result,
                summary:
                    `Deleted menu item ${id} ("${existing.text}") from the menu on ${await siteLabel()}` +
                    (children.length ? `, along with ${children.length} item(s) beneath it` : '') +
                    `. The linked page itself was not deleted. This triggers a full-site re-render.`
            };
        }
    }
];
