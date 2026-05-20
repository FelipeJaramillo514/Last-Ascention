using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField] private int pooledHitEffects = 20;

    private readonly Queue<ParticleSystem> hitPool = new Queue<ParticleSystem>();
    private readonly List<ParticleSystem> allHitEffects = new List<ParticleSystem>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<VFXManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("VFXManager");
        managerObject.AddComponent<VFXManager>();
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
        WarmHitPool();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<ShadowSummonedEvent>(OnShadowSummoned);
        EventBus.Subscribe<PlayerDodgedEvent>(OnPlayerDodged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<ShadowSummonedEvent>(OnShadowSummoned);
        EventBus.Unsubscribe<PlayerDodgedEvent>(OnPlayerDodged);
    }

    public void PlayHitEffect(Vector2 position, Vector2 hitDirection)
    {
        ParticleSystem particleSystem = GetHitEffect();
        if (particleSystem == null)
        {
            return;
        }

        Transform particleTransform = particleSystem.transform;
        particleTransform.position = position;
        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        particleTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        particleSystem.gameObject.SetActive(true);
        particleSystem.Play(true);
        StartCoroutine(ReturnHitEffectRoutine(particleSystem, 0.35f));
    }

    public void PlayEnemyDeath(Vector2 position, Color color, int expValue)
    {
        ParticleSystem burst = CreateOneShotBurst("EnemyDeathVFX", position, color, 20, 0.45f, 2.8f, 0.18f, ParticleSystemShapeType.Circle, 0.3f);
        if (burst != null)
        {
            Destroy(burst.gameObject, 1.25f);
        }

        FloatingTextPopup.Spawn("+" + expValue + " EXP", new Color(0.45f, 0.85f, 1f, 1f), position + Vector2.up * 0.65f);
    }

    public void PlayLevelUp(Vector3 position)
    {
        StartCoroutine(LevelUpRoutine(position));
    }

    public void PlayShadowExtraction(Vector3 startPosition, Transform target, float duration)
    {
        StartCoroutine(ShadowExtractionRoutine(startPosition, target, duration));
    }

    public void PlayShadowSummon(Vector3 position)
    {
        ParticleSystem burst = CreateOneShotBurst("ShadowSummonVFX", position, new Color(0.32f, 0.12f, 0.48f, 1f), 40, 0.38f, 3.6f, 0.16f, ParticleSystemShapeType.Circle, 0.4f);
        if (burst != null)
        {
            Destroy(burst.gameObject, 1.2f);
        }

        StartCoroutine(FlashPointLightRoutine(position, new Color(0.56f, 0.24f, 1f, 1f), 2.1f, 2.4f, 0.3f));
    }

    public void PlayDodgeDust(Vector3 position, Vector2 dodgeDirection)
    {
        ParticleSystem dust = CreateOneShotBurst("DodgeDustVFX", position, new Color(0.32f, 0.24f, 0.18f, 1f), 6, 0.3f, 1.8f, 0.18f, ParticleSystemShapeType.Cone, 0.15f);
        if (dust == null)
        {
            return;
        }

        dust.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-dodgeDirection.y, -dodgeDirection.x) * Mathf.Rad2Deg);
        Destroy(dust.gameObject, 0.8f);
    }

    public void PlayExplosion(Vector3 position)
    {
        ParticleSystem fireBurst = CreateOneShotBurst("ExplosionFireVFX", position, new Color(1f, 0.42f, 0.1f, 1f), 30, 0.45f, 4f, 0.2f, ParticleSystemShapeType.Circle, 0.2f);
        if (fireBurst != null)
        {
            Destroy(fireBurst.gameObject, 1.2f);
        }

        ParticleSystem smokeBurst = CreateOneShotBurst("ExplosionSmokeVFX", position, new Color(0.2f, 0.2f, 0.2f, 0.9f), 5, 0.9f, 1.2f, 0.55f, ParticleSystemShapeType.Circle, 0.1f);
        if (smokeBurst != null)
        {
            Destroy(smokeBurst.gameObject, 1.6f);
        }

        StartCoroutine(ShockwaveRoutine(position));
    }

    private void OnPlayerLevelUp(PlayerLevelUpEvent levelUpEvent)
    {
        if (levelUpEvent == null)
        {
            return;
        }

        KaisenController player = FindFirstObjectByType<KaisenController>();
        if (player != null)
        {
            PlayLevelUp(player.transform.position);
        }
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        if (enemyDiedEvent == null)
        {
            return;
        }

        EnemyBase enemy = enemyDiedEvent.enemy != null ? enemyDiedEvent.enemy.GetComponent<EnemyBase>() : null;
        Color enemyColor = Color.white;
        if (enemy != null && enemy.GetComponent<SpriteRenderer>() != null)
        {
            enemyColor = enemy.GetComponent<SpriteRenderer>().color;
        }

        PlayEnemyDeath(enemyDiedEvent.position, enemyColor, enemyDiedEvent.expValue);
    }

    private void OnShadowSummoned(ShadowSummonedEvent shadowSummonedEvent)
    {
        if (shadowSummonedEvent == null || shadowSummonedEvent.soldier == null)
        {
            return;
        }

        PlayShadowSummon(shadowSummonedEvent.soldier.transform.position);
    }

    private void OnPlayerDodged(PlayerDodgedEvent dodgeEvent)
    {
        if (dodgeEvent == null)
        {
            return;
        }

        PlayDodgeDust(dodgeEvent.position, dodgeEvent.direction);
    }

    private void WarmHitPool()
    {
        while (allHitEffects.Count < pooledHitEffects)
        {
            ParticleSystem particleSystem = CreateHitParticleSystem();
            particleSystem.gameObject.SetActive(false);
            allHitEffects.Add(particleSystem);
            hitPool.Enqueue(particleSystem);
        }
    }

    private ParticleSystem GetHitEffect()
    {
        if (hitPool.Count == 0)
        {
            ParticleSystem extra = CreateHitParticleSystem();
            allHitEffects.Add(extra);
            return extra;
        }

        return hitPool.Dequeue();
    }

    private ParticleSystem CreateHitParticleSystem()
    {
        GameObject effectObject = new GameObject("HitEffect");
        effectObject.transform.SetParent(transform, false);
        ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = 0.2f;
        main.startSpeed = 2.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startColor = Color.white;
        main.maxParticles = 16;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)UnityEngine.Random.Range(8, 13)) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.06f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "VFX";
        renderer.sortingOrder = 15;
        return particleSystem;
    }

    private IEnumerator ReturnHitEffectRoutine(ParticleSystem particleSystem, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (particleSystem == null)
        {
            yield break;
        }

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystem.gameObject.SetActive(false);
        hitPool.Enqueue(particleSystem);
    }

    private ParticleSystem CreateOneShotBurst(string objectName, Vector3 position, Color color, int count, float lifetime, float speed, float size, ParticleSystemShapeType shapeType, float radius)
    {
        GameObject effectObject = new GameObject(objectName);
        effectObject.transform.position = position;
        ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = lifetime;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = shapeType;
        shape.radius = radius;
        shape.angle = 18f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.75f, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "VFX";
        renderer.sortingOrder = 18;

        particleSystem.Play(true);
        return particleSystem;
    }

    private IEnumerator LevelUpRoutine(Vector3 position)
    {
        ParticleSystem burst = CreateOneShotBurst("LevelUpBurst", position, new Color(0.2f, 0.75f, 1f, 1f), 30, 0.5f, 3.2f, 0.14f, ParticleSystemShapeType.Circle, 0.1f);
        if (burst != null)
        {
            Destroy(burst.gameObject, 1.3f);
        }

        yield return FlashPointLightRoutine(position, new Color(0.2f, 0.75f, 1f, 1f), 3f, 4f, 0.5f);
    }

    private IEnumerator ShadowExtractionRoutine(Vector3 startPosition, Transform target, float duration)
    {
        GameObject effectObject = new GameObject("ShadowExtractionTrail");
        effectObject.transform.position = startPosition;
        ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = 0.45f;
        main.startSpeed = 0.5f;
        main.startSize = 0.12f;
        main.startColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;

        ParticleSystem.NoiseModule noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.4f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0.85f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.4f);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "VFX";
        renderer.sortingOrder = 16;

        particleSystem.Play(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 targetPosition = target != null ? target.position : startPosition;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            Vector3 spiralOffset = new Vector3(Mathf.Cos(t * 12f) * 0.25f, Mathf.Sin(t * 14f) * 0.18f, 0f);
            effectObject.transform.position = Vector3.Lerp(startPosition, targetPosition, t) + spiralOffset;
            yield return null;
        }

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(effectObject, 1.2f);
    }

    private IEnumerator FlashPointLightRoutine(Vector3 position, Color color, float targetIntensity, float targetRadius, float duration)
    {
        GameObject lightObject = new GameObject("TransientVFXLight");
        lightObject.transform.position = position;
        Light2D light2D = lightObject.AddComponent<Light2D>();
        light2D.lightType = Light2D.LightType.Point;
        light2D.color = color;
        light2D.intensity = 0f;
        light2D.pointLightOuterRadius = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = t < 0.5f ? Mathf.InverseLerp(0f, 0.5f, t) : Mathf.InverseLerp(1f, 0.5f, t);
            light2D.intensity = Mathf.Lerp(0f, targetIntensity, pulse);
            light2D.pointLightOuterRadius = Mathf.Lerp(0f, targetRadius, t);
            yield return null;
        }

        Destroy(lightObject);
    }

    private IEnumerator ShockwaveRoutine(Vector3 position)
    {
        GameObject waveObject = new GameObject("ExplosionShockwave");
        waveObject.transform.position = position + Vector3.back * 0.1f;
        LineRenderer lineRenderer = waveObject.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.positionCount = 32;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 0.7f, 0.4f, 0.9f);
        lineRenderer.endColor = new Color(1f, 0.7f, 0.4f, 0.1f);
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.sortingLayerName = "VFX";
        lineRenderer.sortingOrder = 10;

        float elapsed = 0f;
        const float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float radius = Mathf.Lerp(0.25f, 2.8f, t);
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                float angle = (i / (float)lineRenderer.positionCount) * Mathf.PI * 2f;
                lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            Color color = new Color(1f, 0.7f, 0.4f, 1f - t);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            yield return null;
        }

        Destroy(waveObject);
    }
}
