using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base para interfaces modales: bloqueo de gameplay, ESC para cerrar, sin creación runtime de jerarquía.
/// </summary>
public abstract class UIModalPanel : MonoBehaviour
{
    [Header("Modal Root")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private CanvasGroup modalCanvasGroup;

    [Header("Behaviour")]
    [SerializeField] private bool pauseTimeScale = true;
    [SerializeField] private bool closeOnEscape = true;
    [SerializeField] private bool allowStacking;

    private bool isOpen;
    private float previousTimeScale = 1f;

    public bool IsOpen => isOpen;
    public bool AllowStacking => allowStacking;

    protected virtual void Awake()
    {
        ValidateEditorReferences();
        HideImmediate();
    }

    protected virtual void Update()
    {
        if (!isOpen || !closeOnEscape)
        {
            return;
        }

        if (WasEscapePressedThisFrame())
        {
            OnEscapePressed();
        }
    }

    protected virtual void OnEscapePressed()
    {
        Close();
    }

    public virtual void Open()
    {
        if (isOpen)
        {
            return;
        }

        if (!UIModalGate.TryAcquire(this))
        {
            return;
        }

        isOpen = true;
        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
        }

        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 1f;
            modalCanvasGroup.interactable = true;
            modalCanvasGroup.blocksRaycasts = true;
        }

        if (pauseTimeScale)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        UIModalGate.NotifyOpened(this);
        OnOpened();
    }

    public virtual void Close()
    {
        if (!isOpen)
        {
            return;
        }

        OnClosing();
        HideImmediate();
        UIModalGate.NotifyClosed(this);

        if (pauseTimeScale)
        {
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        }
    }

    protected void HideImmediate()
    {
        isOpen = false;
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }

        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 0f;
            modalCanvasGroup.interactable = false;
            modalCanvasGroup.blocksRaycasts = false;
        }
    }

    protected virtual void OnOpened()
    {
    }

    protected virtual void OnClosing()
    {
    }

    protected virtual void ValidateEditorReferences()
    {
        if (modalRoot == null)
        {
            Debug.LogError("[UIModalPanel] Falta asignar modalRoot en " + name + ". Usa Last Ascention / Setup Modal UI Prefabs.", this);
        }
    }

    protected static bool WasEscapePressedThisFrame()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }
}
