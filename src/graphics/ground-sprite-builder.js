// Derive compact drop artwork from the inventory image, preserving its silhouette and palette.
export function buildGroundSprite(source, screenSize = 26.4) {
  const target = Math.ceil(screenSize) * 2; // retain native detail on 2x displays
  let image = source;
  while (Math.max(image.width, image.height) > target * 2) {
    const next = document.createElement('canvas');
    const ratio = Math.max(target * 2 / Math.max(image.width, image.height), 0.5);
    next.width = Math.max(1, Math.round(image.width * ratio));
    next.height = Math.max(1, Math.round(image.height * ratio));
    const ctx = next.getContext('2d');
    ctx.imageSmoothingEnabled = true;
    ctx.imageSmoothingQuality = 'high';
    ctx.drawImage(image, 0, 0, next.width, next.height);
    image = next;
  }
  const canvas = document.createElement('canvas');
  const ratio = target / Math.max(source.width, source.height);
  canvas.width = Math.max(1, Math.round(source.width * ratio));
  canvas.height = Math.max(1, Math.round(source.height * ratio));
  const ctx = canvas.getContext('2d');
  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'high';
  ctx.drawImage(image, 0, 0, canvas.width, canvas.height);
  const frame = ctx.getImageData(0, 0, canvas.width, canvas.height);
  const original = new Uint8ClampedArray(frame.data);
  // Smooth isolated color flecks within surfaces, without mixing metal, gems or outlines.
  for (let y = 0; y < canvas.height; y++) {
    for (let x = 0; x < canvas.width; x++) {
      const i = (y * canvas.width + x) * 4;
      if (original[i + 3] < 16) { frame.data[i + 3] = 0; continue; }
      const sum = [0, 0, 0];
      let weight = 0;
      for (let dy = -1; dy <= 1; dy++) {
        for (let dx = -1; dx <= 1; dx++) {
          const nx = x + dx, ny = y + dy;
          if (nx < 0 || ny < 0 || nx >= canvas.width || ny >= canvas.height) continue;
          const j = (ny * canvas.width + nx) * 4;
          if (original[j + 3] < 128) continue;
          const difference = Math.abs(original[i] - original[j])
            + Math.abs(original[i + 1] - original[j + 1]) + Math.abs(original[i + 2] - original[j + 2]);
          const w = Math.exp(-difference / 65) * (dx === 0 && dy === 0 ? 2 : 1);
          weight += w;
          for (let c = 0; c < 3; c++) sum[c] += original[j + c] * w;
        }
      }
      if (weight) {
        for (let c = 0; c < 3; c++) {
          const smooth = sum[c] / weight;
          // Mild local contrast recovers edges after reducing the fine texture.
          frame.data[i + c] = smooth + (original[i + c] - smooth) * 0.25;
        }
      }
    }
  }
  ctx.putImageData(frame, 0, 0);
  return canvas;
}
