// Ground poison zone (Assassin Venom Cloud): same timing model as the Healing Circle (HealingZone), but a
// different `kind`/palette so it is drawn green and `onTick` poisons monsters instead of healing the hero.
import { HealingZone } from './healing-zone.js';

export class VenomZone extends HealingZone {
  constructor(opts) {
    super(opts);
    this.kind = 'venomZone';
  }
}
