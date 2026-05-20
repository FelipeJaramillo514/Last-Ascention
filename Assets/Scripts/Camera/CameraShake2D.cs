using System.Collections;
using UnityEngine;

public class CameraShake2D : MonoBehaviour
{
    public static CameraShake2D Instance { get; private set; }

    [SerializeField] private Transform shakeTarget;

    private Coroutine shakeRoutine;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (shakeTarget == null)
        {
            shakeTarget = transform;
        }
        baseLocalPosition = shakeTarget.localPosition;
    }

    public void Shake(float magnitude, float duration = 0.25f)
    {
        if (shakeTarget == null)
        {
            shakeTarget = transform;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeRoutine(Mathf.Max(0f, magnitude), Mathf.Max(0.05f, duration)));
    }

    private IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        baseLocalPosition = shakeTarget.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damping = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * magnitude * damping;
            shakeTarget.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        shakeTarget.localPosition = baseLocalPosition;
        shakeRoutine = null;
    }
}

