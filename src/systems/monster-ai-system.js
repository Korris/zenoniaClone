// Monster brain: idle -> wander -> chase -> windup -> attack -> cooldown -> (chase | return).
// Pure: takes a monster record + player position and returns a movement/attack intent.
// Monster must carry an `ai` object: { state, timer, dirX, dirY, homeX, homeY }.

const WANDER_SPEED_FACTOR = 0.5;
const CHASE_SPEED_FACTOR = 0.5;

export function createAiState(homeX, homeY) {
  return { state: 'idle', timer: 600, dirX: 0, dirY: 0, homeX, homeY };
}

/**
 * @param {{x:number,y:number,def:object,ai:object}} monster
 * @param {{x:number,y:number}} player
 * @param {number} dt ms
 * @param {() => number} rand
 * @returns {{moveX:number, moveY:number, speedFactor:number, attack:boolean}} intent
 */
export function updateMonsterAi(monster, player, dt, rand = Math.random) {
  const { ai, def } = monster;
  const dx = player.x - monster.x;
  const dy = player.y - monster.y;
  const distToPlayer = Math.hypot(dx, dy);
  const distToHome = Math.hypot(ai.homeX - monster.x, ai.homeY - monster.y);
  const intent = { moveX: 0, moveY: 0, speedFactor: 1, attack: false };
  ai.timer -= dt;

  switch (ai.state) {
    case 'idle':
      if (distToPlayer < def.aggroRange) return enter(ai, 'chase', 0, intent);
      if (ai.timer <= 0) {
        const angle = rand() * Math.PI * 2;
        ai.dirX = Math.cos(angle);
        ai.dirY = Math.sin(angle);
        enter(ai, 'wander', 500 + rand() * 900, intent);
      }
      return intent;

    case 'wander':
      if (distToPlayer < def.aggroRange) return enter(ai, 'chase', 0, intent);
      if (ai.timer <= 0) return enter(ai, 'idle', 600 + rand() * 1200, intent);
      intent.moveX = ai.dirX;
      intent.moveY = ai.dirY;
      intent.speedFactor = WANDER_SPEED_FACTOR;
      return intent;

    case 'chase':
      if (distToHome > def.leashRange) return enter(ai, 'return', 0, intent);
      if (distToPlayer <= def.attackRange) return enter(ai, 'windup', def.attackWindupMs, intent);
      if (distToPlayer > 0) {
        intent.moveX = dx / distToPlayer;
        intent.moveY = dy / distToPlayer;
      }
      intent.speedFactor = CHASE_SPEED_FACTOR;
      return intent;

    case 'windup':
      // Telegraph: stand still, then strike when timer expires
      if (ai.timer <= 0) {
        intent.attack = true;
        enter(ai, 'cooldown', def.attackCooldownMs, intent);
      }
      return intent;

    case 'cooldown':
      if (ai.timer <= 0) enter(ai, 'chase', 0, intent);
      return intent;

    case 'return':
      if (distToHome < 6) return enter(ai, 'idle', 800, intent);
      if (distToPlayer < def.aggroRange * 0.6) return enter(ai, 'chase', 0, intent);
      intent.moveX = (ai.homeX - monster.x) / distToHome;
      intent.moveY = (ai.homeY - monster.y) / distToHome;
      return intent;

    default:
      return enter(ai, 'idle', 500, intent);
  }
}

function enter(ai, state, timer, intent) {
  ai.state = state;
  ai.timer = timer;
  return intent;
}
