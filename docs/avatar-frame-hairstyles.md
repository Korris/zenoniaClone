# Complete animation frames with interchangeable hair

The runtime selects a complete body/face/neck/hair frame, using the same
animation, facing row and frame index as the body and equipment. It hides
the old separate Head SpriteRenderer for these compiled assets. Existing
non-converted assets retain their original rendering path.

Assets are authored as independent hair-only parts and bald anatomy in
`Assets/ArtLibrary/avatar-hair-parts/<class>/`. Each class has front/back/right
views (left mirrors right). Hair accessories stay with the hair; facial
features and Assassin's mouth mask belong to the anatomy. Imagegen source
contacts, outputs and prompts are retained. `import-avatar-hair-parts.py`
registers the parts to canonical 192px head cells.

`compile-avatar-hair-frames.py --target <project-copy>` uses the original bare
frame as the anatomy/pose master. The default style is asserted pixel-identical
to that master. Alternative styles retain visible authored facial features;
bald anatomy fills formerly occluded areas. Long back hair is composited behind
the body, fringe in front. Equipment and fist overlays remain separate.

Output: `body/appearance/<style>/<anim>.png`, plus outfit-specific equivalents
where authored bare/headless sheets exist. Coverage follows the existing
head-anchor animation lists: idle, walk and each class's combo moves. Other
animations without head-layer inputs retain their existing fallback assets.

Build:

    ./Tools/build-from-copy.ps1 -CopyDir C:/temp/zu-copy -OutExe C:/Project/zenoniaUnity/Build/Avatar-Hair-Frames/Zenonia.exe -WizardNorthwind -WizardNorthwindCombos -FrameHairstyles

Classes: Mechanic, Northwind Wizard, Warrior, Assassin, Cleric. Existing hairstyle
indices/H key and network appearance messages stay compatible. This does not
add account-creation UI or alter stored appearance identifiers.

Pre-build magnified reviews are written under `Build/avatar-hair-frames/review/`.
Runtime validation uses each available hairstyle, down-facing basic attacks,
and additional up/side cases to check mirroring, weapon layers and body motion.
