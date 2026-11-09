# Wizard original: ten outfits before Astral

The original is the hooded blue/gold T0 with green, yellow, orange, purple, gold/black, cyan, red, pink and blue/gold higher tiers. `player-clean` belongs to the replacement Astral-like reference design and must not be called the original Wizard. The November 5 player predates installation of all ten outfits.

`Tools/install-wizard-original.py` restores the 106 saved body/head/staff/book/manifest/icon files from `Assets/ArtLibrary/classes/wizard/outfits/reference-20261108/before-install/` into a build copy. It removes later outfit head/held anchor overrides and restores the original idle/down head anchor from Git HEAD. Main assets remain unchanged.

Build with `Tools/build-from-copy.ps1 -WizardOriginal`, using `Build/wizard-reference/project` as CopyDir and `Build/wizard-original/player/Zenonia.exe` as OutExe. Launch `Build/wizard-original/Play-Wizard-Original.cmd`; E/Q or X/Z cycles T0–T9. Screenshots: `Build/wizard-original/captures/`.
