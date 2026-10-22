import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  createQuestState, sanitizeQuestState, questStatus, acceptQuest, recordKill, recordMapEnter,
  isReadyToTurnIn, turnInQuest, nextQuestForNpc, npcMarker, progressText,
} from '../src/systems/quest-system.js';
import { Player } from '../src/entities/player.js';

const level = (n) => { const p = new Player(); p.level = n; p.recomputeStats(); return p; };

test('new state has no quests; slimeTrouble is available at level 1', () => {
  const state = createQuestState();
  assert.equal(questStatus(state, level(1), 'slimeTrouble'), 'available');
  assert.equal(questStatus(state, level(1), 'batWings'), 'locked'); // needs level 2
  assert.equal(questStatus(state, level(5), 'roadToCave'), 'locked'); // prerequisite not done
});

test('accept -> kills progress -> ready -> turn in grants rewards', () => {
  const state = createQuestState();
  const player = level(1);
  assert.equal(acceptQuest(state, player, 'slimeTrouble'), true);
  assert.equal(acceptQuest(state, player, 'slimeTrouble'), false); // already active
  assert.equal(questStatus(state, player, 'slimeTrouble'), 'active');
  for (let i = 0; i < 4; i++) recordKill(state, 'slime');
  assert.equal(progressText(state, 'slimeTrouble'), '4/5');
  assert.equal(isReadyToTurnIn(state, 'slimeTrouble'), false);
  assert.deepEqual(recordKill(state, 'slime'), ['slimeTrouble']);
  assert.deepEqual(recordKill(state, 'slime'), []); // capped at target
  assert.equal(questStatus(state, player, 'slimeTrouble'), 'ready');
  const gold = player.gold;
  const rewards = turnInQuest(state, player, 'slimeTrouble');
  assert.equal(rewards.exp, 60);
  assert.equal(player.gold, gold + 20);
  assert.equal(player.level, 2); // 60 exp >= 50 needed
  assert.equal(questStatus(state, player, 'slimeTrouble'), 'completed');
  assert.equal(turnInQuest(state, player, 'slimeTrouble'), null);
});

test('reach quests complete on map enter and unlock via prerequisite', () => {
  const state = createQuestState();
  const player = level(3);
  state.completed.push('slimeTrouble');
  assert.equal(questStatus(state, player, 'roadToCave'), 'available');
  acceptQuest(state, player, 'roadToCave');
  assert.deepEqual(recordMapEnter(state, 'verdantMeadow'), []);
  assert.deepEqual(recordMapEnter(state, 'darkCave'), ['roadToCave']);
  assert.equal(isReadyToTurnIn(state, 'roadToCave'), true);
  assert.equal(progressText(state, 'roadToCave'), 'Done');
});

test('NPC picks turn-in before offer before progress, with matching marker', () => {
  const state = createQuestState();
  const player = level(1);
  assert.deepEqual(nextQuestForNpc(state, player, 'elderRowan'), { questId: 'slimeTrouble', kind: 'offer' });
  assert.equal(npcMarker(state, player, 'elderRowan'), 'available');
  acceptQuest(state, player, 'slimeTrouble');
  assert.equal(nextQuestForNpc(state, player, 'elderRowan').kind, 'progress');
  assert.equal(npcMarker(state, player, 'elderRowan'), null);
  for (let i = 0; i < 5; i++) recordKill(state, 'slime');
  assert.equal(nextQuestForNpc(state, player, 'elderRowan').kind, 'turnIn');
  assert.equal(npcMarker(state, player, 'elderRowan'), 'turnIn');
  assert.equal(nextQuestForNpc(state, player, 'villagerMia'), null);
});

test('sanitizeQuestState drops unknown quests and clamps progress', () => {
  const state = sanitizeQuestState({ active: { slimeTrouble: { progress: -3 }, bogus: { progress: 1 } }, completed: ['batWings', 'nope', 'batWings'] });
  assert.deepEqual(state, { active: { slimeTrouble: { progress: 0 } }, completed: ['batWings'] });
  assert.deepEqual(sanitizeQuestState(null), createQuestState());
});

test('sanitizeQuestState drops an active entry that is also completed', () => {
  const state = sanitizeQuestState({ active: { slimeTrouble: { progress: 2 } }, completed: ['slimeTrouble'] });
  assert.deepEqual(state, { active: {}, completed: ['slimeTrouble'] });
});
