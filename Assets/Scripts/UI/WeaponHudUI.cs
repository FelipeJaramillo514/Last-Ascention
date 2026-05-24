using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHudUI : MonoBehaviour
{
    public static WeaponHudUI Instance { get; private set; }

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform activePanel;
    [SerializeField] private Image activeIcon;
    [SerializeField] private Text activeNameText;
    [SerializeField] private Image ammoFill;
    [SerializeField] private Text ammoText;
    [SerializeField] private RectTransform secondaryPanel;
    [SerializeField] private Image secondaryIcon;
    [SerializeField] private Text secondaryNameText;
    [SerializeField] private Text secondaryHintText;
    [SerializeField] private Image dodgeIcon;
    [SerializeField] private Image dodgeCooldownFill;

    private Font uiFont;
    private int lastActiveSlot = -1;
    private Coroutine swapRoutine;
    private Coroutine ammoFlashRoutine;
    private AudioSource audioSource;
    private AudioClip emptyClickClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUi();
    }

    public void Refresh(WeaponManager manager)
    {
        KaisenController controller = manager != null ? manager.GetComponent<KaisenController>() : null;
        Refresh(manager, controller);
    }

    public void Refresh(WeaponManager manager, KaisenController controller)
    {
        EnsureUi();
        if (manager == null)
        {
            return;
        }

        int activeSlot = manager.ActiveSlot;
        WeaponData activeWeapon = manager.GetWeaponAtSlot(activeSlot);
        int activeAmmo = manager.GetAmmoForSlot(activeSlot);
        int secondarySlot = activeSlot == 0 ? 1 : 0;
        WeaponData secondaryWeapon = manager.GetWeaponAtSlot(secondarySlot);

        if (activeSlot != lastActiveSlot)
        {
            PlaySwapAnimation();
            lastActiveSlot = activeSlot;
        }

        Sprite activeWeaponIcon = WeaponVisualResolver.GetWeaponIcon(activeWeapon);
        activeIcon.enabled = activeWeaponIcon != null;
        activeIcon.sprite = activeWeaponIcon;
        activeNameText.text = activeWeapon != null ? activeWeapon.weaponName : "Sin arma";
        UpdateAmmoDisplay(activeWeapon, activeAmmo);

        Sprite secondaryWeaponIcon = WeaponVisualResolver.GetWeaponIcon(secondaryWeapon);
        secondaryIcon.enabled = secondaryWeaponIcon != null;
        secondaryIcon.sprite = secondaryWeaponIcon;
        secondaryPanel.gameObject.SetActive(secondaryWeapon != null);
        secondaryNameText.text = secondaryWeapon != null ? secondaryWeapon.weaponName : "Slot vacio";

        if (controller != null)
        {
            SpriteRenderer playerSprite = controller.GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null && playerSprite.sprite != null)
            {
                dodgeIcon.sprite = playerSprite.sprite;
                dodgeIcon.color = Color.white;
            }

            dodgeCooldownFill.fillAmount = controller.DodgeCooldownNormalized;
        }
        else
        {
            dodgeCooldownFill.fillAmount = 1f;
        }
    }

    public void OnWeaponFired(WeaponFiredEvent weaponEvent)
    {
        if (weaponEvent == null)
        {
            return;
        }

        if (weaponEvent.remainingAmmo == 0 && weaponEvent.weaponData != null && weaponEvent.weaponData.maxAmmo > 0)
        {
            PlayEmptyAmmoFeedback();
        }
    }

    public void OnWeaponEmpty()
    {
        PlayEmptyAmmoFeedback();
    }

    public void OnWeaponSwapped()
    {
        PlaySwapAnimation();
    }

    private void UpdateAmmoDisplay(WeaponData weaponData, int ammo)
    {
        if (weaponData == null)
        {
            ammoFill.fillAmount = 0f;
            ammoText.text = "--";
            return;
        }

        if (weaponData.maxAmmo < 0)
        {
            ammoFill.fillAmount = 1f;
            ammoText.text = "INF";
            return;
        }

        float normalized = weaponData.maxAmmo <= 0 ? 0f : Mathf.Clamp01((float)Mathf.Max(0, ammo) / weaponData.maxAmmo);
        ammoFill.fillAmount = normalized;
        ammoText.text = Mathf.Max(0, ammo) + "/" + weaponData.maxAmmo;
    }

    private void PlaySwapAnimation()
    {
        EnsureUi();
        if (!gameObject.activeInHierarchy)
        {
            activePanel.localScale = Vector3.one;
            return;
        }

        if (swapRoutine != null)
        {
            StopCoroutine(swapRoutine);
        }

        swapRoutine = StartCoroutine(SwapRoutine());
    }

    private void PlayEmptyAmmoFeedback()
    {
        EnsureUi();
        if (ammoFlashRoutine != null)
        {
            StopCoroutine(ammoFlashRoutine);
        }

        if (audioSource != null)
        {
            if (emptyClickClip == null)
            {
                emptyClickClip = CreateEmptyClickClip();
            }

            audioSource.PlayOneShot(emptyClickClip, 0.35f);
        }

        ammoFlashRoutine = StartCoroutine(AmmoFlashRoutine());
    }

    private IEnumerator SwapRoutine()
    {
        Vector3 baseScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.1f);
            activePanel.localScale = Vector3.Lerp(baseScale, baseScale * 1.2f, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.1f);
            activePanel.localScale = Vector3.Lerp(baseScale * 1.2f, baseScale, t);
            yield return null;
        }

        activePanel.localScale = baseScale;
        swapRoutine = null;
    }

    private IEnumerator AmmoFlashRoutine()
    {
        Color baseColor = ammoFill.color;
        for (int i = 0; i < 3; i++)
        {
            ammoFill.color = new Color(1f, 0.15f, 0.15f, 1f);
            yield return new WaitForSecondsRealtime(0.08f);
            ammoFill.color = baseColor;
            yield return new WaitForSecondsRealtime(0.08f);
        }

        ammoFill.color = baseColor;
        ammoFlashRoutine = null;
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && activePanel != null && activeIcon != null && activeNameText != null && ammoFill != null && ammoText != null && secondaryPanel != null && secondaryIcon != null && secondaryNameText != null && dodgeIcon != null && dodgeCooldownFill != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        audioSource = audioSource != null ? audioSource : GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        Canvas hostCanvas = FindOverlayCanvas();
        Transform overlay = hostCanvas.transform.Find("WeaponHudOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("WeaponHudOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 111;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        activePanel = EnsurePanel(overlay, "ActivePanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -158f), new Vector2(342f, 82f), new Color(0.015f, 0.024f, 0.036f, 0.88f));
        secondaryPanel = EnsurePanel(overlay, "SecondaryPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -248f), new Vector2(304f, 44f), new Color(0.01f, 0.016f, 0.026f, 0.78f));

        Image activeAccent = EnsureImage(activePanel, "PowerAccent", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(4f, 0f));
        activeAccent.sprite = HUDSpriteFactory.WhiteSprite;
        activeAccent.color = new Color(0.72f, 0.24f, 1f, 0.85f);

        Text activeLabel = EnsureText(activePanel, "Label", 11, TextAnchor.MiddleLeft, new Color(0.56f, 0.94f, 1f, 1f), new Vector2(76f, 27f), new Vector2(106f, 16f));
        activeLabel.text = "ARMA ACTIVA";

        activeIcon = EnsureImage(activePanel, "ActiveIcon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(48f, 48f));
        activeIcon.preserveAspect = true;
        activeNameText = EnsureText(activePanel, "ActiveName", 18, TextAnchor.MiddleLeft, Color.white, new Vector2(76f, 10f), new Vector2(162f, 24f));

        RectTransform ammoRoot = EnsureRect(activePanel, "AmmoRoot", new Vector2(76f, -19f), new Vector2(162f, 14f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        Image ammoBackground = ammoRoot.GetComponent<Image>();
        if (ammoBackground == null)
        {
            ammoBackground = ammoRoot.gameObject.AddComponent<Image>();
        }
        ammoBackground.sprite = HUDSpriteFactory.WhiteSprite;
        ammoBackground.color = new Color(0.025f, 0.025f, 0.035f, 0.96f);

        ammoFill = EnsureImage(ammoRoot, "Fill", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ammoFill.sprite = HUDSpriteFactory.WhiteSprite;
        ammoFill.type = Image.Type.Filled;
        ammoFill.fillMethod = Image.FillMethod.Horizontal;
        ammoFill.fillOrigin = 0;
        ammoFill.color = new Color(0f, 0.82f, 1f, 0.95f);

        ammoText = EnsureText(activePanel, "AmmoText", 16, TextAnchor.MiddleRight, Color.white, new Vector2(246f, -18f), new Vector2(72f, 18f));

        RectTransform dodgeRoot = EnsureRect(activePanel, "DodgeRoot", new Vector2(304f, 0f), new Vector2(38f, 38f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
        Image dodgeBorder = dodgeRoot.GetComponent<Image>();
        if (dodgeBorder == null)
        {
            dodgeBorder = dodgeRoot.gameObject.AddComponent<Image>();
        }
        dodgeBorder.sprite = HUDSpriteFactory.WhiteSprite;
        dodgeBorder.color = new Color(0f, 0.76f, 1f, 0.14f);

        dodgeCooldownFill = EnsureImage(dodgeRoot, "Cooldown", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dodgeCooldownFill.sprite = HUDSpriteFactory.WhiteSprite;
        dodgeCooldownFill.type = Image.Type.Filled;
        dodgeCooldownFill.fillMethod = Image.FillMethod.Radial360;
        dodgeCooldownFill.fillClockwise = false;
        dodgeCooldownFill.fillAmount = 1f;
        dodgeCooldownFill.color = new Color(0f, 0.86f, 1f, 0.72f);

        dodgeIcon = EnsureImage(dodgeRoot, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
        dodgeIcon.sprite = HUDSpriteFactory.GetShadowSprite();
        dodgeIcon.color = Color.white;
        dodgeIcon.preserveAspect = true;

        secondaryIcon = EnsureImage(secondaryPanel, "SecondaryIcon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(28f, 28f));
        secondaryIcon.color = new Color(1f, 1f, 1f, 0.78f);
        secondaryIcon.preserveAspect = true;
        secondaryNameText = EnsureText(secondaryPanel, "SecondaryName", 14, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.78f), new Vector2(48f, 0f), new Vector2(170f, 20f));
        secondaryHintText = EnsureText(secondaryPanel, "Hint", 12, TextAnchor.MiddleRight, new Color(0.48f, 0.9f, 1f, 1f), new Vector2(240f, 0f), new Vector2(48f, 20f));
        secondaryHintText.text = "[Q]";
    }

    private AudioClip CreateEmptyClickClip()
    {
        const int sampleRate = 22050;
        const int sampleCount = 2048;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - (i / (float)sampleCount));
            samples[i] = Mathf.Sin(t * 680f) * envelope * 0.08f;
        }

        AudioClip clip = AudioClip.Create("EmptyClick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private Canvas FindOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas fallback = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                continue;
            }

            if (canvas.name == "HUDCanvas")
            {
                return canvas;
            }

            if (fallback == null)
            {
                fallback = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            }
        }

        if (fallback != null)
        {
            return fallback;
        }

        GameObject canvasObject = new GameObject("HUDCanvas", typeof(RectTransform));
        Canvas newCanvas = canvasObject.AddComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        return newCanvas;
    }

    private RectTransform EnsurePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform rect = EnsureRect(parent, name, anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot);
        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }
        image.sprite = HUDSpriteFactory.WhiteSprite;
        image.color = color;

        Outline outline = rect.GetComponent<Outline>();
        if (outline == null)
        {
            outline = rect.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0.75f, 1f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);
        return rect;
    }

    private RectTransform EnsureRect(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            target = child.transform;
            target.SetParent(parent, false);
        }

        RectTransform rect = target as RectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private Image EnsureImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
            target = child.transform;
            target.SetParent(parent, false);
        }

        Image image = target.GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        if (sizeDelta != Vector2.zero)
        {
            rect.sizeDelta = sizeDelta;
        }
        return image;
    }

    private Text EnsureText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
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
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
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
