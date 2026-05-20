using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private KaisenController controller;
    [SerializeField] private float currentHP;
    [SerializeField] private float damageInvulnerabilityDuration = 0.5f;
    [SerializeField] private float maxHealthMultiplier = 1f;
    [SerializeField] private bool godMode;

    private bool isInvulnerable;
    private bool isDead;

    public float CurrentHP { get { return currentHP; } }
    public float MaxHP { get { return controller != null ? controller.Stats.MaxHP * maxHealthMultiplier : 0f; } }
    public bool IsInvulnerable { get { return isInvulnerable; } }
    public bool GodMode { get { return godMode; } }

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<KaisenController>();
        }

        currentHP = MaxHP;
    }

    private void OnEnable()
    {
        isDead = false;
        isInvulnerable = false;
    }

    public void TakeDamage(float amount)
    {
        if (!isActiveAndEnabled || isDead || isInvulnerable || godMode || amount <= 0f)
        {
            return;
        }

        currentHP = Mathf.Max(0f, currentHP - amount);
        EventBus.Publish(new PlayerDamagedEvent(amount, currentHP));

        if (currentHP <= 0f)
        {
            isDead = true;
            EventBus.Publish(new PlayerDeathEvent());
            StartCoroutine(DeathRoutine());
            return;
        }

        if (controller != null)
        {
            controller.PlayHitAnimation();
        }

        StartCoroutine(InvulnerabilityRoutine());
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentHP = Mathf.Min(MaxHP, currentHP + amount);
    }

    public void SetMaxHealthMultiplier(float multiplier, bool clampCurrentHealth)
    {
        maxHealthMultiplier = Mathf.Max(0.1f, multiplier);
        if (clampCurrentHealth)
        {
            currentHP = Mathf.Min(currentHP, MaxHP);
        }
    }

    public void RestoreToMaxHealth()
    {
        currentHP = MaxHP;
    }

    public void SetGodMode(bool enabled)
    {
        godMode = enabled;
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(damageInvulnerabilityDuration);
        isInvulnerable = false;
    }

    private IEnumerator DeathRoutine()
    {
        isInvulnerable = true;
        float delay = controller != null ? controller.PlayDeathAnimation() : 0.35f;
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    private void Reset()
    {
        controller = GetComponent<KaisenController>();
    }
}
