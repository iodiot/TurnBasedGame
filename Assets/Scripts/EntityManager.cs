using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class EntityManager : MonoBehaviour
{
    [Range(1, 5)]
    public int alliesInitialCount;

    [Range(1, 5)]
    public int enemiesInitialCount;

    public GameObject allyPrefab;
    public GameObject enemyPrefab;
    
    public int AlliesCount { get { return entities.OfType<AllyEntity>().Count(); } }
    public int EnemiesCount { get { return entities.OfType<EnemyEntity>().Count(); } }

    private GameObject entitiesGameObject;

    private List<Entity> entities;

    private GameManager gameManager;

    public void SetGameManager(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void CreateEntities()
    {
        entitiesGameObject = new GameObject("Entities");

        entities = new List<Entity>();

        CreateEntities<AllyEntity>(allyPrefab, alliesInitialCount, leftSide: true, "Ally");
        CreateEntities<EnemyEntity>(enemyPrefab, enemiesInitialCount, leftSide: false, "Enemy");
    }

    public void DestroyEntities()
    {
        entities = null;

        Destroy(entitiesGameObject);
    }

    private void CreateEntities<T>(GameObject entityPrefab, int count, bool leftSide, string namePrefix)
    {
        for (var i = 0; i < count; ++i)
        {
            var (x, y) = GetRandomPosition(leftSide: leftSide);

            var instance = Instantiate(entityPrefab, gameManager.ToWorld(x, y), Quaternion.identity) as GameObject;

            var entity = instance.GetComponent<T>() as Entity;

            entity.SetGameManager(gameManager);
            entity.Name = $"{namePrefix} #{i + 1}";

            entities.Add(entity);

            MoveEntityTo(entity, x, y);

            instance.transform.SetParent(entitiesGameObject.transform);
        }
    }

    /// <summary>
    /// Actually moves an entity to the specific tile. The tile should exists and be vacant. 
    /// </summary>
    public void MoveEntityTo(Entity entity, int toX, int toY)
    {
        gameManager.TileManager.GetTile(entity.X, entity.Y).Entity = null;

        gameManager.TileManager.GetTile(toX, toY).Entity = entity;

        entity.X = toX;
        entity.Y = toY;

        var pos = gameManager.ToWorld(toX, toY);

        entity.transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    /// <summary>
    /// Returns random position on the battlefield. 
    /// </summary>
    /// <param name="leftSide">
    /// Defines the half of the battlefield. 
    /// </param>
    private (int x, int y) GetRandomPosition(bool leftSide = true)
    {
        int x, y;

        while (true)
        {
            x = Random.Range(0, gameManager.TileManager.mapWidth / 2) + (leftSide ? 0 : gameManager.TileManager.mapWidth / 2);
            y = Random.Range(0, gameManager.TileManager.mapHeight);

            var cell = gameManager.TileManager.GetTile(x, y);

            if (cell != null && cell.Entity == null)
            {
                break;
            }
        }

        return (x, y);
    }

    public void KillEntity(Entity entity)
    {
        Debug.Assert(entities.Contains(entity), "Cannot kill missing entity");

        entity.Kill();

        entities.Remove(entity);

        gameManager.TileManager.GetTile(entity.X, entity.Y).Entity = null;
    }

    public List<AllyEntity> GetAllies()
    {
        return entities.OfType<AllyEntity>().ToList();
    }

    public List<EnemyEntity> GetEnemies()
    {
        return entities.OfType<EnemyEntity>().ToList();
    }
}
