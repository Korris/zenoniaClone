# Oakhaven v4 — Plaza prototype

This iteration is deliberately limited to the first quality bar: the north landmark and central plaza.

## Changes
- Added a dedicated `plazaStone` tile semantic, backed by the existing hand-painted stone floor atlas cells.
- Replaced the central dirt/old floor plaza cells with the stone-paving semantic in a stepped, irregular footprint around the well.
- Added `elderHouse.png`, a 1.5x transparent-alpha enlargement of the existing blue-roof house art, to give the elder's residence more visual weight without introducing a mismatched art style.
- Registered `elderHouse` with an appropriate 6x3 walk-blocking footprint and a fallback to the existing house sprite.
- Kept the map size, NPC IDs, quests, portal flow, and general renderer unchanged.

## Build / validation
Run `npm run hd:village-props` to regenerate the village landmark assets, then `npm test`. The full test suite passes in the working copy (116/116). A browser screenshot could not be captured in this environment because the local `puppeteer-core` dependency is not installed; the PNG assets and map are not claimed to have been verified in live browser gameplay yet.

## Known limitation
This is a first-zone prototype, not a complete recreation of the full concept. The next iteration should add a hand-painted modular paving-edge kit and a bespoke elder-house asset rather than relying on scaling the existing house sprite.
