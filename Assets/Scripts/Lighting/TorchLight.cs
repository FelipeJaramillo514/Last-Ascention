using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TorchLight : MonoBehaviour
{
    [SerializeField] private Light2D pointLight;
    [SerializeField] private ParticleSystem sparkParticles;
    [SerializeField] private float minIntensity = 2.2f;
    [SerializeField] private float maxIntensity = 3.15f;

    private void Awake()
    {
        EnsureLight();
        EnsureParticles();
        StartCoroutine(FlickerRoutine());
    }

    private void EnsureLight()
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
        pointLight.color = new Color(1f, 0.55f, 0.26f, 1f);
        pointLight.intensity = 2.65f;
        pointLight.pointLightOuterRadius = 6.2f;
        pointLight.pointLightInnerRadius = 0.9f;
    }

    private void EnsureParticles()
    {
        if (sparkParticles == null)
        {
            Transform existing = transform.Find("SparkParticles");
            if (existing != null)
            {
                sparkParticles = existing.GetComponent<ParticleSystem>();
            }
        }

        if (sparkParticles == null)
        {
            GameObject particleObject = new GameObject("SparkParticles");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = Vector3.zero;
            sparkParticles = particleObject.AddComponent<ParticleSystem>();
        }

        ParticleSystem.MainModule main = sparkParticles.main;
        main.loop = true;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.5f;
        main.startSize = 0.08f;
        main.startColor = new Color(1f, 0.78f, 0.2f, 0.9f);
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = sparkParticles.emission;
        emission.rateOverTime = 5f;

        ParticleSystem.ShapeModule shape = sparkParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = sparkParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.5f, 0.15f), 0f), new GradientColorKey(new Color(1f, 0.95f, 0.35f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.VelocityOverLifetimeModule velocity = sparkParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.5f);

        ParticleSystemRenderer renderer = sparkParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "VFX";
        renderer.sortingOrder = 12;

        if (!sparkParticles.isPlaying)
        {
            sparkParticles.Play();
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (enabled)
        {
            if (pointLight != null)
            {
                pointLight.intensity = Mathf.Clamp(pointLight.intensity + Random.Range(-0.2f, 0.2f), minIntensity, maxIntensity);
            }

            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }
}
