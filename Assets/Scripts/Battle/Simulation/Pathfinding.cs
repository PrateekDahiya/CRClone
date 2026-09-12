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
        private int _gridVersion = 0;

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

            // Mark river as unwalkable for ground units (y=14 to y=18 in tiles = 28 to 36 in half-tiles)
            int riverMinY = Mathf.RoundToInt(14f / TILE_SIZE); // 28
            int riverMaxY = Mathf.RoundToInt(18f / TILE_SIZE); // 36

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = riverMinY; y < riverMaxY; y++)
                {
                    _walkableGround[x, y] = false;
                }
            }

            // Bridges (2 tiles wide at center x=9, so half-tiles 17-18)
            int bridgeCenterX = GRID_WIDTH / 2; // 18
            for (int y = riverMinY; y < riverMaxY; y++)
            {
                _walkableGround[bridgeCenterX - 1, y] = true; // Left bridge tile
                _walkableGround[bridgeCenterX, y] = true;     // Right bridge tile
            }

            _gridVersion++;
        }

        /// <summary>
        /// Updates the collision grid with current building positions.
        /// Call this when buildings are placed or destroyed.
        /// </summary>
        public void UpdateBuildingCollision(List<Building> buildings)
        {
            // Reset to base state (river only)
            int riverMinY = Mathf.RoundToInt(14f / TILE_SIZE);
            int riverMaxY = Mathf.RoundToInt(18f / TILE_SIZE);

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    _walkableGround[x, y] = !(y >= riverMinY && y < riverMaxY);
                }
            }

            // Bridges
            int bridgeCenterX = GRID_WIDTH / 2;
            for (int y = riverMinY; y < riverMaxY; y++)
            {
                _walkableGround[bridgeCenterX - 1, y] = true;
                _walkableGround[bridgeCenterX, y] = true;
            }

            // Mark building footprints as unwalkable
            foreach (var building in buildings)
            {
                if (building.IsDead) continue;
                MarkBuildingFootprint(building);
            }

            _gridVersion++;
        }

        private void MarkBuildingFootprint(Building building)
        {
            // Building sizes in tiles: 2x2 (default), 3x3 (Elixir Collector), 4x4 (X-Bow, Mortar)
            int sizeInTiles = building.CardData.cardName switch
            {
                "X-Bow" => 4,
                "Mortar" => 4,
                "Elixir Collector" => 3,
                _ => 2
            };

            int halfSize = sizeInTiles; // In half-tiles
            var center = WorldToGrid(building.Position);

            int minX = Math.Max(0, center.x - halfSize / 2);
            int maxX = Math.Min(GRID_WIDTH - 1, center.x + halfSize / 2);
            int minY = Math.Max(0, center.y - halfSize / 2);
            int maxY = Math.Min(GRID_HEIGHT - 1, center.y + halfSize / 2);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    _walkableGround[x, y] = false;
                }
            }
        }

        public List<Vector2> FindPath(Vector2 start, Vector2 target, bool isFlying)
        {
            var startNode = WorldToGrid(start);
            var targetNode = WorldToGrid(target);

            // Validate nodes
            if (!IsValidNode(startNode, isFlying) || !IsValidNode(targetNode, isFlying))
            {
                // Find nearest valid nodes
                startNode = FindNearestValidNode(startNode, isFlying);
                targetNode = FindNearestValidNode(targetNode, isFlying);
            }

            if (startNode.x == targetNode.x && startNode.y == targetNode.y)
            {
                return new List<Vector2> { target };
            }

            return AStar(startNode, targetNode, isFlying);
        }

        public int GetPathDistance(Vector2 start, Vector2 target, bool isFlying)
        {
            var path = FindPath(start, target, isFlying);
            if (path.Count <= 1) return 0;

            int distance = 0;
            for (int i = 1; i < path.Count; i++)
            {
                distance += Mathf.RoundToInt(Vector2.Distance(path[i - 1], path[i]) / TILE_SIZE) * 10;
            }
            return distance;
        }

        private List<Vector2> AStar(GridPos start, GridPos target, bool isFlying)
        {
            var openSet = new PriorityQueue<Node>();
            var closedSet = new HashSet<Node>();

            // Reset node costs for this search
            int searchVersion = _gridVersion;
            
            var startNode = _nodes[start.x, start.y];
            var targetNode = _nodes[target.x, target.y];

            startNode.GCost = 0;
            startNode.HCost = Heuristic(startNode, targetNode);
            startNode.FCost = startNode.HCost;
            startNode.SearchVersion = searchVersion;
            openSet.Enqueue(startNode);

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current.X == targetNode.X && current.Y == targetNode.Y)
                {
                    return RetracePath(startNode, current);
                }

                closedSet.Add(current);

                foreach (var neighbor in GetNeighbors(current, isFlying))
                {
                    if (closedSet.Contains(neighbor)) continue;

                    int moveCost = GetMoveCost(current, neighbor);
                    int newGCost = current.GCost + moveCost;

                    if (neighbor.SearchVersion != searchVersion || newGCost < neighbor.GCost || !openSet.Contains(neighbor))
                    {
                        neighbor.GCost = newGCost;
                        neighbor.HCost = Heuristic(neighbor, targetNode);
                        neighbor.FCost = neighbor.GCost + neighbor.HCost;
                        neighbor.Parent = current;
                        neighbor.SearchVersion = searchVersion;

                        if (!openSet.Contains(neighbor))
                            openSet.Enqueue(neighbor);
                        else
                            openSet.UpdatePriority(neighbor);
                    }
                }
            }

            // No path found - return direct path
            return new List<Vector2> { GridToWorld(targetNode) };
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

        private List<Node> GetNeighbors(Node node, bool isFlying)
        {
            var neighbors = new List<Node>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = node.X + dx;
                    int ny = node.Y + dy;

                    if (IsValidNode(new GridPos(nx, ny), isFlying))
                    {
                        neighbors.Add(_nodes[nx, ny]);
                    }
                }
            }

            return neighbors;
        }

        private bool IsValidNode(GridPos pos, bool isFlying)
        {
            if (pos.x < 0 || pos.x >= GRID_WIDTH || pos.y < 0 || pos.y >= GRID_HEIGHT)
                return false;

            return isFlying ? _walkableAir[pos.x, pos.y] : _walkableGround[pos.x, pos.y];
        }

        private GridPos FindNearestValidNode(GridPos pos, bool isFlying)
        {
            // BFS to find nearest walkable
            var queue = new Queue<GridPos>();
            var visited = new bool[GRID_WIDTH, GRID_HEIGHT];
            queue.Enqueue(pos);
            visited[pos.x, pos.y] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (IsValidNode(current, isFlying))
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
            
            public static bool operator ==(GridPos a, GridPos b) => a.x == b.x && a.y == b.y;
            public static bool operator !=(GridPos a, GridPos b) => a.x != b.x || a.y != b.y;
            public override bool Equals(object obj) => obj is GridPos other && this == other;
            public override int GetHashCode() => x * 1000 + y;
        }

        private class Node : IEquatable<Node>
        {
            public int X, Y;
            public int GCost, HCost, FCost;
            public Node Parent;
            public int SearchVersion;

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