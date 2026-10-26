// Top-left minimap: the tilemap rendered once per map at 1px/tile, then scaled, with live dots
// for the hero (yellow), monsters (red), boss (big red) and NPCs with quests (gold).
import { drawPanel } from './pixel-text.js';
import { getTileInfo } from '../world/tile-char-info.js';
import { getPropDefinition } from '../world/prop-definitions.js';

const TILE_COLORS = {
  grass: '#8bc34a', dirtBlock: '#e8a860', waterBlock: '#5fc8f0', cliffWall: '#b07a48', sand: '#f0b870',
  castleFloor: '#7c8c80', caveFloor: '#4a3c30', castleWall: '#4a4a58', caveWall: '#55606a', lava: '#ff7a2a',
  bridge: '#d89050', portal: '#e0a0ff', forest: '#2f5e2c',
};
const PROP_COLORS = {
  pine: '#2f7a3a', pineDark: '#2a6a34', roundTree: '#3f9e3c', smallTree: '#3f9e3c', bigTree: '#3f9e3c', deadTree: '#7a4a30',
  bush: '#4faa44', house: '#e05a3a', house2: '#d8b080', house3: '#e05a3a', rockBrown: '#b07050', rockGrey: '#8a9a8a',
  rockSmallGrey: '#8a9a8a', rockSmallBrown: '#b07050', palm: '#3f9e3c', pillar: '#9aa89a',
  bush2: '#5fb850', stones: '#a0a8a0', stump: '#8a5a3a', fence: '#c89860', signpost: '#c89860',
  torch: '#ffb040', crystal: '#6fd0ff', cactus: '#3f9e3c', bones: '#e0d8c0',
  elderHouse: '#4a6fc0', cottageRed: '#e05a3a', hunterLodge: '#8a5a3a', healerCottage: '#5f9a3c',
  carpenterWorkshop: '#a8703c', marketStall: '#e06060', well: '#9aa0a8', villageOak: '#3f9e3c', villagePine: '#2f7a3a',
};

const SCALE = 3;
const PANEL_X = 12;
const PANEL_Y = 12;
let cachedMapId = null;
let cachedCanvas = null;

function buildMapCanvas(world) {
  const { tilemap, mapDef } = world;
  const canvas = document.createElement('canvas');
  canvas.width = tilemap.width;
  canvas.height = tilemap.height;
  const ctx = canvas.getContext('2d');
  for (let ty = 0; ty < tilemap.height; ty++) {
    for (let tx = 0; tx < tilemap.width; tx++) {
      const info = getTileInfo(tilemap.charAt(tx, ty));
      ctx.fillStyle = TILE_COLORS[info.minimap || info.sprite] || '#000';
      ctx.fillRect(tx, ty, 1, 1);
    }
  }
  for (const p of mapDef.props || []) {
    const color = PROP_COLORS[p.type];
    if (!color) continue;
    const { footprint } = getPropDefinition(p.type);
    ctx.fillStyle = color;
    ctx.fillRect(p.tx, p.ty - footprint.h + 1, footprint.w, footprint.h);
  }
  return canvas;
}

function dot(ctx, originX, originY, worldX, worldY, size, color) {
  ctx.fillStyle = color;
  const x = originX + Math.round((worldX / 16) * SCALE) - size / 2;
  const y = originY + Math.round((worldY / 16) * SCALE) - size / 2;
  ctx.fillRect(x, y, size, size);
}

/**
 * @param {CanvasRenderingContext2D} ctx screen-space context
 * @param {import('../game/world-state.js').WorldState} world
 * @param {Record<string,string>} npcMarkers npcId -> marker kind
 */
export function drawMinimap(ctx, world, npcMarkers = {}) {
  if (cachedMapId !== world.mapId) {
    cachedCanvas = buildMapCanvas(world);
    cachedMapId = world.mapId;
  }
  const w = cachedCanvas.width * SCALE;
  const h = cachedCanvas.height * SCALE;
  drawPanel(ctx, PANEL_X, PANEL_Y, w + 12, h + 12, { fill: '#0d0d14', border: '#d9b45a' });
  const ox = PANEL_X + 6;
  const oy = PANEL_Y + 6;
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(cachedCanvas, ox, oy, w, h);

  for (const m of world.monsters) dot(ctx, ox, oy, m.x, m.y, m.isBoss ? 6 : 3, m.isBoss ? '#ff3030' : '#e04040');
  for (const n of world.npcs) dot(ctx, ox, oy, n.x, n.y, 4, npcMarkers[n.id] ? '#ffe040' : '#ffffff');
  const blink = Math.floor(performance.now() / 300) % 2 === 0;
  dot(ctx, ox, oy, world.player.x, world.player.y, 5, blink ? '#ffff80' : '#ffd000');
  return { x: PANEL_X, y: PANEL_Y, w: w + 12, h: h + 12 };
}
