// Quest data. Objectives: {type:'kill', monster, count} or {type:'reach', map}.
// `prerequisite` must be completed before the quest is offered; `requiredLevel` gates it too.
const kill = (monster, count) => ({ type: 'kill', monster, count });
const reach = (map) => ({ type: 'reach', map });

export const QUEST_DEFINITIONS = Object.freeze({
  slimeTrouble: {
    id: 'slimeTrouble', title: 'Slime Trouble', giver: 'elderRowan', requiredLevel: 1, prerequisite: null,
    description: 'Clear 5 slimes from the meadow fields.', objective: kill('slime', 5), rewards: { exp: 60, gold: 20 },
    dialog: {
      offer: ['Slimes are eating our crops again.', 'Could you clear five of them from the fields?'],
      progress: ['The slimes are still out there. Keep at it!'],
      complete: ['The fields are safe again. Thank you, hero!'],
    },
  },
  batWings: {
    id: 'batWings', title: 'Bat Wings', giver: 'hunterLena', requiredLevel: 2, prerequisite: null,
    description: 'Hunt 4 bats in the southern woods.', objective: kill('bat', 4), rewards: { exp: 90, gold: 30 },
    dialog: {
      offer: ['Bats keep swarming the south woods at dusk.', 'Bring down four of them and I will pay you well.'],
      progress: ['Still hearing those wings flapping...'],
      complete: ['Clean kills. You have a hunter\'s eye.'],
    },
  },
  roadToCave: {
    id: 'roadToCave', title: 'Road to the Cave', giver: 'elderRowan', requiredLevel: 3, prerequisite: 'slimeTrouble',
    description: 'Travel through the east portal to the Dark Cave.', objective: reach('darkCave'), rewards: { exp: 120, gold: 40 },
    dialog: {
      offer: ['Miner Bork has not returned from the Dark Cave.', 'Take the east portal and see what happened.'],
      progress: ['Follow the dirt road east to the portal.'],
      complete: ['You found the cave. Bork must be nearby.'],
    },
  },
  goblinRaid: {
    id: 'goblinRaid', title: 'Goblin Raid', giver: 'minerBork', requiredLevel: 3, prerequisite: null,
    description: 'Defeat 6 goblins in the Dark Cave.', objective: kill('goblin', 6), rewards: { exp: 200, gold: 60 },
    dialog: {
      offer: ['Goblins stole my pickaxe and my dignity.', 'Teach six of them a lesson, would you?'],
      progress: ['Those green pests are still laughing at me.'],
      complete: ['Ha! That will show them. Here, take this.'],
    },
  },
  boneCollector: {
    id: 'boneCollector', title: 'Bone Collector', giver: 'minerBork', requiredLevel: 5, prerequisite: 'goblinRaid',
    description: 'Destroy 5 skeletons deeper in the cave.', objective: kill('skeleton', 5), rewards: { exp: 300, gold: 80 },
    dialog: {
      offer: ['Something is waking the dead in the deep tunnels.', 'Smash five skeletons before they reach the village.'],
      progress: ['I can still hear bones rattling down there.'],
      complete: ['Silence at last. You are stronger than you look.'],
    },
  },
  fireInTheSand: {
    id: 'fireInTheSand', title: 'Fire in the Sand', giver: 'nomadSaif', requiredLevel: 7, prerequisite: null,
    description: 'Put out 6 fire slimes in the Scorched Desert.', objective: kill('fireSlime', 6), rewards: { exp: 450, gold: 120 },
    dialog: {
      offer: ['Fire slimes burn our caravans every night.', 'Six fewer of them would let my people sleep.'],
      progress: ['The sand still glows at night. Keep hunting.'],
      complete: ['The nights are cooler already. My thanks.'],
    },
  },
  warriorsPride: {
    id: 'warriorsPride', title: "Warrior's Pride", giver: 'nomadSaif', requiredLevel: 9, prerequisite: 'fireInTheSand',
    description: 'Defeat 5 goblin warriors.', objective: kill('goblinWarrior', 5), rewards: { exp: 650, gold: 150 },
    dialog: {
      offer: ['Goblin warriors from the castle raid our wells.', 'Show them the desert has a protector.'],
      progress: ['The warriors still roam the dunes.'],
      complete: ['They will think twice before returning.'],
    },
  },
  knightsHonor: {
    id: 'knightsHonor', title: "Knight's Honor", giver: 'knightAldric', requiredLevel: 12, prerequisite: null,
    description: 'Lay 6 skeleton knights to rest.', objective: kill('skeletonKnight', 6), rewards: { exp: 900, gold: 200 },
    dialog: {
      offer: ['My old comrades wander these halls as bones.', 'Grant six of them peace. Please.'],
      progress: ['They still march. Do not falter.'],
      complete: ['They can rest now. As can I, soon.'],
    },
  },
  theAncientGolem: {
    id: 'theAncientGolem', title: 'The Demon Cyclops', giver: 'knightAldric', requiredLevel: 12, prerequisite: 'knightsHonor',
    description: 'Destroy the Demon Cyclops in the throne room.', objective: kill('golemBoss', 1), rewards: { exp: 2000, gold: 500 },
    dialog: {
      offer: ['The Cyclops that killed us still sits on the throne.', 'End it, and this castle will finally be free.'],
      progress: ['The throne room lies to the north-east.'],
      complete: ['It is done. Thank you, hero. Go in peace.'],
    },
  },
});

export const QUEST_ORDER = Object.freeze(Object.keys(QUEST_DEFINITIONS));
