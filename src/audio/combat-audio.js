// Small procedural combat sounds; no downloads, speech services or external dependencies.
let context;
let master;
let noiseBuffer;
let lastImpact = -1;

export function unlockCombatAudio() {
  const AudioContext = globalThis.AudioContext || globalThis.webkitAudioContext;
  if (!AudioContext) return;
  try {
    if (!context) {
      context = new AudioContext();
      master = context.createGain();
      master.gain.value = 0.3;
      const limiter = context.createDynamicsCompressor();
      master.connect(limiter); limiter.connect(context.destination);
      noiseBuffer = context.createBuffer(1, context.sampleRate, context.sampleRate);
      const data = noiseBuffer.getChannelData(0);
      for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1;
    }
    if (context.state === 'suspended') context.resume().catch(() => {});
  } catch { /* Gameplay remains available if the browser disables audio. */ }
}

function noise(time, duration, frequency, volume, type = 'bandpass') {
  const source = context.createBufferSource();
  source.buffer = noiseBuffer;
  const filter = context.createBiquadFilter();
  filter.type = type; filter.frequency.setValueAtTime(frequency, time);
  filter.frequency.exponentialRampToValueAtTime(Math.max(80, frequency * 0.3), time + duration);
  const gain = context.createGain();
  gain.gain.setValueAtTime(0.001, time);
  gain.gain.linearRampToValueAtTime(volume, time + 0.006);
  gain.gain.exponentialRampToValueAtTime(0.001, time + duration);
  source.connect(filter); filter.connect(gain); gain.connect(master);
  source.start(time); source.stop(time + duration);
}

function tone(time, duration, frequency, endFrequency, volume, type = 'sine') {
  const source = context.createOscillator();
  source.type = type;
  source.frequency.setValueAtTime(frequency, time);
  source.frequency.exponentialRampToValueAtTime(endFrequency, time + duration);
  const gain = context.createGain();
  gain.gain.setValueAtTime(volume, time);
  gain.gain.exponentialRampToValueAtTime(0.001, time + duration);
  source.connect(gain); gain.connect(master);
  source.start(time); source.stop(time + duration);
}

/** A short synthesized martial-arts "Hah!": breath onset followed by an open vowel. */
function kiai(time) {
  noise(time, 0.10, 1700, 0.45);
  const voice = context.createOscillator();
  voice.type = 'sawtooth';
  voice.frequency.setValueAtTime(185, time + 0.025);
  voice.frequency.exponentialRampToValueAtTime(115, time + 0.27);
  for (const [frequency, volume] of [[740, 0.5], [1180, 0.3], [2500, 0.12]]) {
    const formant = context.createBiquadFilter();
    formant.type = 'bandpass'; formant.frequency.value = frequency; formant.Q.value = 5;
    const gain = context.createGain();
    gain.gain.setValueAtTime(0.001, time);
    gain.gain.linearRampToValueAtTime(volume, time + 0.055);
    gain.gain.exponentialRampToValueAtTime(0.001, time + 0.29);
    voice.connect(formant); formant.connect(gain); gain.connect(master);
  }
  voice.start(time + 0.025); voice.stop(time + 0.30);
}

export function playAttackSound(classId, step, finisher) {
  if (!context || context.state !== 'running') return;
  const time = context.currentTime;
  const force = finisher ? 1.4 : 1;
  if (classId === 'mechanic') {
    noise(time, 0.12, 2400, 0.65 * force, 'lowpass');
    tone(time, 0.13, 140, 45, 0.45 * force);
  } else if (classId === 'assassin') { // quick high dagger whisks
    noise(time, finisher ? 0.16 : 0.08, 3600 + step * 200, 0.5 * force);
    tone(time + 0.01, 0.05, 260, 90, 0.12 * force);
  } else if (classId === 'wizard' || classId === 'cleric') {
    noise(time, 0.15, 3000, 0.32 * force);
    tone(time, 0.18, 600 + step * 70, 220, 0.2 * force, 'triangle');
  } else {
    noise(time, finisher ? 0.23 : 0.14, 1800 + step * 160, 0.65 * force);
    tone(time + 0.015, 0.09, 180, 65, 0.16 * force);
  }
  if (finisher) kiai(time);
}

export function playImpactSound() {
  if (!context || context.state !== 'running') return;
  const time = context.currentTime;
  if (time - lastImpact < 0.04) return; // area hits share one impact
  lastImpact = time;
  noise(time, 0.07, 900, 0.4, 'lowpass');
  tone(time, 0.08, 125, 50, 0.3);
}
