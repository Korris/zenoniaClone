# Astral Scholar V8 — separate comparison player

V8 installs the approved new ruby/gold T9 armor and matching astrolabe staff/book. T0–T8 clothing remains V7. T9 body is fitted uniformly to the T0 chin/sole landmarks and uses the same head, keeping the character 160px high. Mantle and pauldrons spread sideways without scaling anatomy. Sources/prompts: `Assets/ArtLibrary/classes/wizard/outfits/astral-v8/` (built-in imagegen).

Staff target lengths increase by9px per tier:160,169,178,187,196,205,214,223,232,241px. Actual alpha-bound measurements are checked strictly increasing in `Build/wizard-astral/staff-height-report.json`. Grip and hand overlays are recalculated; the staff changes size across tiers, and uses the same exported size across directions/actions within a tier. The launcher uses zoom42 to give the larger staff head room below the HUD.

V8 captures: `Build/wizard-astral/captures-v8/`, captured with `Tools/check-wizard-height.ps1 -IdleOnly -Zoom 42`. Board: `docs/reference/wizard-astral-v8-all-tiers-ingame.png`, composed with `Tools/compose-astral-v8.py`. Earlier V7 screenshots remain under `captures/`.

Launch `Build/wizard-astral/Play-Astral.cmd`. E/Q or X/Z changes the outfit tier.

This variant restores the raised staff hand, book hand, bell sleeves and leg pose from the Astral Scholar V7 concept. Body fitting uses one uniform scale for both axes; it does not reposition individual limbs to the battle-mage anchors. The default head uses the original T0 proportions. All ten tiers share the same 160px character height and floor line, with separately fitted staff and book anchors.

Sources and generation prompts: `Assets/ArtLibrary/classes/wizard/outfits/astral-v7/`. The installer wrapper `Tools/install-wizard-astral-v7.py` reuses the copy-only extraction/export pipeline. Extracted layers and inspection previews stay in `Build/wizard-astral/`.

Rebuild:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-from-copy.ps1 -CopyDir D:/zenoniaClone/Build/wizard-reference/project -OutExe D:/zenoniaClone/Build/wizard-astral/player/Zenonia.exe -Log D:/zenoniaClone/Build/wizard-astral/build.log -WizardAstral
```

Scope: idle facing down, four breathing frames, ten outfits plus matching staff/book and icons. Walking, attacks and other directions retain existing art. The main Wizard Resources are hashed before/after installation and remain unchanged. Northwind and the current battle-mage executables remain separate.

Validation: exporter checks 30 sheets, 40 fist pairs, outfit anchors, clipping and preserved other directions. Actual player screenshots are captured with `Tools/check-wizard-height.ps1 -IdleOnly` into `Build/wizard-astral/captures/`.
