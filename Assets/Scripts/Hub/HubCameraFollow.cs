using UnityEngine;

public class HubCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smooth = 12f;
    [SerializeField] private Vector2 framingOffset = Vector2.zero;
    [SerializeField] private float movementLookAhead = 0f;
    [SerializeField] private float maxLookAheadSpeed = 4.5f;
    [SerializeField] private bool constrainToWorldBounds = false;
    [SerializeField] private Vector2 worldMin = new Vector2(-10f, -7.5f);
    [SerializeField] private Vector2 worldMax = new Vector2(10f, 6.5f);

    private Vector3 previousTargetPosition;
    private bool hasPreviousTargetPosition;
    private Camera cachedCamera;

    public void SetTarget(Transform followTarget)
    {
        if (target == followTarget)
        {
            return;
        }

        target = followTarget;
        hasPreviousTargetPosition = false;
    }

    public void SetWorldBounds(Vector2 min, Vector2 max)
    {
        worldMin = min;
        worldMax = max;
        constrainToWorldBounds = false;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position;
        Vector2 velocity = Vector2.zero;
        if (hasPreviousTargetPosition && Time.deltaTime > 0f)
        {
            velocity = (targetPosition - previousTargetPosition) / Time.deltaTime;
            velocity = Vector2.ClampMagnitude(velocity, maxLookAheadSpeed);
        }

        previousTargetPosition = targetPosition;
        hasPreviousTargetPosition = true;

        Vector2 lookAhead = velocity.sqrMagnitude > 0.001f ? velocity.normalized * movementLookAhead : Vector2.zero;
        Vector3 desired = new Vector3(
            targetPosition.x + framingOffset.x + lookAhead.x,
            targetPosition.y + framingOffset.y + lookAhead.y,
            transform.position.z);
        desired = ClampToWorldBounds(desired);
        if (smooth <= 0f)
        {
            transform.position = desired;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
    }

    private Vector3 ClampToWorldBounds(Vector3 desired)
    {
        if (!constrainToWorldBounds)
        {
            return desired;
        }

        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        if (cachedCamera == null || !cachedCamera.orthographic)
        {
            desired.x = Mathf.Clamp(desired.x, worldMin.x, worldMax.x);
            desired.y = Mathf.Clamp(desired.y, worldMin.y, worldMax.y);
            return desired;
        }

        float halfHeight = cachedCamera.orthographicSize;
        float halfWidth = halfHeight * cachedCamera.aspect;
        if (worldMax.x - worldMin.x > halfWidth * 2f)
        {
            desired.x = Mathf.Clamp(desired.x, worldMin.x + halfWidth, worldMax.x - halfWidth);
        }
        else
        {
            desired.x = (worldMin.x + worldMax.x) * 0.5f;
        }

        if (worldMax.y - worldMin.y > halfHeight * 2f)
        {
            desired.y = Mathf.Clamp(desired.y, worldMin.y + halfHeight, worldMax.y - halfHeight);
        }
        else
        {
            desired.y = (worldMin.y + worldMax.y) * 0.5f;
        }

        return desired;
    }
}
