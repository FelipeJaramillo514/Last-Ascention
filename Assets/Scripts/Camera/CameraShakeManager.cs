using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour
{
    public const float HitLight = 0.1f;
    public const float HitHeavy = 0.3f;
    public const float BossSlam = 0.8f;
    public const float BossCharge = 0.5f;

    public static CameraShakeManager Instance { get; private set; }

    [SerializeField] private CinemachineImpulseSource impulseSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<CameraShakeManager>() != null)
        {
            return;
        }

        GameObject cameraShakeObject = new GameObject("CameraShakeManager");
        cameraShakeObject.AddComponent<CameraShakeManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        impulseSource = impulseSource != null ? impulseSource : GetComponent<CinemachineImpulseSource>();
        ConfigureImpulseSource();
    }

    public void Shake(float magnitude)
    {
        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null)
            {
                return;
            }
        }

        impulseSource.GenerateImpulse(Vector3.right * magnitude);
        if (HapticFeedbackManager.Instance != null)
        {
            HapticFeedbackManager.Instance.TriggerForShake(magnitude);
        }
    }

    private void ConfigureImpulseSource()
    {
        if (impulseSource == null)
        {
            return;
        }

        impulseSource.DefaultVelocity = Vector3.right;
        impulseSource.ImpulseDefinition.ImpulseDuration = 0.18f;
        impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        impulseSource.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        impulseSource.ImpulseDefinition.DissipationDistance = 60f;
        impulseSource.ImpulseDefinition.DissipationRate = 0.2f;
    }
}
