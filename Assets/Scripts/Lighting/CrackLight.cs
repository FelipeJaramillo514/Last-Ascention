using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CrackLight : MonoBehaviour
{
    [SerializeField] private Light2D pointLight;
    [SerializeField] private float minIntensity = 1.65f;
    [SerializeField] private float maxIntensity = 2.25f;

    private void Awake()
    {
        if (pointLight == null)
        {
            pointLight = GetComponent<Light2D>();
            if (pointLight == null)
            {
                pointLight = gameObject.AddComponent<Light2D>();
            }
        }

        pointLight.lightType = Light2D.LightType.Point;
        pointLight.color = new Color(0.29f, 0.56f, 0.85f, 1f);
        pointLight.intensity = 1.95f;
        pointLight.pointLightOuterRadius = 5.5f;
        pointLight.pointLightInnerRadius = 0.6f;
        StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        while (enabled)
        {
            if (pointLight != null)
            {
                pointLight.intensity = Mathf.Clamp(pointLight.intensity + Random.Range(-0.08f, 0.08f), minIntensity, maxIntensity);
            }

            yield return new WaitForSeconds(Random.Range(0.25f, 0.45f));
        }
    }
}
