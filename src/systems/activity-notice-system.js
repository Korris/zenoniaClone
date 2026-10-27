// One visible activity card; transient events queue while nearby-item previews yield.
export function showActivityNotice(world, notice, durationMs = 1600) {
  const next = { ...notice, lifeMs: durationMs, maxLifeMs: durationMs };
  world.noticeQueue ??= [];
  const current = world.lootFeedback;
  if (notice.questId && current?.questId === notice.questId) {
    world.lootFeedback = next;
    return;
  }
  if (current?.lifeMs > 0 && current.mode !== 'preview'
    && !(current.mode === 'error' && notice.mode === 'picked')) {
    const pending = notice.questId ? world.noticeQueue.findIndex(entry => entry.questId === notice.questId) : -1;
    if (pending >= 0) world.noticeQueue[pending] = next;
    else world.noticeQueue.push(next);
  } else world.lootFeedback = next;
}

export function tickActivityNotice(world, dt) {
  if (world.lootFeedback) world.lootFeedback.lifeMs = Math.max(0, world.lootFeedback.lifeMs - dt);
  if (!(world.lootFeedback?.lifeMs > 0) && world.noticeQueue?.length) world.lootFeedback = world.noticeQueue.shift();
}
