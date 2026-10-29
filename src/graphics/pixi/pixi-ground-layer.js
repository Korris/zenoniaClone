// The map floor in the PixiJS scene. Maps with a painted backdrop (Oakhaven, Frost Hollow) just show that image
// as one sprite. Tile-built maps are baked ONCE per map entry into a single offscreen canvas by the existing
// tilemap renderer (so autotiling, variants and HD tiles look identical), then uploaded as one texture. Only
// the animated portals are drawn live, as small sprites cycling their frames on top.
import { TILE_SIZE, VIEW_WIDTH, VIEW_HEIGHT } from '../../core/constants.js';
import { getTileInfo } from '../../world/tile-char-info.js';
import { drawTilemap } from '../tilemap-renderer.js';
import { placeSprite } from './pixi-actor-node.js';

const BAKE_PPU = 4; // baked pixels per world unit: matches the 64px HD tile cells (16 units)
const MAX_BAKE_SIDE = 8192;
const PORTAL_FRAME_MS = 140;

/** Whole-map floor as one canvas, painted in view-sized chunks through the regular tile renderer. */
export function bakeTilemap(tilemap, registry, ppu = BAKE_PPU) {
  const scale = Math.min(ppu, MAX_BAKE_SIDE / Math.max(tilemap.pixelWidth, tilemap.pixelHeight));
  const canvas = document.createElement('canvas');
  canvas.width = Math.ceil(tilemap.pixelWidth * scale);
  canvas.height = Math.ceil(tilemap.pixelHeight * scale);
  const ctx = canvas.getContext('2d');
  const blank = document.createElement('canvas');
  blank.width = blank.height = 1;
  const noPortals = { ...registry, tiles: { ...registry.tiles, portal: [blank] } }; // portals are live sprites, not baked
  const camera = { renderX: 0, renderY: 0 };
  for (let y = 0; y < tilemap.pixelHeight; y += VIEW_HEIGHT) {
    for (let x = 0; x < tilemap.pixelWidth; x += VIEW_WIDTH) {
      camera.renderX = x; camera.renderY = y;
      ctx.setTransform(scale, 0, 0, scale, -x * scale, -y * scale);
      drawTilemap(ctx, tilemap, camera, noPortals, 0, null);
    }
  }
  return canvas;
}

export class GroundLayer {
  constructor(env, container) {
    this.env = env;
    this.container = container;
    this.mapKey = null;
    this.baked = null;
    this.portals = [];
  }

  reset() {
    for (const child of this.container.removeChildren()) child.destroy();
    this.baked?.destroy(true);
    this.baked = null;
    this.portals = [];
  }

  rebuild(world, registry) {
    const { PIXI } = this.env;
    this.reset();
    const { tilemap } = world;
    const backdrop = registry.backdrops?.[world.mapDef?.backdrop] || null;
    let ground;
    if (backdrop) {
      ground = new PIXI.Sprite(this.env.tex.of(backdrop));
      ground.scale.set(tilemap.pixelWidth / backdrop.width, tilemap.pixelHeight / backdrop.height);
    } else {
      this.baked = PIXI.Texture.from(bakeTilemap(tilemap, registry), true);
      this.baked.source.scaleMode = 'linear';
      ground = new PIXI.Sprite(this.baked);
      ground.scale.set(tilemap.pixelWidth / this.baked.width, tilemap.pixelHeight / this.baked.height);
    }
    this.container.addChild(ground);
    for (let ty = 0; ty < tilemap.height; ty++) {
      for (let tx = 0; tx < tilemap.width; tx++) {
        const ch = tilemap.charAt(tx, ty);
        const info = ch === null ? null : getTileInfo(ch);
        if (info?.kind !== 'animated' || !registry.tiles[info.sprite]) continue;
        const sprite = new PIXI.Sprite();
        this.container.addChild(sprite);
        this.portals.push({ sprite, frames: registry.tiles[info.sprite], x: tx * TILE_SIZE, y: ty * TILE_SIZE });
      }
    }
  }

  update(world, registry) {
    const key = `${world.mapId}|${world.tilemap.width}x${world.tilemap.height}`;
    if (key !== this.mapKey || this.tilemap !== world.tilemap) {
      this.mapKey = key;
      this.tilemap = world.tilemap;
      this.rebuild(world, registry);
    }
    const { env } = this;
    for (const portal of this.portals) {
      const frame = portal.frames[Math.floor(world.elapsedMs / PORTAL_FRAME_MS) % portal.frames.length];
      placeSprite(portal.sprite, env.tex.forSize(frame, TILE_SIZE, TILE_SIZE, env.scale), portal.x, portal.y, TILE_SIZE, TILE_SIZE, env.scale);
    }
  }
}
