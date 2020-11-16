using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MapData {
    public Vector2Int mapSize;
    public bool[,] nodes; // false represents off, true represents on

    Vector3 topLeftPos;

    public MapData(Vector2Int mapSize, bool[,] nodes) {
        this.mapSize = mapSize;
        this.nodes = nodes;
        topLeftPos = new Vector3(-mapSize.x / 2f, mapSize.y / 2f, 0);
    }

    public Vector3 NodePosToWorldPos (Vector2Int nodePos) {
        return topLeftPos + new Vector3(nodePos.x, -nodePos.y, 0);
    }
}

public class Map : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;

    MapData data;

    MapRenderer mapRenderer;

    private void Start() {
        mapRenderer = GetComponent<MapRenderer>();

        data = MapGenerator.GenerateMap(mapWidth, mapHeight);

        mapRenderer.SetMap(ref data);
    }
}
