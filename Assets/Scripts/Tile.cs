using UnityEngine;

public sealed class Tile : MonoBehaviour
{
    public int X { get; set; }
    public int Y { get; set; }

    public Entity Entity { get; set; }

    private SpriteRenderer spriteRenderer;

    private Color tint = Color.white;

    private GameManager gameManager;

    public void SetGameManager(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        HighlightTileIfMouseAbove();
    }

    public void SetTint(Color tint)
    {
        this.tint = tint;
    }

    /// <summary>
    /// Defines whether the entiy is in range of this entity. 
    /// </summary>
    /// <remarks>
    /// Uses Manhattan distance as a metric.
    /// </remarks>
    /// <param name="range">
    /// Provide moveRange or attackRange depending on the situation.
    /// </param>
    public bool InRangeOf(Entity entity, int range)
    {
        return Mathf.Abs(X - entity.X) + Mathf.Abs(Y - entity.Y) <= range;
    }

    private void HighlightTileIfMouseAbove()
    {
        if (!gameManager.ShowingEndGameScreen)
        {
            var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var tileX = (int)Mathf.Floor(gameManager.ToScreen(worldPos).x);
            var tileY = (int)Mathf.Floor(gameManager.ToScreen(worldPos).y);

            var above = (tileX == X) && (tileY == Y);

            spriteRenderer.color = above ? Color.grey : tint;
        }
    }
}
