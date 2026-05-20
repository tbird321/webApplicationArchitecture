// Promise-based mutex — ensures only one holder at a time.
// Used to make create_page_with_article atomic across parallel agents
// that share the same MCP server process.
let _lock = Promise.resolve();

export function acquireLock(fn) {
    const next = _lock.then(() => fn());
    // swallow errors so the lock always releases even on failure
    _lock = next.then(() => {}, () => {});
    return next;
}

// Separate lock for external HTTP fetches (e.g. ldsdiscussions.com).
// All parallel agents queue through this so they never hit the source
// site concurrently. Each agent waits its turn then gets a polite delay
// before the actual request fires.
let _fetchLock = Promise.resolve();

export function acquireFetchLock(fn) {
    const next = _fetchLock.then(() => fn());
    _fetchLock = next.then(() => {}, () => {});
    return next;
}
