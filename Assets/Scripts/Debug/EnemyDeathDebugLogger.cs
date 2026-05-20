using UnityEngine;

public class EnemyDeathDebugLogger : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        Debug.Log($"EnemyDiedEvent -> {enemyDiedEvent.enemy.name} | expValue={enemyDiedEvent.expValue} | position={enemyDiedEvent.position}");
    }
}
