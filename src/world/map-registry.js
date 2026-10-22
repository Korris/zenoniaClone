// Lookup of all map definitions by id, plus the starting map.
import { OAKHAVEN_VILLAGE_MAP } from './maps/oakhaven-village-map.js';
import { VERDANT_MEADOW_MAP } from './maps/verdant-meadow-map.js';
import { DARK_CAVE_MAP } from './maps/dark-cave-map.js';
import { SCORCHED_DESERT_MAP } from './maps/scorched-desert-map.js';
import { RUINED_CASTLE_MAP } from './maps/ruined-castle-map.js';

export const MAP_REGISTRY = Object.freeze({
  [OAKHAVEN_VILLAGE_MAP.id]: OAKHAVEN_VILLAGE_MAP,
  [VERDANT_MEADOW_MAP.id]: VERDANT_MEADOW_MAP,
  [DARK_CAVE_MAP.id]: DARK_CAVE_MAP,
  [SCORCHED_DESERT_MAP.id]: SCORCHED_DESERT_MAP,
  [RUINED_CASTLE_MAP.id]: RUINED_CASTLE_MAP,
});

export const FIRST_MAP_ID = OAKHAVEN_VILLAGE_MAP.id;

export function getMapDefinition(id) {
  return MAP_REGISTRY[id] || MAP_REGISTRY[FIRST_MAP_ID];
}
