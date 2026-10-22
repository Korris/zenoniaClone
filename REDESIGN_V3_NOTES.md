# Oakhaven Village redesign v3

This pass keeps the existing 36x26 map footprint and gameplay IDs while tightening the village composition.

## Changes
- Reworked Oakhaven's residential row into a closer cluster and added a distinct healer cottage and carpenter's yard building using existing house sprites.
- Changed the central plaza from dirt to a smaller, stepped stone-paved space around the fountain.
- Widened the main dirt routes and added feeder paths from the residential doors, market, workshop, and river approach.
- Added more deliberate tree, bush, and flower clusters to frame the settlement without blocking NPCs or the portal.
- Grouped market stalls into a compact market row and added crop rows to the farm.
- Added a continuous `bridgeArch` overlay over the existing walkable bridge tiles, plus a redesigned stone fountain sprite.
- Updated the HD path tile build output so the repeated interior dirt cell no longer carries the source sheet's obvious cell-border strokes.

## Rebuild generated assets
```bash
npm run hd:tiles
npm run hd:village-props
npm test
npm start
```

## Validation
- `npm test`: 116 tests passed.
- Browser screenshot validation could not be completed in this environment because the local `puppeteer-core` install did not finish correctly. Use the game browser to confirm visual layering and collision in your local environment.

This is still a tile-and-prop implementation, not a static screenshot background. It is a closer playable layout, but it does not reproduce every hand-painted detail of the generated concept image.
