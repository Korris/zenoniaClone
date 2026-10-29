// Tuning for melee "game feel": hit-stop, screen shake, hit flash, weapon trail, impact VFX, damage numbers, audio.
// One mutable object (not frozen) so a future settings UI can flip `reduceEffects` at runtime.

export const COMBAT_FEEL = {
  /** Accessibility/perf toggle: disables screen shake, monster flash + squash and the shock ring. */
  reduceEffects: false,

  hitStop: {
    normalMs: 45,
    finisherMs: 80,
    critBonusMs: 20,
    maxMs: 100, // total remaining freeze never exceeds this, however many hits stack
  },

  shake: {
    scale: 1, // multiplier for both trauma and legacy camera.shake()
    maxOffset: 4.5, // world units at trauma 1 (x4 on screen)
    decayPerSec: 2.6,
    trauma: { finisher: 0.55, crit: 0.4, kill: 0.3 },
  },

  hitReaction: {
    flashMs: 110, // white silhouette duration
    squashAmount: 0.16, // +x width / -y height at the moment of impact
    knockbackScale: 1.3,
  },

  trail: {
    lifeMs: 120, // how long a ribbon sample stays visible after the tip passed it
    segments: 12,
    leadFraction: 0.5, // ribbon starts this far along its arc: the blade is already mid-swing when damage lands
    edgeAlpha: 0.88,
    coreAlpha: 0.9,
    // Per class tint: edge = coloured outer ribbon, core = near-white inner ribbon.
    colors: {
      warrior: { edge: '#ff6a24', core: '#fff3d6' },
      assassin: { edge: '#9b5cff', core: '#f1e6ff' },
      default: { edge: '#6ec8ff', core: '#f0fbff' },
    },
    // sweep = arc angle swept by the tip (radians), radius = tip distance from hero as factor of hit reach
    normal: { sweep: 2.0, width: 9, radiusFactor: 0.55, radiusPad: 4 },
    finisher: { sweep: 2.9, width: 12, radiusFactor: 0.6, radiusPad: 5 },
  },

  impact: {
    poolSize: 96, // max simultaneous spark streaks
    ringPoolSize: 8,
    starPoolSize: 12,
    sparks: { normal: 7, finisher: 10, crit: 10 },
    sparkSpeed: [90, 190], // world units / s
    sparkLifeMs: [140, 240],
    sparkLength: 0.04, // streak length = speed * this
    ringMaxRadius: { finisher: 22, crit: 16 },
    ringLifeMs: 190,
    starLifeMs: 120,
    starSize: { normal: 6, finisher: 9, crit: 8 },
    colors: { edge: '#ffb13b', core: '#ffffff', crit: '#ff5a3a' },
  },

  slashAdditiveAlpha: 0.3, // second additive pass over slash sprites

  damageNumber: {
    popFrom: 1.4, popMs: 150, // scale 1.4 -> 1.0 with slight undershoot
    size: 32, critSize: 46,
    critOutline: '#7a1200',
  },

  audio: {
    pitchJitter: 0.1, // +-10% random pitch per impact
  },

  /** Only read by the PixiJS (WebGL) renderer -- see src/graphics/pixi/. Canvas2D ignores this section. */
  pixi: {
    fxEnabled: true, // F8 at runtime: hides the whole FX layer (slashes, trails, sparks, rings) for A/B comparison
    bloom: {
      enabled: true, // F7 at runtime
      threshold: 0.62, // brightness above which pixels bloom (FX layer only, so the scene is never washed out)
      bloomScale: 0.55, // strength of the added glow
      brightness: 1,
      blur: 4,
      quality: 3,
      resolution: 0.5, // bloom is rendered at this fraction of the screen: soft glow, a fraction of the GPU cost
    },
    spark: { echoes: 2, echoSpacing: 0.85, echoAlpha: 0.55 }, // extra faded copies behind every spark streak (GPU particles are cheap)
    trailGlow: 0.7, // additive alpha of the blade trail's white core ribbon
    // Short localized screen ripple on finisher / crit impacts (ShockwaveFilter on the stage, limited to `radius` world units around the impact).
    shockwave: { enabled: true, durationMs: 420, speed: 90, amplitude: 5, wavelength: 26, brightness: 1.1, radius: 60 },
    // Live GlowFilter on enhanced held gear, indexed by enhance band 1..4 (+7 faint ... +13..15 strong).
    gearGlow: { enabled: true, distance: [3, 5, 8, 12], outer: [1, 1.6, 2.4, 3.6], inner: [0, 0.2, 0.4, 0.7], pulse: [0, 0.08, 0.14, 0.22], pulseMs: 1400, alpha: 1 },
  },
};

/** Convenience setter for the future "reduce effects" settings toggle. */
export function setReduceEffects(on) {
  COMBAT_FEEL.reduceEffects = Boolean(on);
}

export function shakeScale() {
  return COMBAT_FEEL.reduceEffects ? 0 : COMBAT_FEEL.shake.scale;
}
