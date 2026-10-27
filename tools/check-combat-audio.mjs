import puppeteer from 'puppeteer-core';
import { mkdirSync, writeFileSync } from 'node:fs';

const out = 'tools/screenshots/combat-audio';
mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(String(error)));
  await page.evaluateOnNewDocument(() => {
    const Native = window.AudioContext;
    window.AudioContext = class extends Native {
      constructor(...args) {
        super(...args);
        window.testAudio = this;
        window.testOscillators = 0;
        const create = this.createOscillator.bind(this);
        this.createOscillator = () => { window.testOscillators++; return create(); };
        const compressor = this.createDynamicsCompressor.bind(this);
        this.createDynamicsCompressor = () => {
          const node = compressor();
          window.testMeter = this.createAnalyser();
          node.connect(window.testMeter);
          window.testRecording = this.createMediaStreamDestination();
          node.connect(window.testRecording);
          return node;
        };
      }
    };
  });
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  const result = await page.evaluate(async () => {
    const { updatePlayer } = await import('/src/game/player-controller.js');
    const { Player } = await import('/src/entities/player.js');
    const { WorldState } = await import('/src/game/world-state.js');
    const { Camera } = await import('/src/core/camera.js');
    if (window.testAudio.state !== 'running') throw Error('Audio did not unlock');
    const chunks = [];
    const recorder = new MediaRecorder(window.testRecording.stream);
    recorder.ondataavailable = event => chunks.push(event.data);
    recorder.start();
    const stats = [];
    for (const classId of ['warrior', 'wizard', 'mechanic']) {
      const player = new Player(classId);
      const world = new WorldState(player, new Camera());
      world.tilemap = window.__zenonia.session.world.tilemap;
      const input = { wasPressed: () => false, getMoveVector: () => ({ x: 0, y: 0 }) };
      for (let step = 1; step <= 4; step++) {
        const before = window.testOscillators;
        player.combo.startHit(step);
        updatePlayer(world, input, player.combo.currentHit.windupMs, {});
        const oscillators = window.testOscillators - before;
        if (oscillators !== (step === 4 ? 2 : 1)) throw Error(`Wrong attack/voice count: ${classId} ${step}`);
        await new Promise(resolve => setTimeout(resolve, 45));
        const samples = new Float32Array(window.testMeter.fftSize);
        window.testMeter.getFloatTimeDomainData(samples);
        const peak = Math.max(...samples.map(Math.abs));
        if (peak < 0.001) throw Error(`Silent attack: ${classId} ${step}`);
        stats.push({ classId, step, oscillators, peak });
        await new Promise(resolve => setTimeout(resolve, 320));
      }
    }
    const stopped = new Promise(resolve => { recorder.onstop = resolve; });
    recorder.stop(); await stopped;
    const buffer = new Uint8Array(await new Blob(chunks).arrayBuffer());
    let binary = '';
    for (const byte of buffer) binary += String.fromCharCode(byte);
    return { stats, recording: btoa(binary) };
  });
  writeFileSync(`${out}/four-hit-combos.webm`, Buffer.from(result.recording, 'base64'));
  if (errors.length) throw Error(errors.join('\n'));
  console.log(JSON.stringify({ attacks: result.stats.length, voicesOnlyOnFourth: true, audibleSignal: true, errors }));
} finally { await browser.close(); }
