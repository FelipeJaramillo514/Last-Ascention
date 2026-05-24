using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CityHubBootstrapper : MonoBehaviour
{
    private static CityHubBootstrapper instance;

    [SerializeField] private Transform runtimeRoot;
    [SerializeField] private HubBuildingInteractable nearbyBuilding;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private Text promptText;
    [SerializeField] private HospitalUI hospitalUI;
    [SerializeField] private AssociationUI associationUI;
    [SerializeField] private MetaUpgradeShop marketUI;

    public static CityHubBootstrapper Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != "CityArken")
        {
            return;
        }

        if (FindFirstObjectByType<CityHubBootstrapper>() != null)
        {
            return;
        }

        new GameObject("CityHubBootstrapper").AddComponent<CityHubBootstrapper>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "CityArken")
        {
            enabled = false;
            return;
        }

        BuildCity();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "CityArken")
        {
            return;
        }

        if (promptText == null)
        {
            return;
        }

        if (AnyPanelVisible() || UIModalGate.IsBlockingInteraction)
        {
            promptText.gameObject.SetActive(false);
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseAllPanels();
            }
            return;
        }

        bool canInteract = nearbyBuilding != null;
        promptText.gameObject.SetActive(canInteract);
        if (canInteract)
        {
            promptText.text = "[E] Entrar";
        }

        if (canInteract && !UIModalGate.IsBlockingInteraction && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenBuilding(nearbyBuilding.BuildingType);
        }
    }

    public void SetNearbyBuilding(HubBuildingInteractable building)
    {
        nearbyBuilding = building;
    }

    public void ClearNearbyBuilding(HubBuildingInteractable building)
    {
        if (nearbyBuilding == building)
        {
            nearbyBuilding = null;
        }
    }

    private void BuildCity()
    {
        runtimeRoot = runtimeRoot != null ? runtimeRoot : transform.Find("CityRuntime");
        if (runtimeRoot == null)
        {
            GameObject rootObject = new GameObject("CityRuntime");
            runtimeRoot = rootObject.transform;
            runtimeRoot.SetParent(transform, false);
        }

        for (int i = runtimeRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(runtimeRoot.GetChild(i).gameObject);
        }

        KaisenController player = FindFirstObjectByType<KaisenController>();
        if (player != null)
        {
            player.transform.position = new Vector3(0f, -4.5f, 0f);
            player.SetCombatInputEnabled(false);
        }

        DungeonBuilder dungeonBuilder = FindFirstObjectByType<DungeonBuilder>();
        if (dungeonBuilder != null)
        {
            dungeonBuilder.enabled = false;
        }

        DungeonCameraController dungeonCamera = FindFirstObjectByType<DungeonCameraController>();
        if (dungeonCamera != null)
        {
            dungeonCamera.enabled = false;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            HubCameraFollow follow = mainCamera.GetComponent<HubCameraFollow>();
            if (follow == null)
            {
                follow = mainCamera.gameObject.AddComponent<HubCameraFollow>();
            }
            follow.SetTarget(player != null ? player.transform : null);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5f;
        }

        BuildGround();
        BuildParallax(mainCamera != null ? mainCamera.transform : null);
        BuildBuildings();
        EnsurePromptUi();
        EnsurePanels();
    }

    private void BuildGround()
    {
        GameObject ground = new GameObject("CityGround");
        ground.transform.SetParent(runtimeRoot, false);
        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Floor";
        renderer.sprite = CreateBlockSprite(320, 240, new Color32(25, 30, 38, 255), new Color32(34, 40, 52, 255));
    }

    private void BuildParallax(Transform cameraTransform)
    {
        GameObject skyline = new GameObject("FarSkyline");
        skyline.transform.SetParent(runtimeRoot, false);
        skyline.transform.localPosition = new Vector3(0f, 2.6f, 8f);
        SpriteRenderer skylineRenderer = skyline.AddComponent<SpriteRenderer>();
        skylineRenderer.sortingLayerName = "Background";
        skylineRenderer.sprite = CreateBlockSprite(320, 96, new Color32(18, 18, 24, 255), new Color32(30, 30, 38, 255));
        HubParallaxLayer skylineParallax = skyline.AddComponent<HubParallaxLayer>();
        skylineParallax.SetMultiplier(0.5f);

        if (cameraTransform != null)
        {
            skylineParallax.SetTarget(cameraTransform);
        }

        GameObject skyRift = new GameObject("SkyRift");
        skyRift.transform.SetParent(runtimeRoot, false);
        skyRift.transform.localPosition = new Vector3(0f, 4.8f, 9f);
        SpriteRenderer riftRenderer = skyRift.AddComponent<SpriteRenderer>();
        riftRenderer.sortingLayerName = "Background";
        riftRenderer.sprite = CreateRiftSprite();
        skyRift.AddComponent<CrackAmbientSound>();
        HubParallaxLayer riftParallax = skyRift.AddComponent<HubParallaxLayer>();
        riftParallax.SetMultiplier(0.25f);
        if (cameraTransform != null)
        {
            riftParallax.SetTarget(cameraTransform);
        }
    }

    private void BuildBuildings()
    {
        CreateBuilding("Hospital", HubBuildingType.Hospital, new Vector3(-6f, -1.5f, 0f), new Color32(200, 225, 235, 255), "HOSPITAL");
        CreateBuilding("Association", HubBuildingType.Association, new Vector3(0f, -1.25f, 0f), new Color32(105, 112, 126, 255), "ASOCIACION");
        CreateBuilding("BlackMarket", HubBuildingType.BlackMarket, new Vector3(6f, -1.75f, 0f), new Color32(42, 42, 48, 255), "MERCADO");
    }

    private void CreateBuilding(string objectName, HubBuildingType buildingType, Vector3 position, Color32 color, string label)
    {
        GameObject building = new GameObject(objectName);
        building.transform.SetParent(runtimeRoot, false);
        building.transform.localPosition = position;

        SpriteRenderer renderer = building.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Props";
        renderer.sprite = CreateBlockSprite(48, 64, color, color);

        BoxCollider2D solid = building.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(2.8f, 3.4f);
        solid.offset = new Vector2(0f, -0.1f);
        solid.isTrigger = false;

        GameObject triggerObject = new GameObject("Trigger");
        triggerObject.transform.SetParent(building.transform, false);
        BoxCollider2D trigger = triggerObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(3.8f, 1.6f);
        trigger.offset = new Vector2(0f, -2.35f);
        HubBuildingInteractable interactable = triggerObject.AddComponent<HubBuildingInteractable>();
        interactable.Configure(this, buildingType);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(building.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = label;
        textMesh.characterSize = 0.16f;
        textMesh.fontSize = 32;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = Color.white;
    }

    private void EnsurePromptUi()
    {
        Canvas canvas = FindOverlayCanvas();
        Transform prompt = canvas.transform.Find("HubPrompt");
        if (prompt == null)
        {
            GameObject promptObject = new GameObject("HubPrompt", typeof(RectTransform), typeof(Text));
            prompt = promptObject.transform;
            prompt.SetParent(canvas.transform, false);
        }

        overlayCanvas = canvas;
        promptText = prompt.GetComponent<Text>();
        RectTransform rect = promptText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 24f);
        rect.sizeDelta = new Vector2(240f, 24f);
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 18;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        promptText.gameObject.SetActive(false);
    }

    private void EnsurePanels()
    {
        hospitalUI = hospitalUI != null ? hospitalUI : FindFirstObjectByType<HospitalUI>();
        if (hospitalUI == null)
        {
            hospitalUI = gameObject.AddComponent<HospitalUI>();
        }

        associationUI = associationUI != null ? associationUI : FindFirstObjectByType<AssociationUI>();
        if (associationUI == null)
        {
            associationUI = gameObject.AddComponent<AssociationUI>();
        }

        marketUI = marketUI != null ? marketUI : FindFirstObjectByType<MetaUpgradeShop>();
        if (marketUI == null)
        {
            Debug.LogError("[CityHubBootstrapper] Falta MetaUpgradeShop en escena. Ejecuta Last Ascention / Setup Modal UI Prefabs.");
        }

        CloseAllPanels();
    }

    private void OpenBuilding(HubBuildingType buildingType)
    {
        CloseAllPanels();
        if (buildingType == HubBuildingType.Hospital)
        {
            hospitalUI.Show();
            return;
        }

        if (buildingType == HubBuildingType.Association)
        {
            associationUI.Show();
            return;
        }

        marketUI.Show();
    }

    private void CloseAllPanels()
    {
        if (hospitalUI != null)
        {
            hospitalUI.Hide();
        }

        if (associationUI != null)
        {
            associationUI.Hide();
        }

        if (marketUI != null)
        {
            marketUI.Hide();
        }
    }

    private bool AnyPanelVisible()
    {
        return (hospitalUI != null && hospitalUI.IsVisible)
            || (associationUI != null && associationUI.IsVisible)
            || (marketUI != null && marketUI.IsVisible);
    }

    private Canvas FindOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvases[i];
            }
        }

        GameObject canvasObject = new GameObject("HUDCanvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private Sprite CreateBlockSprite(int width, int height, Color32 primary, Color32 secondary)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, (x + y) % 2 == 0 ? primary : secondary);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 16f);
    }

    private Sprite CreateRiftSprite()
    {
        Texture2D texture = new Texture2D(160, 24, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color color = new Color(0f, 0f, 0f, 0f);
                if (Mathf.Abs(y - 12 + Mathf.Sin(x * 0.12f) * 4f) < 3f)
                {
                    color = new Color(0.08f, 0.6f, 1f, 0.85f);
                }
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
    }
}
