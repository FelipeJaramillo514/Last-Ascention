using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CrackAmbientSound : MonoBehaviour
{
    [SerializeField] private float baseVolume = 0.05f;
    [SerializeField] private float maxDistance = 12f;
    [SerializeField] private AudioSource source;

    private Transform playerTarget;

    private void Awake()
    {
        source = source != null ? source : GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 0.5f;
        source.maxDistance = maxDistance;
        source.clip = AudioManager.Instance != null ? AudioManager.Instance.GetCueClip(AudioCueId.RiftHum) : null;
        source.volume = 0f;
        if (source.clip != null)
        {
            source.Play();
        }
    }

    private void Update()
    {
        if (source != null && source.clip == null && AudioManager.Instance != null)
        {
            source.clip = AudioManager.Instance.GetCueClip(AudioCueId.RiftHum);
            if (!source.isPlaying && source.clip != null)
            {
                source.Play();
            }
        }

        if (playerTarget == null)
        {
            KaisenController player = FindFirstObjectByType<KaisenController>();
            playerTarget = player != null ? player.transform : null;
        }

        if (playerTarget == null || source == null)
        {
            return;
        }

        float distance = Vector2.Distance(playerTarget.position, transform.position);
        float proximity = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, maxDistance));
        source.volume = baseVolume * proximity;
    }
}
