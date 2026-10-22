# Oakhaven V5 — Elder House Prototype

- Enlarged the Elder House prop canvas to 448x336 RGBA (7x5.25 tiles at 4 px/world unit).
- Kept the existing painted blue-roof house facade as the style anchor.
- Added two small blue/gold entrance banners and flower planters to strengthen its landmark silhouette.
- Updated the prop definition and reserved the wider roof-overhang area on the map.
- Regeneration command: `npm run hd:village-props` (after `npm run hd:props` if the base normalized house is missing).

Validation: `npm test` passed 116/116. `npm run check:browser` could not run because `puppeteer-core` was not installed in the current working environment; no in-browser gameplay screenshot was verified.
