Northwind T9 side animation assets, generated with built-in imagegen from
`outfits/northwind-v8/t9-separated.png`.

Source layout: four columns, four rows: left idle, left attack, right idle,
right attack. Every row contains four frames. Bodies exclude heads, hair and
held equipment so those can remain separate game layers.

`*-headless.png`: 192px-cell horizontal strips.
`frames/`: individual transparent 192px frames.
`*.gif`: looping review with existing default side head layered separately.
`review-with-head.png`: all sixteen frames with the same separate head.

These are exported art assets, not yet installed into live Resources.

Revision 2 uses the actual default `body/bare/idle.png` and `attack1.png`
as pose references. Source-v2 rows 3 and 4 provide left/right attacks;
idle reuses the initial side stance with a 0/1/2/1px breathing offset.
Heads retain their canonical 192px pivot and are attached to each collar,
rather than centering a cropped hair silhouette at a fixed chin height.
