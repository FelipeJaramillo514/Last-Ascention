using UnityEngine;

/// <summary>
/// Bloquea movimiento/combate del jugador y apertura de otras interfaces mientras hay un modal activo.
/// </summary>
public static class UIModalGate
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetOnSceneLoad()
    {
        ResetForSceneLoad();
    }

    private static int openModalCount;
    private static KaisenController cachedPlayer;
    private static bool playerMovementWasEnabled = true;

    public static bool IsBlockingInteraction => openModalCount > 0;

    public static bool TryAcquire(UIModalPanel panel)
    {
        if (panel == null)
        {
            return false;
        }

        if (openModalCount > 0 && !panel.AllowStacking)
        {
            return false;
        }

        return true;
    }

    public static void NotifyOpened(UIModalPanel panel)
    {
        if (panel == null)
        {
            return;
        }

        openModalCount++;
        if (openModalCount == 1)
        {
            ApplyGameplayBlock();
        }
    }

    public static void NotifyClosed(UIModalPanel panel)
    {
        if (panel == null)
        {
            return;
        }

        openModalCount = Mathf.Max(0, openModalCount - 1);
        if (openModalCount == 0)
        {
            ReleaseGameplayBlock();
        }
    }

    private static void ApplyGameplayBlock()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = Object.FindFirstObjectByType<KaisenController>();
        }

        if (cachedPlayer != null)
        {
            playerMovementWasEnabled = cachedPlayer.IsPlayerInputEnabled;
            cachedPlayer.SetPlayerInputEnabled(false);
        }
    }

    private static void ReleaseGameplayBlock()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = Object.FindFirstObjectByType<KaisenController>();
        }

        if (cachedPlayer != null)
        {
            cachedPlayer.SetPlayerInputEnabled(playerMovementWasEnabled);
        }
    }

    public static void ResetForSceneLoad()
    {
        openModalCount = 0;
        cachedPlayer = null;
    }
}
