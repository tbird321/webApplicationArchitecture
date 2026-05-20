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
