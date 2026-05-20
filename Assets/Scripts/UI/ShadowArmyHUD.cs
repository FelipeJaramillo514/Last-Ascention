using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShadowArmyHUD : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform container;

    private readonly Dictionary<ShadowSoldier, Image> iconsByShadow = new Dictionary<ShadowSoldier, Image>();

    private void Awake()
    {
        EnsureUi();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<ShadowSummonedEvent>(OnShadowSummoned);
        EventBus.Subscribe<ShadowDiedEvent>(OnShadowDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ShadowSummonedEvent>(OnShadowSummoned);
        EventBus.Unsubscribe<ShadowDiedEvent>(OnShadowDied);
    }

    private void OnShadowSummoned(ShadowSummonedEvent summonedEvent)
    {
        if (summonedEvent == null || summonedEvent.soldier == null)
        {
            return;
        }

        EnsureUi();
        if (iconsByShadow.ContainsKey(summonedEvent.soldier))
        {
            return;
        }

        Image icon = CreateIcon();
        iconsByShadow[summonedEvent.soldier] = icon;
        RefreshLayout();
    }

    private void OnShadowDied(ShadowDiedEvent diedEvent)
    {
        if (diedEvent == null || diedEvent.soldier == null)
        {
            return;
        }

        Image icon;
        if (!iconsByShadow.TryGetValue(diedEvent.soldier, out icon) || icon == null)
        {
            return;
        }

        iconsByShadow.Remove(diedEvent.soldier);
        StartCoroutine(RemoveIconRoutine(icon));
    }

    private Image CreateIcon()
    {
        GameObject iconObject = new GameObject("ShadowIcon", typeof(RectTransform), typeof(Image), typeof(Outline));
        iconObject.transform.SetParent(container, false);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = HUDSpriteFactory.GetShadowSprite();
        icon.color = Color.white;
        RectTransform rect = icon.rectTransform;
        rect.sizeDelta = new Vector2(16f, 16f);

        Outline outline = iconObject.GetComponent<Outline>();
        outline.effectDistance = new Vector2(1f, -1f);
        outline.effectColor = new Color(0f, 0.9f, 1f, 1f);
        return icon;
    }

    private IEnumerator RemoveIconRoutine(Image icon)
    {
        if (icon == null)
        {
            yield break;
        }

        Vector3 baseScale = icon.rectTransform.localScale;
        Color baseColor = icon.color;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.3f);
            icon.color = Color.Lerp(Color.red, baseColor, Mathf.PingPong(t * 6f, 1f));
            icon.rectTransform.localScale = Vector3.Lerp(baseScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(icon.gameObject);
        RefreshLayout();
    }

    private void RefreshLayout()
    {
        int index = 0;
        foreach (Image icon in iconsByShadow.Values)
        {
            if (icon == null)
            {
                continue;
            }

            icon.rectTransform.anchorMin = new Vector2(1f, 0f);
            icon.rectTransform.anchorMax = new Vector2(1f, 0f);
            icon.rectTransform.pivot = new Vector2(1f, 0f);
            icon.rectTransform.anchoredPosition = new Vector2(-(index * 20f), 0f);
            index++;
        }
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && container != null)
        {
            return;
        }

        Canvas hostCanvas = FindOverlayCanvas();
        Transform overlay = hostCanvas.transform.Find("ShadowArmyOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("ShadowArmyOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 110;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Transform containerTransform = overlay.Find("Icons");
        if (containerTransform == null)
        {
            GameObject containerObject = new GameObject("Icons", typeof(RectTransform));
            containerTransform = containerObject.transform;
            containerTransform.SetParent(overlay, false);
        }

        container = containerTransform as RectTransform;
        container.anchorMin = new Vector2(1f, 0f);
        container.anchorMax = new Vector2(1f, 0f);
        container.pivot = new Vector2(1f, 0f);
        container.anchoredPosition = new Vector2(-18f, 20f);
        container.sizeDelta = new Vector2(220f, 18f);
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
}
