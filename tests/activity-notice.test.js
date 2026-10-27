import test from 'node:test';
import assert from 'node:assert/strict';
import { showActivityNotice, tickActivityNotice } from '../src/systems/activity-notice-system.js';
import { onMonsterKilledForQuests } from '../src/game/quest-flow.js';

test('one card displays while pickup and quest events queue; previews yield immediately', () => {
  const world = { lootFeedback: { mode: 'preview', lifeMs: 1000 }, noticeQueue: [] };
  showActivityNotice(world, { mode: 'picked', text: 'HP Potion' }, 1000);
  showActivityNotice(world, { mode: 'quest', questId: 'slimeTrouble', text: 'Slime Trouble', current: 1 }, 2000);
  showActivityNotice(world, { mode: 'quest', questId: 'slimeTrouble', text: 'Slime Trouble', current: 2 }, 2000);
  assert.equal(world.lootFeedback.mode, 'picked');
  assert.equal(world.noticeQueue.length, 1);
  tickActivityNotice(world, 1000);
  assert.equal(world.lootFeedback.mode, 'quest');
  assert.equal(world.lootFeedback.current, 2);
  assert.equal(world.noticeQueue.length, 0);
});

test('quest kills update the shared card and completion without centered announcements', () => {
  const session = {
    world: { noticeQueue: [], lootFeedback: null },
    quests: { active: { slimeTrouble: { progress: 3 } }, completed: [] },
    banner: { show() { assert.fail('Incremental quest updates must use the shared card'); } },
  };
  onMonsterKilledForQuests(session, 'slime');
  assert.equal(session.world.lootFeedback.current, 4);
  assert.equal(session.world.lootFeedback.total, 5);
  onMonsterKilledForQuests(session, 'slime');
  assert.equal(session.world.lootFeedback.ready, true);
  assert.equal(session.world.lootFeedback.current, 5);
  assert.equal(session.world.noticeQueue.length, 0);
  assert.match(session.world.lootFeedback.subtext, /Return to/);
});
