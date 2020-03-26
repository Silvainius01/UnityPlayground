using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum TerrainType
{
    DESERT,
    WOOD,
    CLAY,
    WHEAT,
    SHEEP,
    STONE,
    WATER,
    PORT_MISC,
    PORT_wOOD,
    PORT_CLAY,
    PORT_WHEAT,
    PORT_SHEEP,
    PORT_STONE
}

public class HexGrid : MonoBehaviour
{
    public bool generate = false;
    public int layerCount = 0;
    public List<HexTile> allTiles = new List<HexTile>();
    Dictionary<int, List<HexTile>> tileLayerDict = new Dictionary<int, List<HexTile>>();
    List<TerrainType> TerrainDeck = new List<TerrainType>(19)
    {
        TerrainType.DESERT,
        TerrainType.WOOD, TerrainType.WOOD, TerrainType.WOOD, TerrainType.WOOD,
        TerrainType.CLAY, TerrainType.CLAY, TerrainType.CLAY,
        TerrainType.WHEAT, TerrainType.WHEAT, TerrainType.WHEAT, TerrainType.WHEAT,
        TerrainType.SHEEP, TerrainType.SHEEP, TerrainType.SHEEP, TerrainType.SHEEP,
        TerrainType.STONE, TerrainType.STONE, TerrainType.STONE,
        TerrainType.WOOD, TerrainType.WOOD, TerrainType.WOOD, TerrainType.WOOD,
    };

    public void GenerateGridCirc(int numLayers)
    {
        Queue<HexTile> q = new Queue<HexTile>();

        this.layerCount = numLayers;
        allTiles.Add(new HexTile(Vector2.zero, 0, 0));
        q.Enqueue(allTiles[0]);

        // numLayers-1, as this algo will create up to the INDEX, instead of the COUNT.
        for (int i = 0; i < numLayers - 1; ++i)
        {
            int qLimit = q.Count;
            for (int j = 0; j < qLimit; ++j)
            {
                HexTile currTile = q.Dequeue();
                FillTileConnections(currTile, in q);
            }
        }
    }

    public void AddLayer()
    {
        if (layerCount <= 0)
        {
            layerCount = 1;
            allTiles.Add(new HexTile(Vector2.zero, 0, 0));
            return;
        }

        Queue<HexTile> q = new Queue<HexTile>(allTiles.Where(tile => tile.layer == layerCount - 1));

        foreach (var tile in allTiles.Where(tile => tile.layer == layerCount - 1))
        {
            FillTileConnections(tile, in q);
        }
    }

    private void FillTileConnections(HexTile currTile, in Queue<HexTile> q)
    {
        // Create 0th tile if it does not exist
        if (currTile.connections[0] == null)
        {
            HexTile newTile = (currTile.CreateConnectedTile(0, allTiles.Count));
            q.Enqueue(newTile);
            allTiles.Add(newTile);
        }

        // Create and/or connect tiles around currTile
        for (int i = 1; i < currTile.connections.Length; ++i)
        {
            if (currTile.connections[i] == null)
            {
                HexTile newTile = (currTile.CreateConnectedTile(i, allTiles.Count));
                q.Enqueue(newTile);
                allTiles.Add(newTile);
            }

            // Connect to the tile around the parent that is "behind" it
            HexTile.Connect((i + 4) % 6, currTile.connections[i], currTile.connections[i - 1]);
        }

        // Connect first tile to last tile.
        HexTile.Connect(4, currTile.connections[0], currTile.connections[5]);
    }

    private void AddTile(HexTile hexTile)
    {
        if (!tileLayerDict.ContainsKey(hexTile.layer))
            tileLayerDict.Add(hexTile.layer, new List<HexTile>());
        tileLayerDict[hexTile.layer].Add(hexTile);
        allTiles.Add(hexTile);

    }

    public string GetBoardInfo()
    {
        StringBuilder msg = new StringBuilder($"Num Layers: {layerCount}\n");
        StringBuilder tmsg = new StringBuilder();
        Dictionary<int, int> layerDict = new Dictionary<int, int>();

        foreach (var tile in allTiles)
        {
            if (!layerDict.ContainsKey(tile.layer))
                layerDict.Add(tile.layer, 1);
            else ++layerDict[tile.layer];
            tmsg.Append(tile.GetConnectionSummary());
        }

        foreach (var kvp in layerDict)
            msg.Append($"\nLayer {kvp.Key}: {kvp.Value}");

        msg.Append($"\n\nTile Summary:\n");
        msg.Append(tmsg.ToString());
        return msg.ToString();
    }

    public void OnDrawGizmos()
    {
        if (generate)
        {

            allTiles.Clear();
            generate = false;
            GenerateGridCirc(layerCount);
        }

        Gizmos.color = Color.cyan;
        foreach (var tile in allTiles)
            Gizmos.DrawSphere(tile.pos, 0.25f);
    }
}
