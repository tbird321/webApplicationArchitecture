import fetch from 'node-fetch';

const BASE = (process.env.LAMBDA_API_BASE_URL || '').replace(/\/$/, '');
const API_KEY = process.env.MCP_API_KEY;

let _websiteId = '';

export function setWebsiteId(id) { _websiteId = String(id); }

const headers = (extra = {}) => ({
    'Content-Type': 'application/json',
    'X-API-Key': API_KEY,
    ...extra
});

export async function apiGet(path) {
    const url = `${BASE}${path}`;
    const res = await fetch(url, { method: 'GET', headers: headers() });
    const text = await res.text();
    if (!res.ok) throw new Error(`GET ${path} ${res.status}: ${text}`);
    return text ? JSON.parse(text) : null;
}

export async function apiPost(path, body) {
    const url = `${BASE}${path}`;
    const res = await fetch(url, {
        method: 'POST',
        headers: headers(),
        body: JSON.stringify(body ?? {})
    });
    const text = await res.text();
    if (!res.ok) throw new Error(`POST ${path} ${res.status}: ${text}`);
    return text ? JSON.parse(text) : null;
}

export async function apiDelete(path) {
    const url = `${BASE}${path}`;
    const res = await fetch(url, { method: 'DELETE', headers: headers() });
    const text = await res.text();
    if (!res.ok) throw new Error(`DELETE ${path} ${res.status}: ${text}`);
    return text ? JSON.parse(text) : null;
}

export function websiteId() { return _websiteId; }

export function websiteIdAsNumber() {
    const n = Number(_websiteId);
    return Number.isFinite(n) ? n : _websiteId;
}
