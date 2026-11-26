# Warrior skeletal rig prototype (down view) — plan

Status: test build done (25/11), design chat final: idle 8.7, walk 8.6, slash 8.5 PASS; waiting for the user verdict. Branch `ro-soft-adventure`. Do NOT commit (test build only).

## Goal
Test build: Warrior (Starward Knight V3) facing camera drawn by a 2D bone rig (idle breathing, walk, one slash) so the user can
compare smoothness with the current frame art (idle 1 f, walk 8 f @75 ms, attack 8 f). Behind flag `--hero-rig`; other
directions and no-flag runs keep the frame art. Gameplay / hit timing unchanged (visual only).

## Route chosen: (b) cut-out rig, bone hierarchy + forward kinematics evaluated in code (HeroRigPose), no package
- (a) Unity 2D Animation (SpriteSkin) needs bones + weights authored in the Skinning Editor or written into the sprite
  `.meta` through `ISpriteEditorDataProvider` in an editor batch job. We cannot click Unity, the user keeps the project
  open, and builds run from a copy: a scripted skinning import is fragile (package version, meta format, PSB-less sprites)
  and adds a package to both projects for a 1-direction test.
- (b) Same visual result for a chibi knight in armour (rigid plates; ChatGPT's own analysis says deformation only pays on the
  cape): each painted part is a SpriteRenderer on a bone Transform (pelvis -> torso -> head / shoulders -> upper arm -> forearm;
  pelvis -> thigh -> shin). Poses = keyframes in a small table (angles, offsets) interpolated with smooth curves every frame
  from a CONTINUOUS phase (walk cycle phase, slash progress from `WarriorSlashFeel`, idle time) instead of a frame index.
- Cape: separate pieces with sway lag (secondary motion) — mesh bend only if the pieces look stiff.
- Hair (dye material `HeroRecolor`), sword, shield: existing layers, parented to head / hand bones.

## Steps
1. Art (ChatGPT design chat, Tay; fallback Tan Pham): separated parts sheet on #00FF00 matching the approved idle-down cell
   + assembled reference + animation direction (breath amounts, walk key poses / timing, slash keys / timing).
   Sources `Assets/ArtLibrary/ro-fantasy-heroes/src/rig-down/`, refs + prompts `ref/rig-down/`.
2. Tool `Tools/build-warrior-rig-down.py`: cut parts (key out green, trim), scale to the in-game cell (bald body 200 px,
   cell 320x256, feet 244), measure pivots (joints), write `Sprites/hero-layered/ro-fantasy/warrior/rig-down/*.png`
   + `Data/hero-layered/ro-fantasy/warrior/rig-down.json` (parts, parent, pivot, rest position, sorting order),
   assembled check vs idle cell.
3. Code: `HeroRigView.cs` (build hierarchy, apply pose), `HeroRigPoses.cs` (keyframes + evaluation), hook in
   `HeroRenderer.Layered.ShowLayered` when `--hero-rig` and Dir8 == down and anim in idle / walk / attack;
   continuous phase passed from `HeroController.Render`.
4. Build from copy (`Tools/build-from-copy.ps1 -CopyDir D:\temp\zu-copy-b`), capture rig vs frame (same exe, flag on / off),
   sheets + GIFs in `docs/reference/character-v3/rig-down/`; ChatGPT verdict, up to 2 fix rounds.
5. dotnet tests; sprite-rules Sổ lỗi; one line in `docs/pending-tasks.md`.

## Risks
- Paper-doll look (visible joints): parts painted with hidden overlap; check every pose at game size.
- Parts not matching approved idle: assemble rest pose and diff against idle cell before rigging.
- Hair anchor: from the head bone (rest = idle head anchor).

## Phase 2 mesh rig (25/11, `--hero-rig-mesh`; cut-out rig stays on `--hero-rig`)
Status: test build `D:/temp/player-rigmesh/Zenonia.exe` runs (SpriteSkin Ready, log `mesh rig on: 5 meshes, 15 bones, SpriteSkin`); NOT scored by the design
chat (the agent had no browser tool; prompt ready in `Assets/ArtLibrary/ro-fantasy-heroes/ref/rig-down/prompt-02-mesh-rig-keyposes-and-parts-NOT-SENT.txt`). Do NOT commit.

Why the cut-out rig looked like a puppet, and what Phase 2 does:
1. 16 rigid parts hinged on one pin -> 4 skinned meshes from the APPROVED idle cell (cape behind, body = head + torso + belt + skirt + legs in ONE mesh, sword arm,
   shield arm; arms separate only because they cross / change order). 4 px triangle grid, up to 4 bone weights per vertex: knees / hips / elbows / shoulder pads bend.
2. Front view, limbs swung in the screen plane -> keys scale a limb ALONG its bone (`sy`, rest axis per bone): lifted knee = shorter thigh + shin, foot up ~12 px;
   stance foot solved to stay on the floor (`plant`); angles 2-3 deg. Slash: wind-up up-left, ease-in chop in 2 segments, at the blow the forearm thrusts TOWARD the
   camera (sy 0.66 / sx 1.12) and the slot swaps to a painted fist-forward piece (sprite swap, `sw` "armL=thrust", cut from the approved frame-art slash f4).
3. Even Catmull-Rom -> contact key + 24 ms contact hold (hips keep sinking), ease-in into each foot strike, back-out overshoot after the blow, hold, settle;
   cape + hair = damped springs (2.6 Hz, 0.38) leaning against joint velocity (lag + overshoot without keys).
4. Typed numbers -> timing / key moments from the design chat's spec V1.0 + its 3 rounds (ref/rig-down/animation-spec-and-reviews-chatgpt.txt) and the approved
   frame art; a fresh ChatGPT pass for foreshortened key poses is still OPEN (see status).

Pipeline (all scripted, nothing in the Unity editor):
- `Tools/build-warrior-rig-mesh-down.py write`: regions/textures (`warrior_rig_mesh_regions`), bones + weight rules BY BONE NAME (`_skeleton`), grid mesh + weights +
  2 Laplacian passes, face kept rigid (`_geometry`), keys (`_anims`), swap piece (`_swap`, stored in rest coords through the forearm's inverse affine at the blow).
  `preview DIR [anim]` = offline LBS render (mirror of the game, `_pose` + `_render`); `mesh OUT.png` = wireframe + top-weight colours.
- Game: `HeroMeshRigSet(.cs/Sprites.cs)` builds runtime Sprites and writes positions / uvs / indices / BlendWeight / bind poses / SpriteBones with
  `UnityEngine.U2D.SpriteDataAccessExtensions` (2D Animation 13.0.6, already in `com.unity.feature.2d`, no package added); `HeroMeshRigView` = flat bone Transforms
  (TRS = joint, rest axis + turn, own sx / sy: no scale inheritance -> no shear) + SpriteRenderer + SpriteSkin per slot; `HeroMeshRigCpuSkin` = fallback.
  `IHeroRig` lets `HeroLayeredView.Rig` hang hair / sword / shield on either rig.
- SpriteSkin vs CPU give the same picture: both compute v' = sum_i w_i * (bone_i.localToWorld relative to the renderer) * bindPose_i * v over the same vertices,
  weights and bind poses (SpriteSkin: `rootInv * bone * bindPose`, Burst job; CPU: same matrices in C#), only who runs the loop differs.
- New armour / outfit later: paint a parts set in the same 320 x 256 cell layout (joints at the `_skeleton.BONES` places), drop it in as the region textures and
  re-run `write`: mesh + weights are regenerated from bone-name rules, the skeleton, keys, springs and game code stay. Hair = existing dyeable layer on the `hair`
  spring bone; weapons = existing sprites on the forearm grips.

Checks done: in-game captures (`--record-rig`, same exe, 3 runs) composed by `Tools/compose-warrior-rig-mesh-compare.py` into
`docs/reference/character-v3/rig-down/mesh/` (compare-{idle,walk,attack}.jpg rows frame art / cut-out / mesh, compare-3way-{walk,attack}.gif, zooms, wireframe).
Open: ChatGPT key poses + score (2 rounds), ChatGPT parts set (body without arms, full cape, arm raised, fist toward camera) to replace the idle-cut textures.

### Phase 2 round 2 (user: "vẫn giống con rối lắm") — amplitudes from the approved frame art
Measured with `Tools/measure-warrior-down-motion-amplitude.py --mesh` (cell px, same metric on frame art and the mesh's offline render):
| | frame art | mesh r1 | mesh r2 |
|---|---|---|---|
| walk boot lift L / R | 12 / 9 | 14 / 14 | 15 / 15 |
| walk head bob / head x / chest x | 5 / 0.7 / 2.4 | 6 / 3.3 / 4.7 | 7 / 2.1 / 5.5 |
| walk glove y (brown centroid) L / R | 13.5 / 19 | 6.3 / 5.9 | 8.0 / 6.6 (hand point 15.4) |
| attack crouch (head drop, max) | 24 (range 15) | 8 | 18 |
| attack head x range | 15 (left at blow) | 33 (right 20 at blow, wrong way) | 18 (left at blow, right after) |
| attack sword-side boot step | 7 | 1 | 6 |
| attack glove L x / y | 15 / 15 | 18 / 24 | 17.5 / 20 |
Changes: pelvis tilt + hip shift to the stance leg, torso counter, head lag + steady head, big arm swing (forearm foreshortened + elbow bend),
contact squash on the landing leg; slash crouch to 18 px with knees opening (feet solved to stay planted), lean to the sword side, squash 0.92
at the blow then stretch 1.02, sword-side step, overshoot + hold + slow return. Cape backdrop with a smooth edge. Timing / blow frame unchanged.
Limits: deeper crouch squashes shins / boots > 17 %; the glove centroid metric stays below the art because a forearm coming toward the camera can
only get shorter on this art (the fist does not grow): needs painted pieces (see prompt-02 file: arm raised, fist toward camera, bent-knee legs).
Images: `docs/reference/character-v3/rig-down/mesh/` (`compare-*.jpg` = r2, `*-r1.jpg` = round 1, contact sheets every 33 ms, legs zoom).
