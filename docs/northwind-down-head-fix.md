# Northwind down head registration

The Northwind comparison installer previously baked the idle/down neck94
registration into shared hairstyle textures. Default attacks and walk still
expected canonical neck107, leaving the head above the collar.

Keep the canonical down head textures for every style. Express the smaller
idle head through idle/down anchors only, for the base body and every outfit.
The idle scale is 65/78 and its position includes the breathing scale about
the feet. Attack anchors and canonical head registration stay together.

Before building, `Tools/review-northwind-down-heads.py` exports all 18 down
attack frames for each of six hairstyles at 2x nearest-neighbor zoom.
Reviewed output: `Build/Northwind-T9-Combo/prebuild-review/`.
Runtime captures also include T0/down with styles 0 and 5, alongside T9 sides.

The combo build now installs Northwind T0 clothing for all four attack moves
in all four facings using `install-northwind-default-attacks.py`. These new
body frames use neck94/sole189 and a consistent 65px head, including idle.
This replaces the previous mix of Northwind idle and legacy Wizard attacks.
The installer exports per-frame hairstyle reviews before building; runtime
checks include all four T0 facings and an idle/down reference screenshot.
