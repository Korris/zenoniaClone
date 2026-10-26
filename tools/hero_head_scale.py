"""Per-sheet scale for HD hero sheets so the HEAD is the same size in every animation.

ChatGPT paints each animation sheet at its own resolution and character size, so every sheet needs
its own scale. Body height can't be used (crouched dash/attack poses have short bodies and came out
with oversized heads), and hair colour is class-specific. Instead:
  1. the reference sheet (idle) is scaled so its first front-facing frame's body is
     `target_body_height` px tall;
  2. its head (top 42% of that frame's bbox, central 70% to skip the side-held weapon) becomes a
     template, and every other sheet's front-row frames are searched at many scales with normalized
     cross-correlation; the median best-matching scale says how big that sheet's head is relative
     to the reference, and the sheet's scale is divided by it.
Needs opencv-python-headless (`pip install opencv-python-headless`).
"""
import cv2
import numpy as np

HEAD_FRACTION = 0.42      # top part of a standing body bbox that is head + hair
SIDE_TRIM = 0.15          # trim each side of the head band (staff / sword held beside the head)
SEARCH_SCALES = np.arange(0.5, 1.6, 0.02)
MIN_MATCH = 0.6           # ignore frames whose best match is weaker (motion blur, heavy FX)


def _gray(arr):
    """RGBA uint8 -> grayscale float32 composited on mid grey (alpha-independent comparison)."""
    a = arr[..., 3:4].astype(np.float32) / 255
    rgb = arr[..., :3].astype(np.float32) * a + 128 * (1 - a)
    return cv2.cvtColor(rgb.astype(np.uint8), cv2.COLOR_RGB2GRAY).astype(np.float32)


def head_template(body):
    """Head band of a tight-cropped standing body (RGBA array) as a grayscale template."""
    h, w = body.shape[:2]
    return _gray(body[: int(h * HEAD_FRACTION), int(w * SIDE_TRIM): int(w * (1 - SIDE_TRIM))])


def relative_head_scale(templates, rows):
    """Median scale at which each facing's head template best matches that facing's frames.
    `templates[r]` is the reference head for row r (None to skip the row), `rows[r]` that row's
    tight-cropped bodies. Using all four facings keeps one odd pose (a hurt flinch, a dash lean)
    from skewing the result. Returns None if no frame matches well enough."""
    found = []
    for template, bodies in zip(templates, rows):
        if template is not None:
            found += _match_scales(template, bodies)
    return float(np.median(found)) if found else None


def _match_scales(template, bodies):
    """Best-match scale of `template` in each body whose match is good enough."""
    found = []
    for body in bodies:
        if body is None:
            continue
        img = _gray(body)
        best_score, best_scale = -1.0, None
        for s in SEARCH_SCALES:
            t = cv2.resize(template, None, fx=s, fy=s, interpolation=cv2.INTER_AREA)
            if t.shape[0] >= img.shape[0] or t.shape[1] >= img.shape[1]:
                continue
            score = float(cv2.matchTemplate(img, t, cv2.TM_CCOEFF_NORMED).max())
            if score > best_score:
                best_score, best_scale = score, float(s)
        if best_score >= MIN_MATCH:
            found.append(best_scale)
    return found
