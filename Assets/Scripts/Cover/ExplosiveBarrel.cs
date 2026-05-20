using UnityEngine;

public class ExplosiveBarrel : CoverObject
{
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 25f;

    protected override void HandleDestroyed()
    {
        Explode();
        base.HandleDestroyed();
    }

    private void Explode()
    {
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayExplosion(transform.position);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.GiantImpact, transform.position, 0.95f, 0.92f, true, 0.95f);
        }

        GameObject explosion = new GameObject("BarrelExplosion");
        explosion.transform.position = transform.position;
        ParticleSystem particles = explosion.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 0.45f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.55f, 0.15f, 1f);
        main.maxParticles = 24;
        main.loop = false;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });
        particles.Play();
        Destroy(explosion, 1f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            Vector2 direction = ((Vector2)hits[i].transform.position - (Vector2)transform.position).normalized;
            PlayerHealth player = hits[i].GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(explosionDamage);
            }

            EnemyBase enemy = hits[i].GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(explosionDamage, direction);
            }
        }
    }
}
