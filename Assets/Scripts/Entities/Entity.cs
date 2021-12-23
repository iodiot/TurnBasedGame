using System.Collections;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public GameObject healthBarPrefab;

    [Range(1, 10)]
    public int health;
    [Range(1, 10)]
    public int damage;
    [Range(1, 4)]
    public int moveRange;
    [Range(1, 4)]
    public int attackRange;

    public string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    private bool IsSelected { get { return gameManager.SelectedEntity == this; } }

    private float initialHeath;

    private SpriteRenderer spriteRenderer;

    private GameObject healthBarObject;

    protected GameManager gameManager;

    public void SetGameManager(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        CreateHealthBarAboveEntity();
    }

    protected virtual void Update()
    {
        var pos = transform.localPosition;

        // Highlight selected entity 
        transform.localPosition = new Vector2(pos.x, pos.y + (IsSelected ? 0.005f * Mathf.Sin(20f * (float)Time.fixedTimeAsDouble) : 0f));

        UpdateHealthBar();
    }

    private void CreateHealthBarAboveEntity()
    {
        var pos = gameManager.ToWorld(X, Y);
        pos.y += 0.5f;

        healthBarObject = Instantiate(healthBarPrefab, pos, Quaternion.identity) as GameObject;
        healthBarObject.transform.SetParent(transform);

        initialHeath = health;
    }

    private void UpdateHealthBar()
    {
        var script = healthBarObject.GetComponent<HealthBar>() as HealthBar;

        script.SetHealth((float)health / (float)initialHeath);
    }

    /// <summary>
    /// Takes damage from the specified entity.
    /// </summary>
    public void TakeDamage(Entity from)
    {
        health -= from.damage;

        StartCoroutine(FlashAfterDamage());
    }

    private IEnumerator FlashAfterDamage()
    {
        for (var i = 0; i < 10; ++i)
        {
            yield return new WaitForSeconds(.05f);
            spriteRenderer.color = i % 2 == 0 ? Color.red : Color.white;
        }
    }

    public void Kill()
    {
        Destroy(gameObject);
    }
}
