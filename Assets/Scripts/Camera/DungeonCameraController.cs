using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[DefaultExecutionOrder(-25)]
public class DungeonCameraController : MonoBehaviour
{
    public static DungeonCameraController Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private KaisenController player;
    [SerializeField] private DungeonBuilder dungeonBuilder;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineVirtualCamera gameplayCamera;
    [SerializeField] private CinemachineVirtualCamera bossCamera;
    [SerializeField] private CinemachineConfiner2D gameplayConfiner;
    [SerializeField] private CinemachineConfiner2D bossConfiner;
    [SerializeField] private Vector3 fallbackOffset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float fallbackSmoothTime = 0.08f;
    [SerializeField] private float fallbackSnapDistance = 4f;
    [SerializeField] private float aimLookAhead = 0f;
    [SerializeField] private float verticalFramingOffset = 0f;
    [SerializeField] private float cameraTargetSmoothTime = 0f;

    private readonly Dictionary<int, PolygonCollider2D> confinersByRoom = new Dictionary<int, PolygonCollider2D>();
    private DungeonRoom observedRoom;
    private BossBase observedBoss;
    private Vector3 fallbackVelocity;
    private Vector3 cameraTargetVelocity;
    private Transform cameraFollowTarget;
    private Transform lastResolvedTarget;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu" || sceneName == "CityArken")
        {
            return;
        }

        if (FindFirstObjectByType<DungeonCameraController>() != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("DungeonCameraController");
        cameraObject.AddComponent<DungeonCameraController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<RoomVisitedEvent>(OnRoomVisited);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<RoomVisitedEvent>(OnRoomVisited);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void Update()
    {
        ResolveReferences();
        EnsureCameraRig();
        if (dungeonBuilder == null)
        {
            return;
        }

        DungeonRoom currentRoom = dungeonBuilder.GetCurrentRoom();
        if (currentRoom != null && currentRoom != observedRoom)
        {
            ApplyRoom(currentRoom);
        }

        if (observedRoom != null && observedRoom.RuntimeRoomType == RoomType.Boss && (observedBoss == null || observedBoss.IsDead))
        {
            SetBossZoom(false);
        }
    }

    private void LateUpdate()
    {
        UpdateCameraFollowTarget();
        ApplyFallbackFollow();
    }

    private void OnRoomVisited(RoomVisitedEvent roomEvent)
    {
        if (roomEvent == null || roomEvent.room == null)
        {
            return;
        }

        ResolveReferences();
        EnsureCameraRig();
        ApplyRoom(roomEvent.room);
    }

    private void OnEnemyDied(EnemyDiedEvent enemyEvent)
    {
        if (enemyEvent == null || enemyEvent.enemy == null)
        {
            return;
        }

        if (observedBoss != null && enemyEvent.enemy == observedBoss.gameObject)
        {
            SetBossZoom(false);
        }
    }

    private void ApplyRoom(DungeonRoom room)
    {
        if (room == null)
        {
            return;
        }

        observedRoom = room;
        PolygonCollider2D confinerShape = EnsureRoomConfiner(room);
        if (gameplayConfiner != null)
        {
            gameplayConfiner.BoundingShape2D = confinerShape;
            gameplayConfiner.InvalidateBoundingShapeCache();
            gameplayConfiner.InvalidateLensCache();
            gameplayConfiner.enabled = false;
        }

        if (bossConfiner != null)
        {
            bossConfiner.BoundingShape2D = confinerShape;
            bossConfiner.InvalidateBoundingShapeCache();
            bossConfiner.InvalidateLensCache();
            bossConfiner.enabled = false;
        }

        observedBoss = room.GetComponentInChildren<BossBase>();
        bool shouldUseBossZoom = room.RuntimeRoomType == RoomType.Boss && observedBoss != null && !observedBoss.IsDead;
        SetBossZoom(shouldUseBossZoom);
    }

    private void SetBossZoom(bool enabled)
    {
        if (gameplayCamera == null || bossCamera == null)
        {
            return;
        }

        gameplayCamera.Priority = 10;
        bossCamera.Priority = enabled ? 20 : 0;
    }

    private void EnsureCameraRig()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || player == null)
        {
            return;
        }

        mainCamera.orthographic = true;
        brain = brain != null ? brain : mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
        }

        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1f);
        brain.IgnoreTimeScale = true;

        Transform followTarget = EnsureCameraFollowTarget();
        gameplayCamera = EnsureVirtualCamera("GameplayCamera", ref gameplayConfiner, 6.25f, 10, followTarget);
        bossCamera = EnsureVirtualCamera("BossCamera", ref bossConfiner, 8.75f, 0, followTarget);
    }

    private CinemachineVirtualCamera EnsureVirtualCamera(string objectName, ref CinemachineConfiner2D confiner, float orthographicSize, int priority, Transform followTarget)
    {
        Transform target = transform.Find(objectName);
        if (target == null)
        {
            GameObject cameraObject = new GameObject(objectName);
            target = cameraObject.transform;
            target.SetParent(transform, false);
        }

        CinemachineVirtualCamera virtualCamera = target.GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            virtualCamera = target.gameObject.AddComponent<CinemachineVirtualCamera>();
        }

        virtualCamera.m_Follow = followTarget != null ? followTarget : player != null ? player.transform : null;
        virtualCamera.m_Lens.OrthographicSize = orthographicSize;
        virtualCamera.m_Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        virtualCamera.Priority = priority;

        CinemachineFramingTransposer framing = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framing == null)
        {
            framing = virtualCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
        }
        framing.m_CameraDistance = 10f;
        framing.m_ScreenX = 0.5f;
        framing.m_ScreenY = 0.5f;
        framing.m_DeadZoneWidth = 0f;
        framing.m_DeadZoneHeight = 0f;
        framing.m_SoftZoneWidth = 1f;
        framing.m_SoftZoneHeight = 1f;
        framing.m_XDamping = 0f;
        framing.m_YDamping = 0f;
        framing.m_ZDamping = 0f;
        framing.m_LookaheadTime = 0f;

        confiner = target.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
        {
            confiner = target.gameObject.AddComponent<CinemachineConfiner2D>();
        }
        confiner.Damping = 0f;
        confiner.enabled = false;

        CinemachineImpulseListener impulseListener = target.GetComponent<CinemachineImpulseListener>();
        if (impulseListener == null)
        {
            impulseListener = target.gameObject.AddComponent<CinemachineImpulseListener>();
        }
        impulseListener.ApplyAfter = CinemachineCore.Stage.Noise;
        impulseListener.ChannelMask = 1;
        impulseListener.Gain = 1f;
        impulseListener.Use2DDistance = true;
        impulseListener.UseCameraSpace = true;

        return virtualCamera;
    }

    private PolygonCollider2D EnsureRoomConfiner(DungeonRoom room)
    {
        PolygonCollider2D cached;
        if (room == null)
        {
            return null;
        }

        if (confinersByRoom.TryGetValue(room.RoomIndex, out cached) && cached != null)
        {
            return cached;
        }

        BoxCollider2D roomBounds = room.GetComponent<BoxCollider2D>();
        if (roomBounds == null)
        {
            return null;
        }

        Transform confinerTransform = room.transform.Find("CameraConfiner");
        if (confinerTransform == null)
        {
            GameObject confinerObject = new GameObject("CameraConfiner");
            confinerTransform = confinerObject.transform;
            confinerTransform.SetParent(room.transform, false);
        }

        PolygonCollider2D polygon = confinerTransform.GetComponent<PolygonCollider2D>();
        if (polygon == null)
        {
            polygon = confinerTransform.gameObject.AddComponent<PolygonCollider2D>();
        }

        Vector2 halfSize = roomBounds.size * 0.5f;
        Vector2 offset = roomBounds.offset;
        polygon.points = new[]
        {
            new Vector2(offset.x - halfSize.x, offset.y - halfSize.y),
            new Vector2(offset.x - halfSize.x, offset.y + halfSize.y),
            new Vector2(offset.x + halfSize.x, offset.y + halfSize.y),
            new Vector2(offset.x + halfSize.x, offset.y - halfSize.y)
        };
        polygon.isTrigger = true;

        confinersByRoom[room.RoomIndex] = polygon;
        return polygon;
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<KaisenController>();
        }

        if (dungeonBuilder == null)
        {
            dungeonBuilder = FindFirstObjectByType<DungeonBuilder>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (player != null && player.transform != lastResolvedTarget)
        {
            lastResolvedTarget = player.transform;
            SnapToPlayer();
        }
    }

    private void ApplyFallbackFollow()
    {
        if (mainCamera == null || player == null)
        {
            return;
        }

        Transform followTarget = cameraFollowTarget != null ? cameraFollowTarget : player.transform;
        bool gameplayCameraFollowsTarget = gameplayCamera != null
            && gameplayCamera.enabled
            && gameplayCamera.gameObject.activeInHierarchy
            && gameplayCamera.Follow == followTarget;
        bool bossCameraFollowsTarget = bossCamera != null
            && bossCamera.enabled
            && bossCamera.gameObject.activeInHierarchy
            && bossCamera.Follow == followTarget;
        bool hasCinemachineFollow = brain != null
            && brain.enabled
            && (gameplayCameraFollowsTarget || bossCameraFollowsTarget);

        Vector3 targetPosition = followTarget.position + fallbackOffset;
        targetPosition.z = fallbackOffset.z;
        float distanceToTarget = Vector2.Distance(mainCamera.transform.position, targetPosition);

        if (hasCinemachineFollow && distanceToTarget <= fallbackSnapDistance)
        {
            return;
        }

        if (distanceToTarget > fallbackSnapDistance)
        {
            mainCamera.transform.position = targetPosition;
            return;
        }

        mainCamera.transform.position = Vector3.SmoothDamp(
            mainCamera.transform.position,
            targetPosition,
            ref fallbackVelocity,
            fallbackSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
    }

    private void SnapToPlayer()
    {
        if (mainCamera == null || player == null)
        {
            return;
        }

        Vector3 desiredCenter = GetDesiredCameraTargetPosition();
        if (cameraFollowTarget != null)
        {
            cameraFollowTarget.position = desiredCenter;
            cameraTargetVelocity = Vector3.zero;
        }

        Vector3 targetPosition = desiredCenter + fallbackOffset;
        targetPosition.z = fallbackOffset.z;
        mainCamera.transform.position = targetPosition;
    }

    private Transform EnsureCameraFollowTarget()
    {
        if (cameraFollowTarget != null)
        {
            return cameraFollowTarget;
        }

        Transform existing = transform.Find("CameraFollowTarget");
        if (existing != null)
        {
            cameraFollowTarget = existing;
        }
        else
        {
            GameObject targetObject = new GameObject("CameraFollowTarget");
            cameraFollowTarget = targetObject.transform;
            cameraFollowTarget.SetParent(transform, false);
        }

        cameraFollowTarget.position = GetDesiredCameraTargetPosition();
        return cameraFollowTarget;
    }

    private void UpdateCameraFollowTarget()
    {
        if (player == null)
        {
            return;
        }

        Transform followTarget = EnsureCameraFollowTarget();
        Vector3 desiredPosition = GetDesiredCameraTargetPosition();
        if (cameraTargetSmoothTime <= 0f)
        {
            followTarget.position = desiredPosition;
            cameraTargetVelocity = Vector3.zero;
            return;
        }

        float smoothTime = Mathf.Max(0.001f, cameraTargetSmoothTime);
        followTarget.position = Vector3.SmoothDamp(
            followTarget.position,
            desiredPosition,
            ref cameraTargetVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
    }

    private Vector3 GetDesiredCameraTargetPosition()
    {
        if (player == null)
        {
            return Vector3.zero;
        }

        Vector2 aim = player.AimDirection.sqrMagnitude > 0.001f ? player.AimDirection.normalized : Vector2.zero;
        Vector3 targetPosition = player.transform.position + (Vector3)(aim * aimLookAhead) + (Vector3.up * verticalFramingOffset);
        targetPosition.z = 0f;
        return targetPosition;
    }
}
