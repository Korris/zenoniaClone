"""Mechanic hit 4, art for the muzzle discharge. NOT WRITTEN YET: this file holds only the prompt, because Tay's image limit stopped the drawing on 2026-11-01.

Until then MechanicCannonBlastFx.cs stretches the combo cast row as a temporary look. The user rejected a round explosion (rocketbarrage/blast-*): it read as something being hit, not a shot
leaving the barrel. Fireball, smoke clouds and debris belong to the impact.

To do when art can be drawn:
- Attach dual-src/hero-test-2-back.png (style and colours only) and fx/attack-orange/cast_2.png (directional density, not its shape).
- Cut to Assets/Resources/Sprites/classes/mechanic/fx/cannon-blast/ with .meta copied from fx/attack-orange/cast_0.png.meta and a new guid. Don't change other files in fx/ or skills/.
- Key on flat #FF00FF, zoom in to check for leftovers. Muzzle point at the same pixel in every frame: that pixel is the sprite pivot.
- Then follow the ART block at the top of MechanicCannonBlastFx.cs.

Usage: python Tools/build-mechanic-cannon-blast.py prompt
"""
import sys

PROMPT = (
    "Generate an image. Draw completely new from scratch, 6 animation frames in ONE horizontal row, left to right, of a huge cannon MUZZLE DISCHARGE for a 2D chibi RPG. "
    "This is a shot leaving a barrel, NOT an explosion: no round fireball, no smoke clouds, no rocks or debris. "
    "Image 1 is the hero and his brass cannon with a blue energy core: use it only for line style and colours, do NOT draw the hero or the cannon. "
    "Image 2 shows a smaller directional flash in the game: match its density and crisp shapes, not its exact outline. "
    "The discharge fires to the RIGHT, seen from the side, in a tight forward cone along one straight horizontal axis. Its start is an invisible muzzle point at the left of each frame, "
    "at the same height and the same distance from the frame's left side in EVERY frame, and frames are equal width. The cone is at least 3 times longer than it is tall. Almost nothing goes backward. "
    "Frame 1: blinding white-blue core star right at the muzzle, first short spike forward. "
    "Frame 2: long white-hot spear forward from the core, electric blue around it, orange-gold flame tongues along the cone, first thin shock ring just ahead of the muzzle, seen edge-on as a narrow upright ellipse. "
    "Frame 3: the longest, full cone of white, blue and orange-gold flame, two shock rings thrown forward along the axis, sparks flying forward only. "
    "Frame 4: cone still long but thinner, rings further forward and wider, core shrinking. "
    "Frame 5: flame breaks into separate forward streaks, one faint ring far ahead, a thin wisp of grey smoke venting backward from the muzzle along the barrel line. "
    "Frame 6: only small streaks and sparks far forward, a small grey wisp at the muzzle. "
    "Every shape is solid, fully opaque, with crisp hard edges, cel-shaded, no soft glow, no blur, no transparency, no gradient haze. "
    "Background is one flat pure magenta #FF00FF everywhere, no shadow, no checkerboard, no ground. Use no pink, no magenta, no purple in the art. "
    "Each frame stays well apart from its neighbours and does not touch the image edge. No text, no frame borders."
)

if __name__ == "__main__":
    if sys.argv[1:] == ["prompt"]:
        print(PROMPT)
    else:
        print(__doc__)
