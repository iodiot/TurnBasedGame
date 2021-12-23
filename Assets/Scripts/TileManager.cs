using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class TileManager : MonoBehaviour
{
    public GameObject[] floorPrefabs;

    [Range(5, 20)]
    public int mapWidth = 10;

    [Range(5, 20)]
    public int mapHeight = 20;

    public bool generateTileGaps = false;

    private Dictionary<(int, int), Tile> tileMap;

    private GameObject tileMapGameObject;

    private GameManager gameManager;

    public void SetGameManager(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public List<Tile> GetAllTiles()
    {
        return tileMap.Values.ToList();
    }

    public Tile GetTile(int x, int y)
    {
        return tileMap.ContainsKey((x, y)) ? tileMap[(x, y)] : null;
    }

    public void CreateTileMap()
    {
        tileMapGameObject = new GameObject("TileMap");

        tileMap = new Dictionary<(int, int), Tile>();

        for (var y = 0; y < mapHeight; ++y)
        {
            for (var x = 0; x < mapWidth; ++x)
            {
                if (generateTileGaps && Random.Range(0, 20) == 0)
                {
                    continue;
                }

                // Choose random prefab
                var toInstantiate = floorPrefabs[Random.Range(0, floorPrefabs.Length)];

                var instance = Instantiate(toInstantiate, gameManager.ToWorld(x, y), Quaternion.identity) as GameObject;

                var tile = instance.GetComponent<Tile>();

                tile.SetGameManager(gameManager);

                tile.X = x;
                tile.Y = y;

                tileMap[(x, y)] = tile;

                instance.transform.SetParent(tileMapGameObject.transform);
            }
        }
    }

    public void DestroyTileMap()
    {
        if (tileMapGameObject != null)
        {
            Destroy(tileMapGameObject);
        }

        tileMap = null;
    }


}
