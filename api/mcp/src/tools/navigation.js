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
    const walk = (parentId) => {
        for (const item of menu.filter(m => m && m.parent === parentId)) {
            out.push(item);
            walk(item.id);
        }
    };
    walk(id);
    return out;
}

export const navigationTools = [
    {
        name: 'get_menu',
        description: 'Retrieve the full site menu structure as a flat array. Each item has: id, parent (0 = top level), droppable (true = section header that can have children), text (display label), and optionally pageId and pageName for leaf items. Sections are displayed alphabetically, but new items are appended — so after adding, check where the item actually sits.',
        inputSchema: { type: 'object', properties: {} },
        handler: async () => apiGet(`/menu/${websiteId()}`)
    },
    {
        name: 'add_menu_item',
        description:
            'Add a new menu item. For a leaf item linking to a page, provide parentId (the section id), itemTitle and pageId. ' +
            'For a new top-level section header, provide parentId: 0 and itemTitle only. ' +
            'The page is verified to exist on the target site before anything is written, and pageName is taken from the page record rather than the caller. ' +
            'NOTE: a menu change re-renders the whole site, and the nav is baked into every prerendered page.',
        inputSchema: {
            type: 'object',
            properties: {
                parentId: { type: 'number', description: 'ID of the parent item. Use 0 for top-level sections.' },
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

            if (parentId != null && parentId !== 0 && !menu.some(m => m && m.id === parentId)) {
                throw new Error(`Refused: no menu item ${parentId} on ${await siteLabel()} to move item ${id} under.`);
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
