using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticFeedbackManager : MonoBehaviour
{
    public static HapticFeedbackManager Instance { get; private set; }

    private Coroutine rumbleRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<HapticFeedbackManager>() != null)
        {
            return;
        }

        new GameObject("HapticFeedbackManager").AddComponent<HapticFeedbackManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TriggerForShake(float magnitude)
    {
        if (magnitude >= CameraShakeManager.BossSlam)
        {
            Pulse(0.9f, 1f, 0.22f, true);
            return;
        }

        if (magnitude >= CameraShakeManager.BossCharge)
        {
            Pulse(0.65f, 0.85f, 0.18f, true);
            return;
        }

        if (magnitude >= CameraShakeManager.HitHeavy)
        {
            Pulse(0.45f, 0.6f, 0.12f, false);
            return;
        }

        if (magnitude >= CameraShakeManager.HitLight)
        {
            Pulse(0.2f, 0.35f, 0.08f, false);
        }
    }

    public void Pulse(float lowFrequency, float highFrequency, float duration, bool vibrate)
    {
        if (vibrate && Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }

        if (Gamepad.current == null)
        {
            return;
        }

        if (rumbleRoutine != null)
        {
            StopCoroutine(rumbleRoutine);
        }

        rumbleRoutine = StartCoroutine(RumbleRoutine(Mathf.Clamp01(lowFrequency), Mathf.Clamp01(highFrequency), Mathf.Max(0.02f, duration)));
    }

    private IEnumerator RumbleRoutine(float lowFrequency, float highFrequency, float duration)
    {
        Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
        yield return new WaitForSecondsRealtime(duration);
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }

        rumbleRoutine = null;
    }
}
