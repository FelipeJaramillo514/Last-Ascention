using UnityEngine;

public class HubCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smooth = 6f;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
    }
}
