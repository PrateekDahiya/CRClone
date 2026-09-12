using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;

namespace CRClone.Battle.Simulation
{
    public class Pathfinding
    {
        private const int GRID_WIDTH = 36;   // 18 tiles * 2 (half-tile precision)
        private const int GRID_HEIGHT = 64;  // 32 tiles * 2
        private const float TILE_SIZE = 0.5f; // Half-tile precision

        private bool[,] _walkableGround;
        private bool[,] _walkableAir;
        private Node[,] _nodes;

        public void Initialize()
        {
            _walkableGround = new bool[GRID_WIDTH, GRID_HEIGHT];
            _walkableAir = new bool[GRID_WIDTH, GRID_HEIGHT];
            _nodes = new Node[GRID_WIDTH, GRID_HEIGHT];

            // Initialize all as walkable
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    _walkableGround[x, y] = true;
                    _walkableAir[x, y] = true;
                    _nodes[x, y] = new Node(x, y);
                }
            }

            // Mark river as unwalkable for ground units
            int riverMinY = Mathf.RoundToInt(14f / TILE_SIZE); // 28
            int riverMaxY = Mathf.RoundToInt(18f / TILE_SIZE); // 36

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = riverMinY; y < riverMaxY; y++)
                {
                    _walkableGround[x, y] = false;
                }
            }

            // Bridges (2 tiles wide at center)
            int bridgeCenterX = GRID_WIDTH / 2; // 18
            for (int y = riverMinY; y < riverMaxY; y++)
            {
                _walkableGround[bridgeCenterX - 1, y] = true; // Left bridge tile
                _walkableGround[bridgeCenterX, y] = true;     // Right bridge tile
            }
        }

        public List<Vector2> FindPath(Vector2 start, Vector2 target, EntityType entityType)
        {
            var startNode = WorldToGrid(start);
            var targetNode = WorldToGrid(target);

            // Validate nodes
            if (!IsValidNode(startNode, entityType) || !IsValidNode(targetNode, entityType))
            {
                // Find nearest valid nodes
                startNode = FindNearestValidNode(startNode, entityType);
                targetNode = FindNearestValidNode(targetNode, entityType);
            }

            if (startNode == targetNode)
            {
                return new List<Vector2> { target };
            }

            return AStar(startNode, targetNode, entityType);
        }

        private List<Vector2> AStar(Node start, Node target, EntityType entityType)
        {
            var openSet = new PriorityQueue<Node>();
            var closedSet = new HashSet<Node>();

            start.GCost = 0;
            start.HCost = Heuristic(start, target);
            start.FCost = start.HCost;
            openSet.Enqueue(start);

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current.X == target.X && current.Y == target.Y)
                {
                    return RetracePath(start, current);
                }

                closedSet.Add(current);

                foreach (var neighbor in GetNeighbors(current, entityType))
                {
                    if (closedSet.Contains(neighbor)) continue;

                    int moveCost = GetMoveCost(current, neighbor);
                    int newGCost = current.GCost + moveCost;

                    if (newGCost < neighbor.GCost || !openSet.Contains(neighbor))
                    {
                        neighbor.GCost = newGCost;
                        neighbor.HCost = Heuristic(neighbor, target);
                        neighbor.FCost = neighbor.GCost + neighbor.HCost;
                        neighbor.Parent = current;

                        if (!openSet.Contains(neighbor))
                            openSet.Enqueue(neighbor);
                        else
                            openSet.UpdatePriority(neighbor);
                    }
                }
            }

            // No path found - return direct path
            return new List<Vector2> { GridToWorld(target) };
        }

        private List<Vector2> RetracePath(Node start, Node end)
        {
            var path = new List<Vector2>();
            var current = end;

            while (current != start && current != null)
            {
                path.Add(GridToWorld(current));
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        private int Heuristic(Node a, Node b)
        {
            // Manhattan distance * 10
            return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y)) * 10;
        }

        private int GetMoveCost(Node from, Node to)
        {
            // Orthogonal = 10, Diagonal = 14
            int dx = Math.Abs(from.X - to.X);
            int dy = Math.Abs(from.Y - to.Y);
            return (dx == 1 && dy == 1) ? 14 : 10;
        }

        private List<Node> GetNeighbors(Node node, EntityType entityType)
        {
            var neighbors = new List<Node>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = node.X + dx;
                    int ny = node.Y + dy;

                    if (IsValidNode(new GridPos(nx, ny), entityType))
                    {
                        neighbors.Add(_nodes[nx, ny]);
                    }
                }
            }

            return neighbors;
        }

        private bool IsValidNode(GridPos pos, EntityType entityType)
        {
            if (pos.x < 0 || pos.x >= GRID_WIDTH || pos.y < 0 || pos.y >= GRID_HEIGHT)
                return false;

            if (entityType == EntityType.Unit)
            {
                // Check if flying (would need to pass unit reference)
                // For now, assume ground
                return _walkableGround[pos.x, pos.y];
            }

            return _walkableGround[pos.x, pos.y];
        }

        private GridPos FindNearestValidNode(GridPos pos, EntityType entityType)
        {
            // BFS to find nearest walkable
            var queue = new Queue<GridPos>();
            var visited = new bool[GRID_WIDTH, GRID_HEIGHT];
            queue.Enqueue(pos);
            visited[pos.x, pos.y] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (IsValidNode(current, entityType))
                    return current;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = current.x + dx;
                        int ny = current.y + dy;

                        if (nx >= 0 && nx < GRID_WIDTH && ny >= 0 && ny < GRID_HEIGHT && !visited[nx, ny])
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue(new GridPos(nx, ny));
                        }
                    }
                }
            }

            return pos; // Fallback
        }

        private GridPos WorldToGrid(Vector2 world)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(world.x / TILE_SIZE), 0, GRID_WIDTH - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(world.y / TILE_SIZE), 0, GRID_HEIGHT - 1);
            return new GridPos(x, y);
        }

        private Vector2 GridToWorld(Node node)
        {
            return new Vector2(node.X * TILE_SIZE, node.Y * TILE_SIZE);
        }

        private Vector2 GridToWorld(GridPos pos)
        {
            return new Vector2(pos.x * TILE_SIZE, pos.y * TILE_SIZE);
        }

        // Internal structures
        private struct GridPos
        {
            public int x, y;
            public GridPos(int x, int y) { this.x = x; this.y = y; }
        }

        private class Node : IEquatable<Node>
        {
            public int X, Y;
            public int GCost, HCost, FCost;
            public Node Parent;

            public Node(int x, int y) { X = x; Y = y; }

            public bool Equals(Node other) => other != null && X == other.X && Y == other.Y;
            public override bool Equals(object obj) => Equals(obj as Node);
            public override int GetHashCode() => X * 1000 + Y;
        }

        // Simple priority queue for A*
        private class PriorityQueue<T> where T : Node
        {
            private List<T> _heap = new List<T>();
            private Dictionary<T, int> _indices = new Dictionary<T, int>();

            public int Count => _heap.Count;

            public void Enqueue(T item)
            {
                _indices[item] = _heap.Count;
                _heap.Add(item);
                HeapifyUp(_heap.Count - 1);
            }

            public T Dequeue()
            {
                var top = _heap[0];
                var last = _heap[_heap.Count - 1];
                _heap[0] = last;
                _indices[last] = 0;
                _heap.RemoveAt(_heap.Count - 1);
                _indices.Remove(top);
                if (_heap.Count > 0) HeapifyDown(0);
                return top;
            }

            public bool Contains(T item) => _indices.ContainsKey(item);

            public void UpdatePriority(T item)
            {
                int index = _indices[item];
                HeapifyUp(index);
                HeapifyDown(index);
            }

            private void HeapifyUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (_heap[index].FCost >= _heap[parent].FCost) break;
                    Swap(index, parent);
                    index = parent;
                }
            }

            private void HeapifyDown(int index)
            {
                while (true)
                {
                    int smallest = index;
                    int left = 2 * index + 1;
                    int right = 2 * index + 2;

                    if (left < _heap.Count && _heap[left].FCost < _heap[smallest].FCost)
                        smallest = left;
                    if (right < _heap.Count && _heap[right].FCost < _heap[smallest].FCost)
                        smallest = right;

                    if (smallest == index) break;
                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int i, int j)
            {
                var temp = _heap[i];
                _heap[i] = _heap[j];
                _heap[j] = temp;
                _indices[_heap[i]] = i;
                _indices[_heap[j]] = j;
            }
        }
    }
}