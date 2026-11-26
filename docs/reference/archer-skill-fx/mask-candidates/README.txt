Archer light-skill masks: ChatGPT candidates (2026-11-26). CANDIDATES ONLY: nothing in Assets/ was changed. Waiting for user approval.

Chat: "Thiết kế Ranger Asteria" (Tay account) https://chatgpt.com/c/6b0794c9-1f18-83ec-ab80-b7e41e9ca172
Raw ChatGPT sheet (2x2, grey on pure black #000000, 1254x1254): raw/archer-masks-chatgpt-1.png

Candidates (game format, drop-in: same size as the current file, RGB grey on black, shader reads red, centred like the old mask):
  archer-eye-chatgpt-1.png        256x256  Eagle Eye hawk eye glyph over the head (Glyph / Gold). Width matched to the old eye.
  archer-vine-ring-chatgpt-1.png  512x512  Binding Shot vine ring, top-down (Line / Vine). Outer extent matched to the old ring.
  archer-leaf-chatgpt-1.png       128x128  leaf particle, tip up (Glyph / Leaf). Height matched.
  archer-wind-arc-chatgpt-1.png   256x256  wind crescent bulging to +x (Line / Wind|Leaf). Height matched.

Review sheet: compare-old-vs-new.png (old vs new: raw | tinted on dark | tinted on snow, emulating LightBlob Line/Glyph roles
of ClericLightLook.M, no Bloom; bottom = mock on a game screenshot at ~4.8 px/unit).

Swap (only after the user approves; .png.meta files stay untouched so GUIDs / import settings are kept):
  cd D:/zenoniaClone
  for n in archer-eye archer-vine-ring archer-leaf archer-wind-arc; do
    cp docs/reference/archer-skill-fx/mask-candidates/$n-chatgpt-1.png Assets/Resources/Sprites/fx/light-skill/$n.png
  done
(Swap only the approved names. Note: re-running Tools/light_skill_textures_archer.py would overwrite them with the code versions again.)
