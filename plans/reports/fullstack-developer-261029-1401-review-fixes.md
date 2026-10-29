# Review fixes report (2026-10-29)
Status: DONE_WITH_CONCERNS. npm test 357/357 (was 334). Browser check: 5 classes New Game, no page errors.

Load size (before title screen): 514 images / 114.1 MB -> 197 images / 31.9 MB.
Per class after pick: warrior 87 / 20.1 MB, wizard 44 / 14.9, mechanic 79 / 19.8, cleric 72 / 17.1, assassin 36 / 11.9 (browser-measured counts match).

Fixes: items 1-7, 9, 10, 13, 14 + extra requirement (death -> auto-return to Oakhaven, 1.4 s, banner, createDeathMenu deleted).
Tests: tests/review-fixes.test.js, tests/class-asset-loader.test.js, tests/gear-icons.test.js (renamed), updated offhand/enhance/loot/assassin/held tests.
Concerns: other-class drop icons use painted atlas fallback until that class is loaded; ultimateIcons fallback sheet shared by warrior+mechanic sets; class-select shows portraits not animated idle; weapon-overlay-manifest-generated.js kept (test imports it).
