using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoverObject : MonoBehaviour
{
    [SerializeField] protected float maxHP = 50f;
    [SerializeField] protected float currentHP;
    [SerializeField] protected bool isDestructible = true;
    [SerializeField] protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        currentHP = maxHP;
    }

    public virtual void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentHP -= amount;
        if (isDestructible && rb != null && hitDirection.sqrMagnitude > 0.001f)
        {
            rb.MovePosition(rb.position + (hitDirection.normalized * 0.5f));
            rb.AddForce(hitDirection.normalized * 0.5f, ForceMode2D.Impulse);
        }

        if (currentHP <= 0f)
        {
            HandleDestroyed();
        }
    }

    protected virtual void HandleDestroyed()
    {
        SpawnDebris();
        Destroy(gameObject);
    }

    protected virtual void SpawnDebris()
    {
        GameObject debris = new GameObject("CoverDebris");
        debris.transform.position = transform.position;
        ParticleSystem particles = debris.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 0.35f;
        main.startSpeed = 2f;
        main.startSize = 0.08f;
        main.maxParticles = 18;
        main.loop = false;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        particles.Play();
        Destroy(debris, 1f);
    }
}

