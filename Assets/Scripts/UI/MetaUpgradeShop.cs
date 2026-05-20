using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MetaUpgradeShop : MonoBehaviour
{
    private sealed class UpgradeDefinition
    {
        public string key;
        public string name;
        public string description;
        public int cost;
    }

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Text walletText;

    private readonly List<UpgradeDefinition> definitions = new List<UpgradeDefinition>();
    private Font uiFont;
    private bool visible;

    public bool IsVisible => visible;

    private void Awake()
    {
        definitions.Add(new UpgradeDefinition { key = "vitalidad_extra", name = "Vitalidad extra", description = "+5 MaxHP base", cost = 500 });
        definitions.Add(new UpgradeDefinition { key = "slot_sombra", name = "Slot de sombra", description = "+1 slot permanente", cost = 1000 });
        definitions.Add(new UpgradeDefinition { key = "filo_eterno", name = "Filo eterno", description = "+5% dano base de espada", cost = 800 });
        definitions.Add(new UpgradeDefinition { key = "reflejos", name = "Reflejos", description = "-0.1s cooldown dodge", cost = 600 });
        definitions.Add(new UpgradeDefinition { key = "percepcion_aguda", name = "Percepcion aguda", description = "+3 Perception base", cost = 700 });
        EnsureUi();
    }

    public void Show()
    {
        EnsureUi();
        visible = true;
        panelRoot.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        visible = false;
        panelRoot.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        EnsureUi();
        for (int i = 0; i < definitions.Count; i++)
        {
            UpgradeDefinition definition = definitions[i];
            RectTransform card = panelRoot.Find("Card_" + i) as RectTransform;
            if (card == null)
            {
                continue;
            }

            Text title = card.Find("Title").GetComponent<Text>();
            Text description = card.Find("Description").GetComponent<Text>();
            Text price = card.Find("Price").GetComponent<Text>();
            Text tier = card.Find("Tier").GetComponent<Text>();
            Button button = card.Find("Button").GetComponent<Button>();
            Text buttonText = button.GetComponentInChildren<Text>();

            int purchaseCount = PersistentData.Instance.GetUpgradePurchaseCount(definition.key);
            title.text = definition.name;
            description.text = definition.description;
            price.text = definition.cost + " cristales";
            tier.text = "Comprado: " + purchaseCount + " / III";
            button.interactable = purchaseCount < 3 && PersistentData.Instance.SpendableGold >= definition.cost;
            buttonText.text = purchaseCount >= 3 ? "MAX" : "Comprar";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (PersistentData.Instance.GetUpgradePurchaseCount(definition.key) >= 3)
                {
                    return;
                }

                if (PersistentData.Instance.PurchaseUpgrade(definition.key, definition.cost))
                {
                    if (GameStateManager.Instance != null)
                    {
                        GameStateManager.Instance.SaveCurrentState();
                    }
                    Refresh();
                }
            });
        }

        walletText.text = "Fondos: " + Mathf.RoundToInt(PersistentData.Instance.SpendableGold) + " cristales";
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && panelRoot != null && walletText != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas canvas = FindOverlayCanvas();
        EnsureEventSystem();

        Transform overlay = canvas.transform.Find("MarketOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("MarketOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(canvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 220;
        if (overlay.GetComponent<GraphicRaycaster>() == null)
        {
            overlay.gameObject.AddComponent<GraphicRaycaster>();
        }


        panelRoot = EnsureRect(overlay, "Panel", Vector2.zero, new Vector2(820f, 460f), new Vector2(0.5f, 0.5f));
        Image background = panelRoot.GetComponent<Image>();
        if (background == null)
        {
            background = panelRoot.gameObject.AddComponent<Image>();
        }

        background.sprite = HUDSpriteFactory.WhiteSprite;
        background.color = new Color(0.05f, 0.05f, 0.07f, 0.96f);

        Text titleText = EnsureText(panelRoot, "Title", 28, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 198f), new Vector2(520f, 36f), new Vector2(0.5f, 0.5f));
        titleText.text = "MERCADO NEGRO";
        walletText = EnsureText(panelRoot, "Wallet", 18, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0f, 1f), new Vector2(0f, 168f), new Vector2(360f, 22f), new Vector2(0.5f, 0.5f));

        for (int i = 0; i < definitions.Count; i++)
        {
            RectTransform card = EnsureRect(panelRoot, "Card_" + i, new Vector2(-210f + (i % 2) * 420f, 120f - (i / 2) * 140f), new Vector2(380f, 120f), new Vector2(0f, 0.5f));
            Image cardImage = card.GetComponent<Image>();
            if (cardImage == null)
            {
                cardImage = card.gameObject.AddComponent<Image>();
            }

            cardImage.sprite = HUDSpriteFactory.WhiteSprite;
            cardImage.color = new Color(0f, 0f, 0f, 0.72f);

            EnsureText(card, "Title", 18, TextAnchor.MiddleLeft, Color.white, new Vector2(12f, 38f), new Vector2(240f, 22f), new Vector2(0f, 0.5f));
            EnsureText(card, "Description", 15, TextAnchor.MiddleLeft, new Color(0.82f, 0.82f, 0.82f, 1f), new Vector2(12f, 12f), new Vector2(260f, 20f), new Vector2(0f, 0.5f));
            EnsureText(card, "Price", 15, TextAnchor.MiddleLeft, new Color(1f, 0.84f, 0f, 1f), new Vector2(12f, -18f), new Vector2(180f, 20f), new Vector2(0f, 0.5f));
            EnsureText(card, "Tier", 13, TextAnchor.MiddleLeft, new Color(0.65f, 0.8f, 1f, 1f), new Vector2(12f, -40f), new Vector2(180f, 18f), new Vector2(0f, 0.5f));

            RectTransform buttonRect = EnsureRect(card, "Button", new Vector2(300f, -4f), new Vector2(64f, 34f), new Vector2(0.5f, 0.5f));
            Image buttonImage = buttonRect.GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = buttonRect.gameObject.AddComponent<Image>();
            }

            buttonImage.sprite = HUDSpriteFactory.WhiteSprite;
            buttonImage.color = new Color(0.18f, 0.45f, 0.85f, 0.95f);
            Button button = buttonRect.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRect.gameObject.AddComponent<Button>();
            }

            Text buttonText = EnsureText(buttonRect, "Text", 15, TextAnchor.MiddleCenter, Color.white, Vector2.zero, buttonRect.sizeDelta, new Vector2(0.5f, 0.5f));
            buttonText.text = "Comprar";
        }

        panelRoot.gameObject.SetActive(false);
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

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }

    private RectTransform EnsureRect(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            target = child.transform;
            target.SetParent(parent, false);
        }

        RectTransform rect = target as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private Text EnsureText(Transform parent, string name, int fontSize, TextAnchor anchor, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Text));
            target = child.transform;
            target.SetParent(parent, false);
        }

        Text text = target.GetComponent<Text>();
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }
}
