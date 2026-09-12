import { EntityState } from '../types';

export interface EntitySnapshot {
  id: number;
  state: EntityState;
  tick: number;
}

export class EntityManager {
  private _previousStates: Map<number, EntitySnapshot> = new Map();
  private _currentTick: number = 0;

  public computeDelta(currentEntities: EntityState[]): EntityState[] {
    this._currentTick++;
    const delta: EntityState[] = [];

    const currentMap = new Map<number, EntityState>();
    for (const entity of currentEntities) {
      currentMap.set(entity.id, entity);
    }

    // Check for new or changed entities
    for (const entity of currentEntities) {
      const previous = this._previousStates.get(entity.id);
      
      if (!previous) {
        // New entity
        delta.push(this.cloneEntityState(entity));
      } else if (this.hasChanged(previous.state, entity)) {
        // Changed entity
        delta.push(this.cloneEntityState(entity));
      }
      
      // Update previous state
      this._previousStates.set(entity.id, { id: entity.id, state: this.cloneEntityState(entity), tick: this._currentTick });
    }

    // Check for deleted entities
    for (const [id, previous] of this._previousStates) {
      if (!currentMap.has(id)) {
        // Entity was removed - send death marker
        const deadEntity: EntityState = {
          ...previous.state,
          isDead: true,
          hp: 0,
        };
        delta.push(deadEntity);
        this._previousStates.delete(id);
      }
    }

    return delta;
  }

  private hasChanged(prev: EntityState, curr: EntityState): boolean {
    // Position threshold - only send if moved significantly
    const posThreshold = 0.01;
    if (Math.abs(prev.position.x - curr.position.x) > posThreshold ||
        Math.abs(prev.position.y - curr.position.y) > posThreshold) {
      return true;
    }

    // Velocity threshold
    const velThreshold = 0.01;
    if (Math.abs(prev.velocity.x - curr.velocity.x) > velThreshold ||
        Math.abs(prev.velocity.y - curr.velocity.y) > velThreshold) {
      return true;
    }

    // HP changed
    if (prev.hp !== curr.hp) return true;
    if (prev.maxHp !== curr.maxHp) return true;

    // Target changed
    if (prev.targetId !== curr.targetId) return true;

    // Death state changed
    if (prev.isDead !== curr.isDead) return true;

    // Unit-specific
    if (prev.state !== curr.state) return true;
    if (prev.attackCooldown !== curr.attackCooldown) return true;

    // Building-specific
    if (prev.lifetime !== curr.lifetime) return true;
    if (prev.isRetracted !== curr.isRetracted) return true;
    if (prev.spawnTimer !== curr.spawnTimer) return true;

    // Projectile-specific
    if (prev.sourceId !== curr.sourceId) return true;
    if (prev.isBeam !== curr.isBeam) return true;

    // Spell-specific
    if (prev.spellType !== curr.spellType) return true;
    if (prev.radius !== curr.radius) return true;
    if (prev.remainingTime !== curr.remainingTime) return true;

    return false;
  }

  private cloneEntityState(entity: EntityState): EntityState {
    return {
      id: entity.id,
      type: entity.type,
      owner: entity.owner,
      position: { ...entity.position },
      velocity: { ...entity.velocity },
      hp: entity.hp,
      maxHp: entity.maxHp,
      targetId: entity.targetId,
      isDead: entity.isDead,
      state: entity.state,
      attackCooldown: entity.attackCooldown,
      lifetime: entity.lifetime,
      isRetracted: entity.isRetracted,
      spawnTimer: entity.spawnTimer,
      sourceId: entity.sourceId,
      isBeam: entity.isBeam,
      spellType: entity.spellType,
      radius: entity.radius,
      remainingTime: entity.remainingTime,
    };
  }

  public applyFullState(entities: EntityState[]): void {
    this._previousStates.clear();
    for (const entity of entities) {
      this._previousStates.set(entity.id, { 
        id: entity.id, 
        state: this.cloneEntityState(entity), 
        tick: this._currentTick 
      });
    }
  }

  public getPreviousState(entityId: number): EntityState | undefined {
    return this._previousStates.get(entityId)?.state;
  }

  public getAllPreviousStates(): Map<number, EntitySnapshot> {
    return new Map(this._previousStates);
  }

  public clear(): void {
    this._previousStates.clear();
    this._currentTick = 0;
  }
}

export class EntityInterpolator {
  private _entityHistory: Map<number, EntityState[]> = new Map();
  private readonly _maxHistory = 3; // Keep last 3 states for interpolation

  public addState(entity: EntityState): void {
    const history = this._entityHistory.get(entity.id) || [];
    history.push({ ...entity });
    if (history.length > this._maxHistory) {
      history.shift();
    }
    this._entityHistory.set(entity.id, history);
  }

  public getInterpolatedState(entityId: number, alpha: number): EntityState | null {
    const history = this._entityHistory.get(entityId);
    if (!history || history.length < 2) return null;

    const current = history[history.length - 1];
    const previous = history[history.length - 2];

    return {
      ...current,
      position: this.lerp(previous.position, current.position, alpha),
      velocity: this.lerp(previous.velocity, current.velocity, alpha),
    };
  }

  private lerp(a: { x: number; y: number }, b: { x: number; y: number }, t: number): { x: number; y: number } {
    return {
      x: a.x + (b.x - a.x) * t,
      y: a.y + (b.y - a.y) * t,
    };
  }

  public clear(): void {
    this._entityHistory.clear();
  }
}