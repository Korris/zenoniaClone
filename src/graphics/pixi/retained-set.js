// Keeps one retained display node per game entity so the renderer never creates objects per frame:
// get() returns the node for a key (building it on first sight), endFrame() hands every node that was
// not asked for this frame to `release` (hide / return to a pool). Pure, DOM-free.

export class RetainedSet {
  /**
   * @param {(key:object)=>object} create builds a node for a new entity
   * @param {(node:object, key:object)=>void} release called once when an entity stops being drawn
   */
  constructor(create, release) {
    this.create = create;
    this.release = release;
    this.nodes = new Map();
    this.seen = new Set();
  }

  get(key) {
    let node = this.nodes.get(key);
    if (!node) { node = this.create(key); this.nodes.set(key, node); }
    this.seen.add(key);
    return node;
  }

  /** Releases everything not touched since the last endFrame(); returns how many nodes were released. */
  endFrame() {
    let released = 0;
    if (this.seen.size !== this.nodes.size) {
      for (const [key, node] of this.nodes) {
        if (this.seen.has(key)) continue;
        this.nodes.delete(key);
        this.release(node, key);
        released++;
      }
    }
    this.seen.clear();
    return released;
  }

  clear() {
    for (const [key, node] of this.nodes) this.release(node, key);
    this.nodes.clear();
    this.seen.clear();
  }

  get size() {
    return this.nodes.size;
  }
}

/** Free-list of reusable nodes: take() recycles a released one before building a new one. */
export class NodePool {
  constructor(make, reset) {
    this.make = make;
    this.reset = reset;
    this.free = [];
    this.created = 0;
  }

  take() {
    const node = this.free.pop();
    if (node) return node;
    this.created++;
    return this.make();
  }

  put(node) {
    this.reset?.(node);
    this.free.push(node);
  }
}
