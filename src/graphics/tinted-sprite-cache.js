// Colour-washed copies of FX sprites (e.g. the warrior slash tinted purple for the Assassin). Built lazily
// once per (sprite, colour) and cached; the wash only covers the sprite's opaque pixels.
const cache = new WeakMap(); // sprite -> Map<color, canvas>

export function tintedSprite(sprite, color) {
  let byColor = cache.get(sprite);
  if (!byColor) cache.set(sprite, (byColor = new Map()));
  let out = byColor.get(color);
  if (!out) {
    out = document.createElement('canvas');
    out.width = sprite.width;
    out.height = sprite.height;
    const ctx = out.getContext('2d');
    ctx.drawImage(sprite, 0, 0);
    ctx.globalCompositeOperation = 'source-atop';
    ctx.globalAlpha = 0.72;
    ctx.fillStyle = color;
    ctx.fillRect(0, 0, out.width, out.height);
    out.pixelsPerUnit = sprite.pixelsPerUnit;
    byColor.set(color, out);
  }
  return out;
}
