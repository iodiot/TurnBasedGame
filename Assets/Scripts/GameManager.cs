using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    private enum PlayerAction
    {
        Idle,
        SelectedEntity,
        PressedMoveButton,
        PressedAttackButton
    }

    public TileManager TileManager { get; private set; }
    public EntityManager EntityManager { get; private set; }
    public Entity SelectedEntity { get; private set; }

    [Range(1, 10)]
    public int movesPerTurn;

    public Text statsText;
    public Text actionText;
    public Text movesText;
    public Text winText;
    public Button moveButton;
    public Button attackButton;
    public Button endTurnButton;

    public GameObject inGameUI;
    public GameObject endGameUI;

    public bool ShowingEndGameScreen { get; set; }

    private InputController inputController;

    private bool buttonWasClicked;

    private PlayerAction currentAction;

    private int movesLeft;

    private bool enemyTurn;

    private void Awake()
    {
        TileManager = GetComponent<TileManager>();
        EntityManager = GetComponent<EntityManager>();
        inputController = GetComponent<InputController>();

        TileManager.SetGameManager(this);
        EntityManager.SetGameManager(this);
    }

    void Start()
    {
        inputController.OnSingleTap += InputController_OnSingleTap;

        ResetGame();
    }

    void Update()
    {
        movesText.text = $"Moves left: {movesLeft}";
    }

    private void SetPlayerAction(PlayerAction newAction)
    {
        currentAction = newAction;
    }

    private void SetStateText(string text)
    {
        actionText.text = text;
    }

    private void ResetMoves()
    {
        movesLeft = movesPerTurn;   
    }

    private void ResetGame()
    {
        TileManager.DestroyTileMap();
        TileManager.CreateTileMap();

        EntityManager.DestroyEntities();
        EntityManager.CreateEntities();

        ResetMoves();
        OnEntitySelect(null);

        inGameUI.SetActive(true);
        endGameUI.SetActive(false);

        ShowingEndGameScreen = false;
    }

    #region Enemy logic
    /// <summary>
    /// Returns best move vector for the specified enemy entity.
    /// </summary>
    /// <returns>
    /// (n, 0) or (0, n) if there is a move available.
    /// (0, 0) if there is no available move.
    /// </returns>
    private (int dx, int dy) FindBestMoveVectorFor(EnemyEntity enemy)
    {
        int dx = 0;
        int dy = 0;

        int minDist = 1000;
        AllyEntity nearestAlly = null;

        // Find the nearest ally
        foreach (var ally in EntityManager.GetAllies())
        {
            int dist = Mathf.Abs(ally.X - enemy.X) + Mathf.Abs(ally.Y - enemy.Y);

            if (dist < minDist)
            {
                nearestAlly = ally;
            }
        }

        if (nearestAlly == null)
        {
            return (0, 0);
        }

        dx = (int)Mathf.Sign(nearestAlly.X - enemy.X);
        dy = (int)Mathf.Sign(nearestAlly.Y - enemy.Y);

        var n = enemy.moveRange;

        // First try the longest moves along the axis 
        while (n > 0)
        {
            int xSteps = 0, ySteps = 0;

            // Randomize movements by axis 
            if (Random.Range(0, 2) == 0)
            {
                if (dx != 0)
                {
                    xSteps = dx * n;
                }
                else if (dy != 0)
                {
                    ySteps = dy * n;
                }
            }
            else
            {
                if (dy != 0)
                {
                    ySteps = dy * n;
                }
                else if (dx != 0)
                {
                    xSteps = dx * n;
                }
            }

            var tile = TileManager.GetTile(enemy.X + xSteps, enemy.Y + ySteps);

            if (tile != null && tile.Entity == null)
            {
                return (xSteps, ySteps);
            }

            --n;
        }

        return (0, 0);
    }

    private IEnumerator MoveEnemies()
    {
        enemyTurn = true;
        endTurnButton.interactable = false;

        ResetMoves();

        while (movesLeft > 0 && EntityManager.AlliesCount > 0)
        {
            SetStateText("Enemy thinks");

            HideEntityRange();

            // Wait to show that the enemy thinks 
            yield return new WaitForSeconds(1f);

            var enemies = EntityManager.GetEnemies();
            var allies = EntityManager.GetAllies();

            var enemy = enemies[Random.Range(0, enemies.Count)];

            OnEntitySelect(enemy);

            AllyEntity allyToAttack = null;

            foreach (var ally in allies)
            {
                var tile = TileManager.GetTile(ally.X, ally.Y);
                if (tile.InRangeOf(enemy, enemy.attackRange))
                {
                    allyToAttack = ally;
                    break;
                }
            }

            var attackMode = allyToAttack != null;

            ShowEntityRange(enemy, attackMode ? Color.red : Color.green, attackMode ? enemy.attackRange : enemy.moveRange);

            yield return new WaitForSeconds(1f);

            // Enemy attacks if there is an ally within attack range
            if (attackMode)
            {
                SetStateText("Enemy attacks");

                allyToAttack.TakeDamage(from: enemy);

                if (allyToAttack.health <= 0)
                {
                    EntityManager.KillEntity(allyToAttack);
                }
            }
            // Enemy moves otherwise
            else
            {
                SetStateText("Enemy moves");

                var (dx, dy) = FindBestMoveVectorFor(enemy);


                if (!(dx == 0 && dy == 0))
                {
                    EntityManager.MoveEntityTo(enemy, enemy.X + dx, enemy.Y +  dy);
                }
            }

            yield return new WaitForSeconds(1f);

            movesLeft -= 1;
        }

        enemyTurn = false;

        OnEntitySelect(null);

        ResetMoves();

        endTurnButton.interactable = true;

        buttonWasClicked = false;

        if (EntityManager.AlliesCount == 0)
        {
            OnEndGame();
        }
    }
    #endregion

    #region Game events
    private void OnEntitySelect(Entity entity)
    {
        HideEntityRange();

        ShowEntityStats(entity);

        moveButton.interactable = entity is AllyEntity && movesLeft > 0;
        attackButton.interactable = entity is AllyEntity && movesLeft > 0;
        endTurnButton.interactable = true && !enemyTurn;

        SelectedEntity = entity;

        SetPlayerAction(entity == null ? PlayerAction.Idle : PlayerAction.SelectedEntity);

        if (enemyTurn)
        {
            SetStateText("Enemy thinks");
        }
        else
        {
            SetStateText(entity == null ? "Select entity" : "Select action");
        }
    }

    private void OnTileSelect(Tile tile)
    {
        HideEntityRange();

        switch (currentAction)
        {
            case PlayerAction.Idle:
            case PlayerAction.SelectedEntity:
                if (tile != null && tile.Entity != null)
                {
                    OnEntitySelect(tile.Entity);
                    SetPlayerAction(PlayerAction.SelectedEntity);
                }
                else
                {
                    OnEntitySelect(null);
                    SetPlayerAction(PlayerAction.Idle);
                }
                break;
            case PlayerAction.PressedMoveButton:

                if (tile != null && tile.Entity == null && tile.InRangeOf(SelectedEntity, SelectedEntity.moveRange))
                {
                    EntityManager.MoveEntityTo(SelectedEntity, tile.X, tile.Y);
                    movesLeft -= 1;
                }

                OnEntitySelect(SelectedEntity);

                break;
            case PlayerAction.PressedAttackButton:
                if ((tile != null) && (tile.Entity is EnemyEntity) && tile.InRangeOf(SelectedEntity, SelectedEntity.attackRange))
                {
                    tile.Entity.TakeDamage(SelectedEntity);
                    movesLeft -= 1;

                    if (tile.Entity.health <= 0)
                    {
                        EntityManager.KillEntity(tile.Entity);
                    }
                }

                OnEntitySelect(SelectedEntity);

                if (EntityManager.EnemiesCount == 0)
                {
                    OnEndGame();
                }

                break;
            default:
                break;
        }
    }

    private void OnEndGame()
    {
        if (EntityManager.AlliesCount > 0)
        {
            winText.text = "Allies won the game";
        }
        else
        {
            winText.text = "Enemies won the game";
        }

        inGameUI.SetActive(false);
        endGameUI.SetActive(true);

        ShowingEndGameScreen = true;
    }
    #endregion

    #region Camera
    public Vector2 ToWorld(int x, int y)
    {
        return new Vector2(x - TileManager.mapWidth / 2 + .5f, y - TileManager.mapHeight / 2 + .5f);
    }

    public Vector2 ToScreen(Vector2 position)
    {
        return new Vector2(position.x + TileManager.mapWidth / 2, position.y + TileManager.mapHeight / 2);
    }
    #endregion

    #region Button handlers
    public void MoveButton_OnClick()
    {
        buttonWasClicked = true;

        HideEntityRange();
        ShowEntityRange(SelectedEntity, Color.green, SelectedEntity.moveRange);

        SetStateText("Select tile to move in");

        currentAction = PlayerAction.PressedMoveButton;
    }

    public void AttackButton_OnClick()
    {
        buttonWasClicked = true;

        HideEntityRange();
        ShowEntityRange(SelectedEntity, Color.red, SelectedEntity.attackRange);

        SetStateText("Select enemy to attack");

        currentAction = PlayerAction.PressedAttackButton;
    }

    public void EndTurnButton_OnClick()
    {
        buttonWasClicked = true;

        OnEntitySelect(null);

        StartCoroutine(MoveEnemies());
    }

    public void RestartButton_OnClick()
    {
        buttonWasClicked = true;

        ResetGame();
    }
    #endregion

    private void InputController_OnSingleTap()
    {
        if (enemyTurn)
        {
            return;
        }

        if (buttonWasClicked)
        {
            buttonWasClicked = false;
            return;
        }

        var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        var tileX = (int)ToScreen(worldPos).x;
        var tileY = (int)ToScreen(worldPos).y;

        var tile = TileManager.GetTile(tileX, tileY);

        OnTileSelect(tile);
    }

    private void ShowEntityStats(Entity entity)
    {
        if (entity == null)
        {
            statsText.text = "Select entity";
        }
        else
        {
            statsText.text = $"Who: {entity.Name}\nHealth: {entity.health}\nDamage: {entity.damage}\nMove range: {entity.moveRange}\nAttack range: {entity.attackRange}";
        }
    }

    /// <summary>
    /// Highlights the tiles on the battlefield to show the possible moves. 
    /// </summary>
    private void ShowEntityRange(Entity entity, Color color, int range)
    {
        foreach (var tile in TileManager.GetAllTiles())
        {
            if (tile.InRangeOf(entity, range))
            {
                tile.SetTint(color);
            }
        }
    }

    /// <summary>
    /// Cleans up the highlight of all the tiles. 
    /// </summary>
    private void HideEntityRange()
    {
        foreach (var tile in TileManager.GetAllTiles())
        {
            tile.SetTint(Color.white);
        }
    }
}
