#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Genera prefabs modales editables (Pausa, Fin de run, Mercado negro) y los coloca en escenas.
/// </summary>
public static class ModalUIPrefabBuilder
{
    private const string PrefabFolder = "Assets/UI/Prefabs";
    private const float ButtonHeight = 52f;
    private const float ButtonSpacing = 64f;
    private const float ButtonWidth = 280f;

    [MenuItem("Last Ascention/Setup Modal UI Prefabs")]
    public static void SetupAll()
    {
        EnsureFolder("Assets/UI");
        EnsureFolder("Assets/UI/Prefabs");

        GameObject pausePrefab = BuildPauseMenuPrefab();
        GameObject summaryPrefab = BuildRunSummaryPrefab();
        GameObject marketPrefab = BuildMarketPrefab();

        WireGameplayScene(pausePrefab, summaryPrefab);
        WireCityArkenScene(marketPrefab);
        WireTestScene(pausePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Last Ascention] Prefabs modales creados en " + PrefabFolder + " y enlazados en escenas.");
    }

    private static GameObject BuildPauseMenuPrefab()
    {
        GameObject root = new GameObject("PauseMenu", typeof(RectTransform), typeof(PauseMenu));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        root.AddComponent<GraphicRaycaster>();

        GameObject modalRoot = CreateStretchChild(root.transform, "ModalRoot");
        CanvasGroup modalGroup = modalRoot.AddComponent<CanvasGroup>();

        GameObject dimmer = CreateStretchChild(modalRoot.transform, "Dimmer");
        Image dimmerImage = dimmer.AddComponent<Image>();
        dimmerImage.sprite = HUDSpriteFactory.WhiteSprite;
        dimmerImage.color = new Color(0f, 0f, 0f, 0.72f);
        dimmerImage.raycastTarget = true;

        RectTransform panel = CreatePanel(modalRoot.transform, "Panel", new Vector2(640f, 560f), Vector2.zero);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = HUDSpriteFactory.WhiteSprite;
        panelImage.color = new Color(0.04f, 0.07f, 0.12f, 0.96f);

        Text title = CreateText(panel, "Title", "PAUSA", 32, FontStyle.Bold, Color.white, new Vector2(0f, 220f), new Vector2(400f, 48f));
        Text hint = CreateText(panel, "Hint", "ESC — Cerrar", 16, FontStyle.Normal, new Color(0.7f, 0.8f, 0.9f, 1f), new Vector2(0f, -248f), new Vector2(320f, 28f));

        float y = 120f;
        Button continueBtn = CreateButton(panel, "Button_Continue", "Continuar", new Vector2(0f, y));
        y -= ButtonSpacing;
        Button optionsBtn = CreateButton(panel, "Button_Options", "Opciones", new Vector2(0f, y));
        y -= ButtonSpacing;
        Button abandonBtn = CreateButton(panel, "Button_AbandonRun", "Abandonar Run", new Vector2(0f, y));
        y -= ButtonSpacing;
        Button quitBtn = CreateButton(panel, "Button_Quit", "Salir del Juego", new Vector2(0f, y));

        RectTransform selection = CreatePanel(panel, "Selection", new Vector2(ButtonWidth + 24f, ButtonHeight + 12f), continueBtn.GetComponent<RectTransform>().anchoredPosition);
        Image selectionImage = selection.gameObject.AddComponent<Image>();
        selectionImage.sprite = HUDSpriteFactory.WhiteSprite;
        selectionImage.color = new Color(0f, 0.75f, 1f, 0.22f);
        selection.transform.SetAsFirstSibling();

        RectTransform optionsRoot = CreatePanel(panel, "OptionsPanel", new Vector2(520f, 220f), new Vector2(0f, -40f));
        Image optionsBg = optionsRoot.gameObject.AddComponent<Image>();
        optionsBg.sprite = HUDSpriteFactory.WhiteSprite;
        optionsBg.color = new Color(0.02f, 0.05f, 0.1f, 0.95f);
        optionsRoot.gameObject.SetActive(false);

        Slider master = CreateSlider(optionsRoot, "Master", "MASTER", new Vector2(0f, 64f));
        Slider music = CreateSlider(optionsRoot, "Music", "MUSICA", new Vector2(0f, 0f));
        Slider sfx = CreateSlider(optionsRoot, "Sfx", "SFX", new Vector2(0f, -64f));

        PauseMenu pauseMenu = root.GetComponent<PauseMenu>();
        SetModalFields(pauseMenu, modalRoot, modalGroup, true, true, false);
        SetPauseFields(pauseMenu, panel, selection, continueBtn, optionsBtn, abandonBtn, quitBtn, optionsRoot, master, music, sfx, hint);

        return SavePrefab(root, PrefabFolder + "/PauseMenu.prefab");
    }

    private static GameObject BuildRunSummaryPrefab()
    {
        GameObject root = new GameObject("RunSummaryUI", typeof(RectTransform), typeof(RunSummaryUI));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 340;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        root.AddComponent<GraphicRaycaster>();

        GameObject modalRoot = CreateStretchChild(root.transform, "ModalRoot");
        CanvasGroup modalGroup = modalRoot.AddComponent<CanvasGroup>();

        GameObject dimmer = CreateStretchChild(modalRoot.transform, "Dimmer");
        Image dimmerImage = dimmer.AddComponent<Image>();
        dimmerImage.sprite = HUDSpriteFactory.WhiteSprite;
        dimmerImage.color = new Color(0f, 0f, 0f, 0.82f);
        dimmerImage.raycastTarget = true;

        RectTransform panel = CreatePanel(modalRoot.transform, "Panel", new Vector2(720f, 520f), Vector2.zero);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = HUDSpriteFactory.WhiteSprite;
        panelImage.color = new Color(0.03f, 0.05f, 0.08f, 0.96f);

        Text title = CreateText(panel, "Title", "RUN COMPLETADA", 34, FontStyle.Bold, Color.white, new Vector2(0f, 200f), new Vector2(600f, 52f));
        Text body = CreateText(panel, "Body", "Estadísticas...", 20, FontStyle.Normal, Color.white, new Vector2(-40f, 60f), new Vector2(520f, 200f));
        body.alignment = TextAnchor.UpperLeft;
        Text gold = CreateText(panel, "Gold", "Cristales obtenidos: 0", 24, FontStyle.Bold, new Color(1f, 0.84f, 0f, 1f), new Vector2(0f, -80f), new Vector2(480f, 36f));
        Text footer = CreateText(panel, "Footer", string.Empty, 18, FontStyle.Italic, new Color(0.55f, 0.78f, 1f, 1f), new Vector2(0f, -140f), new Vector2(600f, 56f));
        Button continueBtn = CreateButton(panel, "Button_Continue", "Continuar", new Vector2(0f, -210f));
        Text continueLabel = continueBtn.GetComponentInChildren<Text>();
        Text hint = CreateText(panel, "Hint", "ESC — Cerrar", 16, FontStyle.Normal, new Color(0.65f, 0.75f, 0.85f, 1f), new Vector2(0f, -252f), new Vector2(280f, 28f));

        RunSummaryUI summary = root.GetComponent<RunSummaryUI>();
        SetModalFields(summary, modalRoot, modalGroup, true, true, false);
        SetSummaryFields(summary, panel, title, body, gold, footer, continueBtn, continueLabel, hint);

        return SavePrefab(root, PrefabFolder + "/RunSummaryUI.prefab");
    }

    private static GameObject BuildMarketPrefab()
    {
        GameObject root = new GameObject("MetaUpgradeShop", typeof(RectTransform), typeof(MetaUpgradeShop));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        root.AddComponent<GraphicRaycaster>();

        GameObject modalRoot = CreateStretchChild(root.transform, "ModalRoot");
        CanvasGroup modalGroup = modalRoot.AddComponent<CanvasGroup>();

        GameObject dimmer = CreateStretchChild(modalRoot.transform, "Dimmer");
        Image dimmerImage = dimmer.AddComponent<Image>();
        dimmerImage.sprite = HUDSpriteFactory.WhiteSprite;
        dimmerImage.color = new Color(0f, 0f, 0f, 0.7f);
        dimmerImage.raycastTarget = true;

        RectTransform panel = CreatePanel(modalRoot.transform, "Panel", new Vector2(920f, 560f), Vector2.zero);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = HUDSpriteFactory.WhiteSprite;
        panelImage.color = new Color(0.04f, 0.04f, 0.06f, 0.97f);

        Text title = CreateText(panel, "Title", "MERCADO NEGRO", 30, FontStyle.Bold, Color.white, new Vector2(0f, 232f), new Vector2(520f, 44f));
        Text wallet = CreateText(panel, "Wallet", "Fondos: 0 cristales", 20, FontStyle.Normal, new Color(1f, 0.84f, 0f, 1f), new Vector2(0f, 188f), new Vector2(420f, 32f));
        Text hint = CreateText(panel, "Hint", "ESC — Cerrar", 16, FontStyle.Normal, new Color(0.65f, 0.75f, 0.85f, 1f), new Vector2(0f, -248f), new Vector2(280f, 28f));

        MetaUpgradeCardView[] cards = new MetaUpgradeCardView[5];
        float[] xs = { -220f, 220f, -220f, 220f, 0f };
        float[] ys = { 72f, 72f, -88f, -88f, -248f };
        for (int i = 0; i < 5; i++)
        {
            cards[i] = CreateMarketCard(panel, "Card_" + i, new Vector2(xs[i], ys[i]));
        }

        MetaUpgradeShop shop = root.GetComponent<MetaUpgradeShop>();
        SetModalFields(shop, modalRoot, modalGroup, false, true, false);
        SetMarketFields(shop, wallet, hint, cards);

        return SavePrefab(root, PrefabFolder + "/MetaUpgradeShop.prefab");
    }

    private static MetaUpgradeCardView CreateMarketCard(RectTransform parent, string name, Vector2 position)
    {
        RectTransform card = CreatePanel(parent, name, new Vector2(400f, 148f), position);
        card.pivot = new Vector2(0.5f, 0.5f);
        Image cardBg = card.gameObject.AddComponent<Image>();
        cardBg.sprite = HUDSpriteFactory.WhiteSprite;
        cardBg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

        MetaUpgradeCardView view = new MetaUpgradeCardView();
        view.root = card;
        view.title = CreateText(card, "Title", "Mejora", 20, FontStyle.Bold, Color.white, new Vector2(-80f, 44f), new Vector2(260f, 28f));
        view.title.alignment = TextAnchor.MiddleLeft;
        view.description = CreateText(card, "Description", "Descripcion", 16, FontStyle.Normal, new Color(0.82f, 0.82f, 0.82f, 1f), new Vector2(-80f, 8f), new Vector2(280f, 40f));
        view.description.alignment = TextAnchor.UpperLeft;
        view.price = CreateText(card, "Price", "500 cristales", 16, FontStyle.Normal, new Color(1f, 0.84f, 0f, 1f), new Vector2(-80f, -32f), new Vector2(200f, 24f));
        view.price.alignment = TextAnchor.MiddleLeft;
        view.tier = CreateText(card, "Tier", "Comprado: 0 / III", 14, FontStyle.Normal, new Color(0.6f, 0.8f, 1f, 1f), new Vector2(-80f, -56f), new Vector2(200f, 22f));
        view.tier.alignment = TextAnchor.MiddleLeft;
        view.button = CreateButton(card, "Button", "Comprar", new Vector2(148f, 0f), new Vector2(96f, 44f));
        view.buttonLabel = view.button.GetComponentInChildren<Text>();
        return view;
    }

    private static void WireGameplayScene(GameObject pausePrefab, GameObject summaryPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", OpenSceneMode.Single);
        PlaceModalIfMissing<PauseMenu>(pausePrefab, "PauseMenu");
        PlaceModalIfMissing<RunSummaryUI>(summaryPrefab, "RunSummaryUI");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void WireTestScene(GameObject pausePrefab)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/TestScene.unity", OpenSceneMode.Single);
        PlaceModalIfMissing<PauseMenu>(pausePrefab, "PauseMenu");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void WireCityArkenScene(GameObject marketPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/CityArken.unity", OpenSceneMode.Single);
        MetaUpgradeShop shop = PlaceModalIfMissing<MetaUpgradeShop>(marketPrefab, "MetaUpgradeShop").GetComponent<MetaUpgradeShop>();
        CityHubBootstrapper bootstrapper = Object.FindFirstObjectByType<CityHubBootstrapper>();
        if (bootstrapper != null)
        {
            SerializedObject so = new SerializedObject(bootstrapper);
            so.FindProperty("marketUI").objectReferenceValue = shop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject PlaceModalIfMissing<T>(GameObject prefab, string objectName) where T : Component
    {
        T existing = Object.FindFirstObjectByType<T>();
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = objectName;
        return instance;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void SetModalFields(UIModalPanel panel, GameObject modalRoot, CanvasGroup group, bool pauseTime, bool closeEsc, bool allowStack)
    {
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        so.FindProperty("modalCanvasGroup").objectReferenceValue = group;
        so.FindProperty("pauseTimeScale").boolValue = pauseTime;
        so.FindProperty("closeOnEscape").boolValue = closeEsc;
        so.FindProperty("allowStacking").boolValue = allowStack;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPauseFields(PauseMenu menu, RectTransform panel, RectTransform selection, Button c, Button o, Button a, Button q, RectTransform options, Slider m, Slider mu, Slider s, Text hint)
    {
        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("selectionRect").objectReferenceValue = selection;
        so.FindProperty("continueButton").objectReferenceValue = c;
        so.FindProperty("optionsButton").objectReferenceValue = o;
        so.FindProperty("abandonRunButton").objectReferenceValue = a;
        so.FindProperty("quitButton").objectReferenceValue = q;
        so.FindProperty("optionsRoot").objectReferenceValue = options;
        so.FindProperty("masterSlider").objectReferenceValue = m;
        so.FindProperty("musicSlider").objectReferenceValue = mu;
        so.FindProperty("sfxSlider").objectReferenceValue = s;
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSummaryFields(RunSummaryUI ui, RectTransform panel, Text title, Text body, Text gold, Text footer, Button btn, Text btnLabel, Text hint)
    {
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("goldText").objectReferenceValue = gold;
        so.FindProperty("footerText").objectReferenceValue = footer;
        so.FindProperty("continueButton").objectReferenceValue = btn;
        so.FindProperty("continueButtonLabel").objectReferenceValue = btnLabel;
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetMarketFields(MetaUpgradeShop shop, Text wallet, Text hint, MetaUpgradeCardView[] cards)
    {
        SerializedObject so = new SerializedObject(shop);
        so.FindProperty("walletText").objectReferenceValue = wallet;
        so.FindProperty("hintText").objectReferenceValue = hint;
        SerializedProperty array = so.FindProperty("upgradeCards");
        array.arraySize = cards.Length;
        for (int i = 0; i < cards.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("root").objectReferenceValue = cards[i].root;
            element.FindPropertyRelative("title").objectReferenceValue = cards[i].title;
            element.FindPropertyRelative("description").objectReferenceValue = cards[i].description;
            element.FindPropertyRelative("price").objectReferenceValue = cards[i].price;
            element.FindPropertyRelative("tier").objectReferenceValue = cards[i].tier;
            element.FindPropertyRelative("button").objectReferenceValue = cards[i].button;
            element.FindPropertyRelative("buttonLabel").objectReferenceValue = cards[i].buttonLabel;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateStretchChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static Text CreateText(RectTransform parent, string name, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 rectSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = rectSize;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 pos, Vector2? sizeOverride = null)
    {
        Vector2 size = sizeOverride ?? new Vector2(ButtonWidth, ButtonHeight);
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        Image image = go.GetComponent<Image>();
        image.sprite = HUDSpriteFactory.WhiteSprite;
        image.color = new Color(0.1f, 0.14f, 0.22f, 0.95f);
        Button button = go.GetComponent<Button>();
        CreateText(rect, "Label", label, 20, FontStyle.Bold, Color.white, Vector2.zero, size);
        return button;
    }

    private static Slider CreateSlider(RectTransform parent, string name, string label, Vector2 pos)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(460f, 48f);
        rowRect.anchoredPosition = pos;

        CreateText(rowRect, "Label", label, 18, FontStyle.Bold, Color.white, new Vector2(-150f, 0f), new Vector2(120f, 32f)).alignment = TextAnchor.MiddleLeft;

        GameObject sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(260f, 28f);
        sliderRect.anchoredPosition = new Vector2(60f, 0f);
        Image bg = sliderGo.GetComponent<Image>();
        bg.sprite = HUDSpriteFactory.WhiteSprite;
        bg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        Slider slider = sliderGo.GetComponent<Slider>();

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(6f, 6f);
        fillAreaRect.offsetMax = new Vector2(-6f, -6f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = HUDSpriteFactory.WhiteSprite;
        fillImage.color = new Color(0f, 0.75f, 1f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(sliderGo.transform, false);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = HUDSpriteFactory.WhiteSprite;
        handleImage.color = Color.white;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(14f, 28f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.75f;
        return slider;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
