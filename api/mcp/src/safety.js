// Guards that make destructive CMS writes safe BY CONSTRUCTION.
//
// WHY THIS EXISTS
// ---------------
// The Lambda has no PATCH endpoint. POST /article and POST /page are full-record upserts:
// the stored procedure writes EVERY column from the model it is handed, so any field absent
// from the request body is written as NULL (and `status` even defaults back to 'draft').
//
// That means a naive "update just the name" call silently:
//   - nulls articlePath        -> breaks the S3 filename, article content 404s
//   - nulls articleId          -> destroys the stable slug
//   - resets status to draft   -> unpublishes a live page without saying so
//   - rewrites websiteId       -> MOVES the record to whatever WEBSITE_ID env currently is
//
// None of that errors. The call returns 200 and looks like a success.
//
// Callers should not have to know any of this. These helpers enforce it centrally:
// read the current record, layer the caller's changes on top, and refuse writes that would
// cross a site boundary.

import { apiGet, websiteIdAsNumber } from './apiClient.js';

/**
 * Refuse to write a record that belongs to a different website than the one this server
 * is configured for. Without this, an update issued while WEBSITE_ID points elsewhere
 * silently reparents the record to the wrong site.
 */
export function assertSameSite(record, kind, id) {
    const configured = websiteIdAsNumber();
    const owner = record?.websiteId;

    if (owner === undefined || owner === null || owner === 0) return; // nothing to compare

    if (Number(owner) !== Number(configured)) {
        throw new Error(
            `Refused: ${kind} ${id} belongs to website ${owner}, but this MCP server is ` +
            `configured for website ${configured}. Writing would move the record between sites. ` +
            `Change WEBSITE_ID and restart, or target a ${kind} on website ${configured}.`
        );
    }
}

/**
 * Read-modify-write for a full-record upsert endpoint.
 *
 * Fetches the existing record, applies only the fields the caller actually supplied, and
 * returns a complete object safe to POST. Fields the caller omitted keep their stored values
 * instead of being nulled.
 *
 * @param {string} getPath   API path to fetch the current record
 * @param {object} patch     caller-supplied fields (undefined values are ignored)
 * @param {object} opts      { kind, id, defaults }
 * @returns {{ merged: object, existing: object, changed: string[] }}
 */
export async function readModifyWrite(getPath, patch, { kind, id, defaults = {} } = {}) {
    let existing;
    try {
        existing = await apiGet(getPath);
    } catch (e) {
        throw new Error(
            `Refused: could not read the current ${kind} ${id} before updating it (${e.message}). ` +
            `Updating without reading first would null out every field you did not pass.`
        );
    }

    if (!existing || existing.id == null || existing.id === 0) {
        throw new Error(`Refused: ${kind} ${id} not found. Nothing was written.`);
    }

    assertSameSite(existing, kind, id);

    const merged = { ...defaults, ...existing };
    const changed = [];

    for (const [key, value] of Object.entries(patch)) {
        if (value === undefined) continue;
        const before = JSON.stringify(merged[key]);
        const after = JSON.stringify(value);
        if (before !== after) changed.push(key);
        merged[key] = value;
    }

    return { merged, existing, changed };
}

/**
 * Build a short human-readable summary of what an update actually changed, so a successful
 * call reports its effect instead of echoing an opaque record back.
 */
export function describeChange(kind, id, changed, extra = '') {
    if (changed.length === 0) {
        return `${kind} ${id}: no fields changed (values already matched).${extra ? ' ' + extra : ''}`;
    }
    return `${kind} ${id}: updated ${changed.join(', ')}. All other fields preserved.${extra ? ' ' + extra : ''}`;
}
