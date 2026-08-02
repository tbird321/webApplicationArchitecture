// Central argument coercion + validation for every MCP tool call.
//
// WHY THIS EXISTS
// ---------------
// inputSchema was advertised to the model but never enforced. Two consequences bit us
// repeatedly:
//
//   1. Array/object params arrive as JSON *strings* (sometimes double-encoded) depending on
//      how the harness serialises them. update_page grew a local while-loop to unwrap that;
//      every other array-taking tool silently received a string and did the wrong thing.
//   2. A typo'd or missing field produced a confusing 500 from the Lambda, or worse, wrote a
//      null into a column, instead of failing fast with a readable message.
//
// Coercing and validating once, centrally, means a tool handler can trust its args.

/** Unwrap a value that may have been JSON-encoded one or more times. */
function unwrapJson(value, wantType) {
    let v = value;
    let guard = 0;
    while (typeof v === 'string' && guard++ < 5) {
        const trimmed = v.trim();
        // Attempt a parse when the string looks like JSON for the type we want, OR when it
        // looks like a quoted string — that is the double-encoded case, where one parse peels
        // off a layer and the next iteration finds the real array/object underneath.
        const open = wantType === 'array' ? '[' : '{';
        const looksRight = trimmed.startsWith(open) || trimmed.startsWith('"');
        if (!looksRight) break;
        try {
            const parsed = JSON.parse(trimmed);
            if (parsed === v) break; // no progress; stop rather than spin
            v = parsed;
        } catch { break; }
    }
    return v;
}

function coerceScalar(value, type, path, errors) {
    if (type === 'number') {
        if (typeof value === 'number') return value;
        if (typeof value === 'string' && value.trim() !== '' && Number.isFinite(Number(value))) {
            return Number(value);
        }
        errors.push(`${path}: expected a number, got ${JSON.stringify(value)}`);
        return value;
    }
    if (type === 'string') {
        if (typeof value === 'string') return value;
        if (typeof value === 'number' || typeof value === 'boolean') return String(value);
        errors.push(`${path}: expected a string, got ${JSON.stringify(value)}`);
        return value;
    }
    if (type === 'boolean') {
        if (typeof value === 'boolean') return value;
        if (value === 'true') return true;
        if (value === 'false') return false;
        errors.push(`${path}: expected a boolean, got ${JSON.stringify(value)}`);
        return value;
    }
    return value;
}

function coerceValue(value, schema, path, errors) {
    if (!schema || value === undefined || value === null) return value;

    const type = schema.type;

    if (type === 'array') {
        const arr = unwrapJson(value, 'array');
        if (!Array.isArray(arr)) {
            errors.push(`${path}: expected an array, got ${JSON.stringify(value)}`);
            return arr;
        }
        return arr.map((item, i) => coerceValue(item, schema.items, `${path}[${i}]`, errors));
    }

    if (type === 'object') {
        const obj = unwrapJson(value, 'object');
        if (typeof obj !== 'object' || Array.isArray(obj) || obj === null) {
            errors.push(`${path}: expected an object, got ${JSON.stringify(value)}`);
            return obj;
        }
        const out = { ...obj };
        for (const [key, propSchema] of Object.entries(schema.properties || {})) {
            if (out[key] !== undefined) {
                out[key] = coerceValue(out[key], propSchema, `${path}.${key}`, errors);
            }
        }
        for (const req of schema.required || []) {
            if (out[req] === undefined || out[req] === null) {
                errors.push(`${path}.${req} is required`);
            }
        }
        return out;
    }

    const coerced = coerceScalar(value, type, path, errors);

    if (Array.isArray(schema.enum) && !schema.enum.includes(coerced)) {
        errors.push(`${path}: must be one of ${schema.enum.join(', ')} — got ${JSON.stringify(coerced)}`);
    }

    return coerced;
}

/**
 * Validate and coerce a tool's arguments against its inputSchema.
 * Throws a single readable Error listing every problem found.
 * Returns a new args object with values coerced to the declared types.
 */
export function validateArgs(toolName, schema, rawArgs) {
    const args = rawArgs || {};
    const errors = [];
    const out = {};

    const properties = (schema && schema.properties) || {};
    const required = (schema && schema.required) || [];

    // Unknown keys are a caller mistake worth surfacing — a typo'd field name would
    // otherwise be silently dropped and the update would look like it succeeded.
    const known = new Set(Object.keys(properties));
    const unknown = Object.keys(args).filter(k => !known.has(k));

    for (const [key, propSchema] of Object.entries(properties)) {
        if (args[key] === undefined) continue;
        out[key] = coerceValue(args[key], propSchema, key, errors);
    }

    for (const req of required) {
        if (out[req] === undefined || out[req] === null || out[req] === '') {
            errors.push(`${req} is required`);
        }
    }

    if (unknown.length > 0) {
        errors.push(
            `unknown parameter(s): ${unknown.join(', ')}. ` +
            `Valid parameters are: ${[...known].join(', ') || '(none)'}`
        );
    }

    if (errors.length > 0) {
        throw new Error(`${toolName}: invalid arguments —\n  - ${errors.join('\n  - ')}`);
    }

    return out;
}
