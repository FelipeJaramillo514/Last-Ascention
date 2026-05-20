using System.Collections.Generic;
using UnityEngine;

public class DungeonLayout
{
    public const int GridSize = 5;
    private static readonly Vector2Int[] DirectionOffsets =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left
    };

    public struct RoomNode
    {
        public Vector2Int gridPos;
        public RoomType type;
        public bool[] connections;
        public int roomIndex;
    }

    private readonly List<RoomNode> rooms;

    public int Seed { get; private set; }
    public IReadOnlyList<RoomNode> Rooms { get { return rooms; } }

    private DungeonLayout(int seed, List<RoomNode> rooms)
    {
        Seed = seed;
        this.rooms = rooms;
    }

    public static DungeonLayout Generate(int seed, int totalRooms)
    {
        int clampedRoomCount = Mathf.Clamp(totalRooms, 2, GridSize * GridSize);
        System.Random random = new System.Random(seed);
        Vector2Int entryPosition = new Vector2Int(2, 2);

        List<Vector2Int> occupiedOrder = new List<Vector2Int>();
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        occupied.Add(entryPosition);
        occupiedOrder.Add(entryPosition);

        Vector2Int current = entryPosition;
        while (occupiedOrder.Count < clampedRoomCount)
        {
            List<Vector2Int> validNeighbors = GetEmptyNeighbors(current, occupied);
            if (validNeighbors.Count == 0)
            {
                List<Vector2Int> expandableRooms = new List<Vector2Int>();
                for (int i = 0; i < occupiedOrder.Count; i++)
                {
                    if (GetEmptyNeighbors(occupiedOrder[i], occupied).Count > 0)
                    {
                        expandableRooms.Add(occupiedOrder[i]);
                    }
                }

                if (expandableRooms.Count == 0)
                {
                    break;
                }

                current = expandableRooms[random.Next(expandableRooms.Count)];
                validNeighbors = GetEmptyNeighbors(current, occupied);
            }

            Vector2Int next = validNeighbors[random.Next(validNeighbors.Count)];
            occupied.Add(next);
            occupiedOrder.Add(next);
            current = next;
        }

        List<RoomNode> nodes = new List<RoomNode>();
        for (int i = 0; i < occupiedOrder.Count; i++)
        {
            RoomNode node = new RoomNode();
            node.gridPos = occupiedOrder[i];
            node.type = occupiedOrder[i] == entryPosition ? RoomType.Entry : RoomType.Normal;
            node.connections = new bool[4];
            node.roomIndex = i;
            nodes.Add(node);
        }

        int bossIndex = FindFarthestRoomIndex(nodes, entryPosition);
        if (bossIndex >= 0)
        {
            RoomNode bossNode = nodes[bossIndex];
            bossNode.type = RoomType.Boss;
            nodes[bossIndex] = bossNode;
        }

        List<int> normalCandidates = new List<int>();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].type == RoomType.Normal)
            {
                normalCandidates.Add(i);
            }
        }

        if (normalCandidates.Count > 0 && random.NextDouble() <= 0.15d)
        {
            int shopPick = normalCandidates[random.Next(normalCandidates.Count)];
            RoomNode shopNode = nodes[shopPick];
            shopNode.type = RoomType.Shop;
            nodes[shopPick] = shopNode;
            normalCandidates.Remove(shopPick);
        }

        if (normalCandidates.Count > 0 && random.NextDouble() <= 0.10d)
        {
            int secretPick = normalCandidates[random.Next(normalCandidates.Count)];
            RoomNode secretNode = nodes[secretPick];
            secretNode.type = RoomType.Secret;
            nodes[secretPick] = secretNode;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            RoomNode node = nodes[i];
            for (int d = 0; d < DirectionOffsets.Length; d++)
            {
                node.connections[d] = occupied.Contains(node.gridPos + DirectionOffsets[d]);
            }
            node.roomIndex = i;
            nodes[i] = node;
        }

        return new DungeonLayout(seed, nodes);
    }

    public bool TrySetRoomType(int roomIndex, RoomType roomType)
    {
        if (roomIndex < 0 || roomIndex >= rooms.Count)
        {
            return false;
        }

        RoomNode node = rooms[roomIndex];
        node.type = roomType;
        rooms[roomIndex] = node;
        return true;
    }

    private static int FindFarthestRoomIndex(List<RoomNode> nodes, Vector2Int entryPosition)
    {
        int farthestIndex = -1;
        int bestDistance = int.MinValue;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].type != RoomType.Normal)
            {
                continue;
            }

            int distance = Mathf.Abs(nodes[i].gridPos.x - entryPosition.x) + Mathf.Abs(nodes[i].gridPos.y - entryPosition.y);
            if (distance > bestDistance)
            {
                bestDistance = distance;
                farthestIndex = i;
            }
        }

        return farthestIndex;
    }

    private static List<Vector2Int> GetEmptyNeighbors(Vector2Int origin, HashSet<Vector2Int> occupied)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int i = 0; i < DirectionOffsets.Length; i++)
        {
            Vector2Int candidate = origin + DirectionOffsets[i];
            if (candidate.x < 0 || candidate.x >= GridSize || candidate.y < 0 || candidate.y >= GridSize)
            {
                continue;
            }

            if (!occupied.Contains(candidate))
            {
                result.Add(candidate);
            }
        }

        return result;
    }
}

