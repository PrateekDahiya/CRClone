export interface QueueItem<T> {
  item: T;
  priority: number;
  insertedAt: number;
}

export class PriorityQueue<T> {
  private items: QueueItem<T>[] = [];

  enqueue(item: T, priority: number): void {
    const queueItem: QueueItem<T> = {
      item,
      priority,
      insertedAt: Date.now(),
    };

    let inserted = false;
    for (let i = 0; i < this.items.length; i++) {
      if (priority > this.items[i].priority || 
          (priority === this.items[i].priority && queueItem.insertedAt < this.items[i].insertedAt)) {
        this.items.splice(i, 0, queueItem);
        inserted = true;
        break;
      }
    }

    if (!inserted) {
      this.items.push(queueItem);
    }
  }

  dequeue(): T | undefined {
    return this.items.shift()?.item;
  }

  peek(): T | undefined {
    return this.items[0]?.item;
  }

  peekAt(index: number): T | undefined {
    return this.items[index]?.item;
  }

  remove(predicate: (item: T) => boolean): T | undefined {
    const index = this.items.findIndex(q => predicate(q.item));
    if (index !== -1) {
      return this.items.splice(index, 1)[0].item;
    }
    return undefined;
  }

  clear(): void {
    this.items = [];
  }

  get size(): number {
    return this.items.length;
  }

  isEmpty(): boolean {
    return this.items.length === 0;
  }

  toArray(): T[] {
    return this.items.map(q => q.item);
  }

  find(predicate: (item: T) => boolean): T | undefined {
    return this.items.find(q => predicate(q.item))?.item;
  }

  filter(predicate: (item: T) => boolean): T[] {
    return this.items.filter(q => predicate(q.item)).map(q => q.item);
  }
}

export class Queue<T> {
  private items: T[] = [];

  enqueue(item: T): void {
    this.items.push(item);
  }

  dequeue(): T | undefined {
    return this.items.shift();
  }

  peek(): T | undefined {
    return this.items[0];
  }

  remove(predicate: (item: T) => boolean): T | undefined {
    const index = this.items.findIndex(predicate);
    if (index !== -1) {
      return this.items.splice(index, 1)[0];
    }
    return undefined;
  }

  clear(): void {
    this.items = [];
  }

  get size(): number {
    return this.items.length;
  }

  isEmpty(): boolean {
    return this.items.length === 0;
  }

  toArray(): T[] {
    return [...this.items];
  }
}