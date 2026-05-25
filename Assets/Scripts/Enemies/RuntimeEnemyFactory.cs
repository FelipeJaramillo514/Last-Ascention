using UnityEngine;

public static class RuntimeEnemyFactory
{
    public static GameObject CreateEnemy(EnemyData enemyData, Vector2 position)
    {
        if (enemyData == null)
        {
            return null;
        }

        GameObject enemyObject = new GameObject("Enemy_" + enemyData.enemyType);
        enemyObject.transform.position = position;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            enemyObject.layer = enemyLayer;
        }

        SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Characters";
        spriteRenderer.sortingOrder = 2;

        Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D collider = enemyObject.AddComponent<CapsuleCollider2D>();
        collider.size = enemyData.enemyType == EnemyType.PutridAcolyte ? new Vector2(0.74f, 1.12f) : new Vector2(0.8f, 1f);
        collider.offset = new Vector2(0f, 0.06f);

        enemyObject.AddComponent<Animator>();
        AddEnemyBrain(enemyObject, enemyData);
        return enemyObject;
    }

    private static void AddEnemyBrain(GameObject enemyObject, EnemyData enemyData)
    {
        switch (enemyData.enemyType)
        {
            case EnemyType.PutridAcolyte:
                enemyObject.AddComponent<PutridAcolyteEnemy>();
                break;
            case EnemyType.BeastMinor:
                enemyObject.AddComponent<BeastMinorEnemy>();
                break;
            default:
                enemyObject.AddComponent<GoblinEnemy>();
                break;
        }
    }
}
