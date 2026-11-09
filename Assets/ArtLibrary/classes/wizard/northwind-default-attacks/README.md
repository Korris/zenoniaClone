Northwind default T0 costume repaint for attack1-4, all four directions.
Generated with built-in imagegen from current Wizard combo pose sheets and
Northwind V7 T0 body reference. Prompts are in prompts.json.

72 headless body frames, with independent hairstyle and held-gear layers.
Uniform source fitting uses collar94/sole189, with 65px head height matching
idle/down. Side collar overlap is registered separately from the jaw pivot.
Other idle directions use the first Northwind stance with subtle breathing.

Collar coordinates are explicitly registered per painted source frame in
collar-landmarks.json. Body registration uses the feet; the head follows the
measured collar of that exact frame. One body scale is shared across the
frames of each animation/facing to prevent normalization jitter. Head-space
neck joints map to the collar exactly (asserted before export). The open neck
is part of body anatomy underneath the interchangeable hairstyle layer.

Tools/install-northwind-default-attacks.py installs into the build copy and
exports magnified per-frame/style reviews before the Unity build is started.
T9 side combos are installed afterward by install-northwind-t9-combos.py.
