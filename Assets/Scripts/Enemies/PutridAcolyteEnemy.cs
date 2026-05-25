using UnityEngine;

public class PutridAcolyteEnemy : EnemyBase
{
    private const string SheetResourcePath = "Enemies/PutridAcolyte/PutridAcolyteSheet";
    private const int SheetColumns = 4;
    private const int SheetRows = 4;
    private const float PixelsPerUnit = 160f;

    [SerializeField] private float optimalDistance = 4.7f;
    [SerializeField] private float projectileSpeed = 7.2f;
    [SerializeField] private float projectileRange = 8.5f;
    [SerializeField] private float projectileSpawnOffset = 0.68f;

    private static Sprite[][] cachedAnimationRows;
    private EnemyState visualState = (EnemyState)(-1);
    private int visualFrame;
    private float visualTimer;

    protected override void Awake()
    {
        base.Awake();
        ConfigureRuntimeVisuals();
    }

    protected override void Update()
    {
        base.Update();
        UpdateSheetAnimation();
    }

    protected override Vector2 GetChaseDirection(Vector2 toPlayer, float distanceToPlayer)
    {
        if (toPlayer.sqrMagnitude > 0.001f && distanceToPlayer < optimalDistance)
        {
            return -toPlayer.normalized;
        }

        return base.GetChaseDirection(toPlayer, distanceToPlayer);
    }

    protected override void PerformAttack()
    {
        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = movementDirection.sqrMagnitude > 0.001f ? movementDirection.normalized : Vector2.right;
        }

        Vector2 origin = (Vector2)transform.position + direction.normalized * projectileSpawnOffset;
        PutridAcolyteBolt.Spawn(origin, direction, Data != null ? Data.damage : 4f, projectileSpeed, projectileRange);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.EnemyProjectile, origin, 0.55f, 0.92f, true, 0.8f);
        }
    }

    private void ConfigureRuntimeVisuals()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sortingLayerName = "Characters";
        spriteRenderer.sortingOrder = 2;
        spriteRenderer.color = Color.white;

        Sprite[] idleFrames = GetFramesForState(EnemyState.Idle);
        if (idleFrames != null && idleFrames.Length > 0)
        {
            spriteRenderer.sprite = idleFrames[0];
        }
    }

    private void UpdateSheetAnimation()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite[] frames = GetFramesForState(CurrentState);
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        if (visualState != CurrentState)
        {
            visualState = CurrentState;
            visualFrame = 0;
            visualTimer = 0f;
        }

        float frameDuration = 1f / GetFrameRate(CurrentState);
        visualTimer += Time.deltaTime;
        while (visualTimer >= frameDuration)
        {
            visualTimer -= frameDuration;
            if (CurrentState == EnemyState.Dead)
            {
                visualFrame = Mathf.Min(visualFrame + 1, frames.Length - 1);
            }
            else
            {
                visualFrame = (visualFrame + 1) % frames.Length;
            }
        }

        spriteRenderer.sprite = frames[Mathf.Clamp(visualFrame, 0, frames.Length - 1)];
    }

    private static float GetFrameRate(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Attack:
                return 11f;
            case EnemyState.Patrol:
            case EnemyState.Chase:
            case EnemyState.Stagger:
                return 8f;
            case EnemyState.Dead:
                return 7f;
            default:
                return 5f;
        }
    }

    private static Sprite[] GetFramesForState(EnemyState state)
    {
        Sprite[][] rows = LoadAnimationRows();
        if (rows == null || rows.Length == 0)
        {
            return null;
        }

        switch (state)
        {
            case EnemyState.Patrol:
            case EnemyState.Chase:
            case EnemyState.Stagger:
                return rows.Length > 1 ? rows[1] : rows[0];
            case EnemyState.Attack:
                return rows.Length > 2 ? rows[2] : rows[0];
            case EnemyState.Dead:
                return rows.Length > 3 ? rows[3] : rows[0];
            default:
                return rows[0];
        }
    }

    private static Sprite[][] LoadAnimationRows()
    {
        if (cachedAnimationRows != null)
        {
            return cachedAnimationRows;
        }

        Texture2D sheet = Resources.Load<Texture2D>(SheetResourcePath);
        if (sheet == null)
        {
            return null;
        }

        sheet.filterMode = FilterMode.Point;
        sheet.wrapMode = TextureWrapMode.Clamp;

        int frameWidth = sheet.width / SheetColumns;
        int frameHeight = sheet.height / SheetRows;
        cachedAnimationRows = new Sprite[SheetRows][];
        for (int row = 0; row < SheetRows; row++)
        {
            cachedAnimationRows[row] = new Sprite[SheetColumns];
            for (int column = 0; column < SheetColumns; column++)
            {
                Rect rect = new Rect(column * frameWidth, sheet.height - ((row + 1) * frameHeight), frameWidth, frameHeight);
                Sprite sprite = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.42f), PixelsPerUnit);
                sprite.name = "PutridAcolyte_" + row + "_" + column;
                cachedAnimationRows[row][column] = sprite;
            }
        }

        return cachedAnimationRows;
    }
}
