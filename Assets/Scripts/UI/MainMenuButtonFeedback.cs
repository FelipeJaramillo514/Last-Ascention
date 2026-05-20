using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI cursor;
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private float idleFontSize = 20f;
    [SerializeField] private float hoverFontSize = 24f;
    [SerializeField] private float lerpSpeed = 16f;

    private MainMenuController controller;
    private Button button;
    private bool highlighted;
    private float clickScale = 1f;
    private Coroutine clickRoutine;

    private void Awake()
    {
        AutoWire();
    }

    private void Update()
    {
        AutoWire();
        if (label != null)
        {
            float targetSize = highlighted ? hoverFontSize : idleFontSize;
            label.fontSize = Mathf.Lerp(label.fontSize, targetSize, Time.unscaledDeltaTime * lerpSpeed);
        }

        if (cursor != null)
        {
            cursor.gameObject.SetActive(highlighted);
        }

        if (scaleTarget != null)
        {
            float baseScale = highlighted ? 1.04f : 1f;
            scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, Vector3.one * baseScale * clickScale, Time.unscaledDeltaTime * lerpSpeed);
        }
    }

    public void Initialize(MainMenuController owner)
    {
        controller = owner;
        AutoWire();
    }

    public void ConfigureSizing(float idleSize, float hoverSize)
    {
        idleFontSize = idleSize;
        hoverFontSize = hoverSize;
        if (label != null)
        {
            label.fontSize = idleFontSize;
        }
    }

    public void SetVisualColors(Color backgroundColor, Color textColor)
    {
        AutoWire();
        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.color = backgroundColor;
        }

        if (label != null)
        {
            label.color = textColor;
        }
    }

    public void PlayClickFeedback()
    {
        if (clickRoutine != null)
        {
            StopCoroutine(clickRoutine);
        }

        clickRoutine = StartCoroutine(ClickRoutine());
        if (controller != null)
        {
            controller.PlayClickSfx();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null)
        {
            button.Select();
        }

        SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlighted(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlighted(false);
    }

    private void SetHighlighted(bool value)
    {
        if (highlighted == value)
        {
            return;
        }

        highlighted = value;
        if (highlighted && controller != null)
        {
            controller.HandleButtonHighlighted(this);
        }
    }

    private IEnumerator ClickRoutine()
    {
        clickScale = 0.9f;
        yield return new WaitForSecondsRealtime(0.05f);
        clickScale = 1f;
    }

    private void AutoWire()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (scaleTarget == null)
        {
            scaleTarget = transform as RectTransform;
        }

        if (label == null)
        {
            Transform labelTransform = transform.Find("Label");
            if (labelTransform != null)
            {
                label = labelTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (cursor == null)
        {
            Transform cursorTransform = transform.Find("Cursor");
            if (cursorTransform != null)
            {
                cursor = cursorTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (cursor != null && cursor.text != ">")
        {
            cursor.text = ">";
        }
    }
}
