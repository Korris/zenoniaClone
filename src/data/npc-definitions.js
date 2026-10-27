// Friendly NPCs. `sprite` picks the pixel-art pack sheet (NPC_SHEETS in asset-atlas.js) used as the
// fallback body + faceset; Oakhaven villagers normally render from the HD sheet keyed by their `id`
// (asset-atlas-hd-npcs.js). `quests` lists the quests this NPC offers, in order. `lines` is idle
// chatter when no quest applies. Roles without gameplay yet (shop, fishing, crafting, healing) are
// dialogue-only on purpose -- no shop/economy system exists, so nothing is stubbed.
export const NPC_DEFINITIONS = Object.freeze({
  elderRowan: {
    id: 'elderRowan', name: 'Elder Rowan', sprite: 'elder', dir: 'down',
    lines: ['Welcome to Oakhaven Village, young one.', 'The road east leads to the Dark Cave. Be careful.'],
    quests: ['slimeTrouble', 'roadToCave'],
  },
  hunterLena: {
    id: 'hunterLena', name: 'Hunter Lena', sprite: 'hunter', dir: 'left',
    lines: ['The bats near the south woods keep stealing my arrows.', 'Press J four times fast for a full combo!'],
    quests: ['batWings'],
  },
  villagerMia: {
    id: 'villagerMia', name: 'Mia', sprite: 'girl', dir: 'down',
    lines: ['Did you know? Spending skill points raises your power a lot.', 'Press TAB to open the skill tree.'],
    quests: [],
  },
  merchantDale: {
    id: 'merchantDale', name: 'Merchant Dale', sprite: 'nomad', dir: 'down',
    lines: ['Potions, trinkets, odds and ends... the stall opens as soon as my cart arrives.', 'Gold from the meadow slimes spends just fine here.'],
    quests: [],
  },
  fishermanRoy: {
    id: 'fishermanRoy', name: 'Fisherman Roy', sprite: 'hunter', dir: 'right',
    lines: ['The river runs clear past the dock. Best bite is at dawn.', 'Mind the current east of the bridge.'],
    quests: [],
  },
  carpenterBrol: {
    id: 'carpenterBrol', name: 'Carpenter Brol', sprite: 'miner', dir: 'left',
    lines: ['Oak for beams, pine for kindling. Everything here I cut myself.', 'Bring me good timber and I might sharpen that sword of yours.'],
    quests: [],
  },
  farmerGuri: {
    id: 'farmerGuri', name: 'Farmer Guri', sprite: 'miner', dir: 'down',
    lines: ['Turnips, carrots and the odd pumpkin. The slimes keep trampling the rows.', 'Shut the gate behind you, would you?'],
    quests: [],
  },
  guardRook: {
    id: 'guardRook', name: 'Guard Rook', sprite: 'knight', dir: 'down',
    lines: ['Halt... ah, it is you. The south road leads to Verdant Meadow.', 'Nothing gets past this gate without my say-so.'],
    quests: [],
  },
  childTim: {
    id: 'childTim', name: 'Tim', sprite: 'girl', dir: 'down',
    lines: ['Are you a real adventurer? Can I see your sword?', 'Mum says I am not allowed past the bridge.'],
    quests: [],
  },
  healerNina: {
    id: 'healerNina', name: 'Nina', sprite: 'girl', dir: 'down',
    lines: ['Herbs from the riverbank, honey from the woods. Rest here if you are hurt.', 'Red potions mend the body, blue ones the spirit.'],
    quests: [],
  },
  minerBork: {
    id: 'minerBork', name: 'Miner Bork', sprite: 'miner', dir: 'right',
    lines: ['Goblins took over my tunnels...', 'Watch out for lava. It burns.'],
    quests: ['goblinRaid', 'boneCollector'],
  },
  nomadSaif: {
    id: 'nomadSaif', name: 'Nomad Saif', sprite: 'nomad', dir: 'down',
    lines: ['The desert is unkind to strangers.', 'Fire slimes explode with heat. Keep moving.'],
    quests: ['fireInTheSand', 'warriorsPride'],
  },
  knightAldric: {
    id: 'knightAldric', name: 'Sir Aldric', sprite: 'knight', dir: 'down',
    lines: ['I guarded this castle in life. Now I only watch.', 'The Cyclops sleeps in the throne room to the north-east.'],
    quests: ['knightsHonor', 'theAncientGolem'],
  },
});
