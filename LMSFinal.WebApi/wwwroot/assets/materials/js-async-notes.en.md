# Asynchronous JavaScript — notes

## The event loop in one paragraph

JavaScript is single-threaded. `setTimeout`, `fetch` and event handlers do not
run "in parallel" — they queue a task, and the engine picks it up only after
the current code has finished. That is why one long synchronous loop freezes
the whole page, animations and clicks included.

## Three things people trip over

**1. `await` inside `forEach` does nothing.**

```javascript
// Waits for nothing: forEach does not understand promises.
ids.forEach(async (id) => { await save(id); });
console.log('done');   // prints FIRST

// Sequential and predictable.
for (const id of ids) {
    await save(id);
}

// Parallel, when order does not matter.
await Promise.all(ids.map((id) => save(id)));
```

**2. `fetch` does not treat 404 as an error.**

The promise rejects only when the server could not be reached. A 4xx or 5xx
is a successfully received response, so you must check it yourself:

```javascript
const response = await fetch(url);

if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
}
```

**3. An error in an async function without `await` disappears.**

Calling it without `await` and without `.catch()` gives an unhandled
rejection: a warning in the console, and silence plus a blank screen in the UI.

## Exercise

Write a function that fires three requests in parallel and returns the first
successful result, ignoring the failures. Hint: `Promise.any`.
