using UnityEngine;

public class HubParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private float multiplier = 0.5f;

    private Vector3 initialPosition;

    private void Awake()
    {
        initialPosition = transform.position;
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }

    public void SetTarget(Transform followTarget)
    {
        targetCamera = followTarget;
    }

    public void SetMultiplier(float parallaxMultiplier)
    {
        multiplier = parallaxMultiplier;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 cameraPosition = targetCamera.position;
        transform.position = new Vector3(initialPosition.x + cameraPosition.x * multiplier, initialPosition.y + cameraPosition.y * multiplier * 0.15f, initialPosition.z);
    }
}
