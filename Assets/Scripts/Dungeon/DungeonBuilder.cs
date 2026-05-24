using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class EnemyPrefabEntry
{
    public EnemyType enemyType;
    public GameObject prefab;
}

public class DungeonBuilder : MonoBehaviour
{
    public static DungeonBuilder Instance { get; private set; }

    [Header("Room Prefabs")]
    [SerializeField] private GameObject[] normalRoomPrefabs;
    [SerializeField] private GameObject bossRoomPrefab;
    [SerializeField] private GameObject shopRoomPrefab;
    [SerializeField] private GameObject entryRoomPrefab;

    [Header("Enemy Prefabs")]
    [SerializeField] private List<EnemyPrefabEntry> enemyPrefabs = new List<EnemyPrefabEntry>();

    [Header("Generation")]
    [SerializeField] private Vector2 roomSpacing = new Vector2(25f, 18f);
    [SerializeField] private int totalRooms = 8;
    [SerializeField] private int seed = 1337;
    [SerializeField] private bool buildOnStart = true;

    [Header("Scene References")]
    [SerializeField] private Transform dungeonRoot;
    [SerializeField] private KaisenController player;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MinimapSystem minimapSystem;

    private readonly Dictionary<Vector2Int, DungeonRoom> roomsByGrid = new Dictionary<Vector2Int, DungeonRoom>();
    private readonly Dictionary<int, DungeonRoom> roomsByIndex = new Dictionary<int, DungeonRoom>();

    private DungeonLayout currentLayout;
    private DungeonRoom entryRoom;
    private DungeonRoom currentRoom;
    private float nextDoorTransitionTime;

    public DungeonLayout CurrentLayout { get { return currentLayout; } }
    public int CurrentSeed => seed;
    public int CurrentFloorNumber => 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dungeonRoot == null)
        {
            GameObject root = new GameObject("GeneratedDungeon");
            root.transform.SetParent(transform);
            dungeonRoot = root.transform;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<KaisenController>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (minimapSystem == null)
        {
            minimapSystem = FindFirstObjectByType<MinimapSystem>();
        }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "CityArken" || SceneManager.GetActiveScene().name == "MainMenu")
        {
            return;
        }

        if (RunManager.Instance != null && RunManager.Instance.ShouldManageDungeonBuild(SceneManager.GetActiveScene().name))
        {
            return;
        }

        if (buildOnStart)
        {
            BuildDungeon(seed);
        }
    }

    public void BuildDungeon(int buildSeed)
    {
        seed = buildSeed;
        System.Random layoutRandom = new System.Random(buildSeed);
        totalRooms = GameBalanceData.Instance.GetRoomsForFloor(layoutRandom);
        currentLayout = DungeonLayout.Generate(buildSeed, totalRooms);
        ClearGeneratedRooms();
        roomsByGrid.Clear();
        roomsByIndex.Clear();
        entryRoom = null;
        currentRoom = null;

        if (player == null)
        {
            player = FindFirstObjectByType<KaisenController>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (minimapSystem == null)
        {
            minimapSystem = FindFirstObjectByType<MinimapSystem>();
        }

        System.Random random = new System.Random(buildSeed);
        for (int i = 0; i < currentLayout.Rooms.Count; i++)
        {
            DungeonLayout.RoomNode node = currentLayout.Rooms[i];
            GameObject roomPrefab = SelectRoomPrefab(node.type, random);
            if (roomPrefab == null)
            {
                continue;
            }

            Vector3 roomPosition = new Vector3(node.gridPos.x * roomSpacing.x, node.gridPos.y * roomSpacing.y, 0f);
            GameObject roomObject = Instantiate(roomPrefab, roomPosition, Quaternion.identity, dungeonRoot);
            roomObject.name = string.Format("{0}_{1}", node.type, node.roomIndex);

            DungeonRoom room = roomObject.GetComponent<DungeonRoom>();
            if (room == null)
            {
                continue;
            }

            room.ConfigureRuntime(this, node);
            room.ActivateDoors(node.connections[0], node.connections[1], node.connections[2], node.connections[3]);
            room.Initialize(BuildEnemyList(room.Data, room.SpawnPointCount, random, node.type));

            roomsByGrid[node.gridPos] = room;
            roomsByIndex[node.roomIndex] = room;
            if (node.type == RoomType.Entry)
            {
                entryRoom = room;
            }
        }

        for (int i = 0; i < currentLayout.Rooms.Count; i++)
        {
            DungeonLayout.RoomNode node = currentLayout.Rooms[i];
            DungeonRoom room;
            if (!roomsByGrid.TryGetValue(node.gridPos, out room))
            {
                continue;
            }

            for (int doorIndex = 0; doorIndex < 4; doorIndex++)
            {
                if (!node.connections[doorIndex])
                {
                    continue;
                }

                Vector2Int neighborPos = node.gridPos + GetDirectionOffset(doorIndex);
                DungeonRoom neighborRoom;
                if (!roomsByGrid.TryGetValue(neighborPos, out neighborRoom))
                {
                    continue;
                }

                room.ConfigureDoorLink(doorIndex, neighborRoom, OppositeDoorIndex(doorIndex));
            }
        }

        if (entryRoom != null && player != null)
        {
            MovePlayerToPosition(entryRoom.GetRoomCenter());
            SetCurrentRoom(entryRoom);
            entryRoom.HandlePlayerEntered();
            SnapCameraToRoom(entryRoom);
        }

        if (minimapSystem != null)
        {
            minimapSystem.Initialize(this, currentLayout);
            minimapSystem.SetCurrentRoom(entryRoom);
        }
    }

    public GameObject ResolveEnemyPrefab(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            return null;
        }

        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            if (enemyPrefabs[i] != null && enemyPrefabs[i].enemyType == enemyData.enemyType)
            {
                return enemyPrefabs[i].prefab;
            }
        }

        return null;
    }

    public DungeonRoom GetCurrentRoom()
    {
        if (player == null)
        {
            return currentRoom;
        }

        Vector2 playerPosition = player.transform.position;
        if (currentRoom != null && currentRoom.ContainsPoint(playerPosition))
        {
            return currentRoom;
        }

        Collider2D[] overlaps = Physics2D.OverlapPointAll(playerPosition);
        for (int i = 0; i < overlaps.Length; i++)
        {
            DungeonRoom room = overlaps[i].GetComponentInParent<DungeonRoom>();
            if (room != null)
            {
                currentRoom = room;
                return currentRoom;
            }
        }

        return currentRoom;
    }

    public DungeonRoom GetRoomByIndex(int roomIndex)
    {
        DungeonRoom room;
        roomsByIndex.TryGetValue(roomIndex, out room);
        return room;
    }

    public int GetVisitedRoomCount()
    {
        int count = 0;
        foreach (DungeonRoom room in roomsByIndex.Values)
        {
            if (room != null && room.IsVisited)
            {
                count++;
            }
        }

        return count;
    }

    public int GetTotalRoomCount()
    {
        return roomsByIndex.Count;
    }

    public bool AreAllCombatRoomsCleared()
    {
        foreach (DungeonRoom room in roomsByIndex.Values)
        {
            if (room == null || room.SpawnPointCount <= 0)
            {
                continue;
            }

            if (!room.IsCleared)
            {
                return false;
            }
        }

        return roomsByIndex.Count > 0;
    }

    public bool TryPromoteNormalRoomToSecret()
    {
        List<DungeonRoom> unvisitedCandidates = new List<DungeonRoom>();
        List<DungeonRoom> fallbackCandidates = new List<DungeonRoom>();
        foreach (DungeonRoom room in roomsByIndex.Values)
        {
            if (room == null || room.RuntimeRoomType != RoomType.Normal)
            {
                continue;
            }

            if (!room.IsVisited)
            {
                unvisitedCandidates.Add(room);
            }
            else if (room != currentRoom)
            {
                fallbackCandidates.Add(room);
            }
        }

        List<DungeonRoom> candidates = unvisitedCandidates.Count > 0 ? unvisitedCandidates : fallbackCandidates;
        if (candidates.Count == 0)
        {
            return false;
        }

        DungeonRoom selectedRoom = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        if (selectedRoom == null || currentLayout == null)
        {
            return false;
        }

        if (!currentLayout.TrySetRoomType(selectedRoom.RoomIndex, RoomType.Secret))
        {
            return false;
        }

        selectedRoom.SetRuntimeRoomType(RoomType.Secret);
        if (minimapSystem != null)
        {
            minimapSystem.SetCurrentRoom(currentRoom);
        }

        return true;
    }

    public void HandleDoorTransition(DoorTrigger trigger, Transform playerTransform)
    {
        if (trigger == null || trigger.DestinationRoom == null)
        {
            return;
        }

        if (!trigger.TraversalEnabled)
        {
            return;
        }

        if (trigger.SourceRoom != null && trigger.SourceRoom.IsCombatLocked)
        {
            return;
        }

        if (Time.time < nextDoorTransitionTime)
        {
            return;
        }

        nextDoorTransitionTime = Time.time + 0.25f;
        DungeonRoom destinationRoom = trigger.DestinationRoom;
        MovePlayerToPosition(destinationRoom.GetDoorArrivalPosition(trigger.DestinationDoorIndex));
        SetCurrentRoom(destinationRoom);
        destinationRoom.HandlePlayerEntered();
        SnapCameraToRoom(destinationRoom);
        if (minimapSystem != null)
        {
            minimapSystem.SetCurrentRoom(destinationRoom);
        }
    }

    private void SetCurrentRoom(DungeonRoom room)
    {
        if (currentRoom == room)
        {
            if (currentRoom != null)
            {
                currentRoom.MarkVisited();
            }
            return;
        }

        currentRoom = room;
        if (currentRoom != null)
        {
            currentRoom.MarkVisited();
            EventBus.Publish(new RoomVisitedEvent(currentRoom));
        }
    }


    private void MovePlayerToPosition(Vector2 destination)
    {
        if (player == null)
        {
            return;
        }

        player.transform.position = destination;
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
        {
            playerBody.position = destination;
#if UNITY_6000_OR_NEWER
            playerBody.linearVelocity = Vector2.zero;
#else
            playerBody.linearVelocity = Vector2.zero;
#endif
        }
    }

    private void SnapCameraToRoom(DungeonRoom room)
    {
        if (room == null || mainCamera == null)
        {
            return;
        }

        if (DungeonCameraController.Instance != null)
        {
            return;
        }

        Vector3 position = mainCamera.transform.position;
        Vector2 roomCenter = room.GetRoomCenter();
        mainCamera.transform.position = new Vector3(roomCenter.x, roomCenter.y, position.z);
    }

    private void ClearGeneratedRooms()
    {
        if (dungeonRoot == null)
        {
            return;
        }

        List<GameObject> children = new List<GameObject>();
        for (int i = 0; i < dungeonRoot.childCount; i++)
        {
            children.Add(dungeonRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < children.Count; i++)
        {
            if (Application.isPlaying)
            {
                Destroy(children[i]);
            }
            else
            {
                DestroyImmediate(children[i]);
            }
        }
    }

    private GameObject SelectRoomPrefab(RoomType roomType, System.Random random)
    {
        if (roomType == RoomType.Entry)
        {
            return entryRoomPrefab;
        }

        if (roomType == RoomType.Boss)
        {
            return bossRoomPrefab != null ? bossRoomPrefab : GetRandomNormalRoom(random);
        }

        if (roomType == RoomType.Shop)
        {
            return shopRoomPrefab != null ? shopRoomPrefab : GetRandomNormalRoom(random);
        }

        return GetRandomNormalRoom(random);
    }

    private GameObject GetRandomNormalRoom(System.Random random)
    {
        if (normalRoomPrefabs == null || normalRoomPrefabs.Length == 0)
        {
            return null;
        }

        int index = random.Next(normalRoomPrefabs.Length);
        return normalRoomPrefabs[index];
    }

    private List<EnemyData> BuildEnemyList(RoomData roomData, int spawnCapacity, System.Random random, RoomType roomType)
    {
        List<EnemyData> result = new List<EnemyData>();
        if (roomData == null || roomData.enemySpawnTable == null || spawnCapacity <= 0)
        {
            return result;
        }

        if (roomType == RoomType.Entry || roomType == RoomType.Shop)
        {
            return result;
        }

        int totalMinimum = 0;
        int totalMaximum = 0;
        int[] spawnedCounts = new int[roomData.enemySpawnTable.Count];
        for (int i = 0; i < roomData.enemySpawnTable.Count; i++)
        {
            EnemySpawnEntry entry = roomData.enemySpawnTable[i];
            if (entry == null || entry.data == null)
            {
                continue;
            }

            int minCount = Mathf.Max(0, entry.minCount);
            int maxCount = Mathf.Max(minCount, entry.maxCount);
            totalMinimum += minCount;
            totalMaximum += maxCount;
        }

        int targetCount = Math.Min(spawnCapacity, RandomRange(random, Math.Max(1, totalMinimum), Math.Max(totalMinimum, totalMaximum) + 1));
        for (int i = 0; i < roomData.enemySpawnTable.Count && result.Count < spawnCapacity; i++)
        {
            EnemySpawnEntry entry = roomData.enemySpawnTable[i];
            if (entry == null || entry.data == null)
            {
                continue;
            }

            int minCount = Mathf.Max(0, entry.minCount);
            for (int j = 0; j < minCount && result.Count < spawnCapacity; j++)
            {
                result.Add(entry.data);
                spawnedCounts[i]++;
            }
        }

        while (result.Count < targetCount)
        {
            float totalWeight = 0f;
            for (int i = 0; i < roomData.enemySpawnTable.Count; i++)
            {
                EnemySpawnEntry entry = roomData.enemySpawnTable[i];
                if (entry == null || entry.data == null)
                {
                    continue;
                }

                int maxCount = Mathf.Max(entry.minCount, entry.maxCount);
                if (spawnedCounts[i] >= maxCount)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0.01f, entry.weight);
            }

            if (totalWeight <= 0f)
            {
                break;
            }

            float pick = (float)(random.NextDouble() * totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < roomData.enemySpawnTable.Count; i++)
            {
                EnemySpawnEntry entry = roomData.enemySpawnTable[i];
                if (entry == null || entry.data == null)
                {
                    continue;
                }

                int maxCount = Mathf.Max(entry.minCount, entry.maxCount);
                if (spawnedCounts[i] >= maxCount)
                {
                    continue;
                }

                cumulative += Mathf.Max(0.01f, entry.weight);
                if (pick > cumulative)
                {
                    continue;
                }

                result.Add(entry.data);
                spawnedCounts[i]++;
                break;
            }

            if (result.Count >= spawnCapacity)
            {
                break;
            }
        }

        return result;
    }

    private static int OppositeDoorIndex(int doorIndex)
    {
        if (doorIndex == 0) return 1;
        if (doorIndex == 1) return 0;
        if (doorIndex == 2) return 3;
        return 2;
    }

    private static Vector2Int GetDirectionOffset(int doorIndex)
    {
        if (doorIndex == 0) return Vector2Int.up;
        if (doorIndex == 1) return Vector2Int.down;
        if (doorIndex == 2) return Vector2Int.right;
        return Vector2Int.left;
    }

    private static int RandomRange(System.Random random, int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        return random.Next(minInclusive, maxExclusive);
    }
}
