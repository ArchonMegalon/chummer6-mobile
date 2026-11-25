const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { resolve } = require("node:path");
const { test } = require("node:test");
const { webcrypto } = require("node:crypto");
const vm = require("node:vm");

const source = readFileSync(resolve(__dirname, "../src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js"), "utf8");
function harness(fetch, observe = async () => {}) {
  const window = {
    crypto: webcrypto, addEventListener() {}, setTimeout() {},
    localStorage: { length: 0 },
  };
  const context = vm.createContext({ window, navigator: { onLine: true }, fetch, observe,
    document: { readyState: "loading", addEventListener() {} } });
  // Run the real entry point and queue functions; replace only rendering and
  // the independently tested observation boundary. Never use real network/data.
  assert.match(source, /\}\(\)\);\s*$/);
  vm.runInContext(source.replace(/\}\(\)\);\s*$/, `
    render = function () {};
    refreshClaimedDeviceStatus = observe;
    window.replayTests = { queueLocalEvent, replayLocalQueue, clearPrivateDeviceData };
  })();`), context);
  return window.replayTests;
}
function client() {
  return { sessionId: "synthetic-session", roleName: "player", deviceId: "synthetic-device",
    localReplayQueue: [], pendingReplayBatch: null, networkBusy: false,
    projection: { history: { entries: [] }, trust: {}, sync: {} } };
}
function ack(batch, ids = batch.identifiedEvents.map(item => item.eventId)) {
  return { accepted: true, retrySafe: true, acknowledgedBatchId: batch.batchId, acknowledgedEventIds: ids };
}
function response(payload) { return { ok: true, text: async () => JSON.stringify(payload) }; }
function deferred() {
  let resolve;
  const promise = new Promise(done => { resolve = done; });
  return { promise, resolve };
}

test("pending local edits survive settlement; accepted POST settles before a failing observation GET", async () => {
  const pending = deferred();
  let submitted;
  const state = client();
  const api = harness(async (_, request) => {
    submitted = JSON.parse(request.body);
    return pending.promise;
  }, async () => {
    assert.equal(state.localReplayQueue.length, 1);
    assert.equal(state.localReplayQueue[0].text, "turn:metric:ammo:-2");
    assert.equal(state.pendingReplayBatch, null);
    throw new Error("GET offline");
  });
  api.queueLocalEvent(state, "turn:metric:ammo:-1");
  const replay = api.replayLocalQueue(state);
  api.queueLocalEvent(state, "turn:metric:ammo:-2");
  assert.equal(submitted.identifiedEvents.length, 1);
  pending.resolve(response(ack(submitted)));
  await replay;
  assert.equal(state.localReplayQueue.length, 1);
  assert.match(state.statusMessage, /confirmed.*refresh failed/);
  assert.equal(state.networkBusy, false);
});

test("lost reply retries the identical frozen batch, preserving new same-text actions", async () => {
  const bodies = [];
  const state = client();
  const api = harness(async (_, request) => {
    bodies.push(request.body);
    if (bodies.length === 1) throw new Error("reply lost after commit");
    return response(ack(JSON.parse(request.body)));
  });
  api.queueLocalEvent(state, "quick-action:player-mark-ready");
  await api.replayLocalQueue(state);
  const firstId = state.localReplayQueue[0].eventId;
  api.queueLocalEvent(state, "quick-action:player-mark-ready");
  assert.notEqual(state.localReplayQueue[1].eventId, firstId);
  await api.replayLocalQueue(state);
  assert.equal(bodies[0], bodies[1]);
  assert.equal(state.localReplayQueue.length, 1);
  assert.notEqual(state.localReplayQueue[0].eventId, firstId);
  await api.replayLocalQueue(state);
  assert.notEqual(JSON.parse(bodies[2]).batchId, JSON.parse(bodies[0]).batchId);
  assert.equal(state.localReplayQueue.length, 0);
});

test("concurrent replay clicks send once; partial acknowledgement settles only submitted acknowledged identities", async () => {
  let calls = 0;
  let submitted;
  const pending = deferred();
  const state = client();
  const api = harness(async (_, request) => {
    calls++;
    submitted = JSON.parse(request.body);
    return pending.promise;
  });
  api.queueLocalEvent(state, "turn:metric:ammo:-1");
  api.queueLocalEvent(state, "turn:metric:ammo:-1");
  const first = api.replayLocalQueue(state);
  await api.replayLocalQueue(state);
  api.queueLocalEvent(state, "turn:metric:ammo:-1");
  pending.resolve(response(ack(submitted, [submitted.identifiedEvents[0].eventId])));
  await first;
  assert.equal(calls, 1);
  assert.equal(state.localReplayQueue.length, 2);
  assert.equal(state.localReplayQueue[0].eventId, submitted.identifiedEvents[1].eventId);
  assert.equal(state.pendingReplayBatch.batchId, submitted.batchId);
});

test("legacy text has no invented identity; count-only or wrong-batch acknowledgements cannot settle events", async () => {
  let calls = 0;
  const state = client();
  const api = harness(async (_, request) => {
    calls++;
    const batch = JSON.parse(request.body);
    return response(calls === 1 ? { accepted: true, acceptedEventCount: 1 }
      : { ...ack(batch), acknowledgedBatchId: "another-batch" });
  });
  state.localReplayQueue.push("turn:metric:ammo:-1");
  await api.replayLocalQueue(state);
  assert.equal(calls, 0);
  assert.equal(state.localReplayQueue[0], "turn:metric:ammo:-1");
  assert.match(state.statusMessage, /Legacy receipts/);
  state.localReplayQueue = [];
  api.queueLocalEvent(state, "turn:metric:ammo:-1");
  await api.replayLocalQueue(state);
  await api.replayLocalQueue(state);
  assert.equal(state.localReplayQueue.length, 1);
});

test("acknowledging an event added after submission cannot settle that event", async () => {
  const pending = deferred();
  let submitted;
  const state = client();
  const api = harness(async (_, request) => {
    submitted = JSON.parse(request.body);
    return pending.promise;
  });
  api.queueLocalEvent(state, "turn:metric:ammo:-1");
  const replay = api.replayLocalQueue(state);
  api.queueLocalEvent(state, "turn:metric:ammo:-2");
  pending.resolve(response(ack(submitted, [state.localReplayQueue[1].eventId])));
  await replay;
  assert.equal(state.localReplayQueue.length, 2);
  assert.equal(state.pendingReplayBatch.batchId, submitted.batchId);
});

test("a late reply after private clear cannot remove newer work", async () => {
  let submitted;
  const pending = deferred();
  const state = client();
  const api = harness(async (_, request) => {
    submitted = JSON.parse(request.body);
    return pending.promise;
  });
  api.queueLocalEvent(state, "turn:metric:ammo:-1");
  const replay = api.replayLocalQueue(state);
  api.clearPrivateDeviceData(state, false);
  api.queueLocalEvent(state, "turn:metric:ammo:-2");
  pending.resolve(response(ack(submitted, [state.localReplayQueue[0].eventId])));
  await replay;
  assert.equal(state.localReplayQueue.length, 1);
  assert.equal(state.pendingReplayBatch, null);
});
