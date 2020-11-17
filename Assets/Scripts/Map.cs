using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MapData {
    public Vector2Int mapSize;
    public bool[,] nodes; // false represents off, true represents on

    public Vector3 topLeftPos;

    public MapData(Vector2Int mapSize, bool[,] nodes) {
        this.mapSize = mapSize;
        this.nodes = nodes;
        topLeftPos = new Vector3(-mapSize.x / 2f, mapSize.y / 2f, 0);
    }

    public Vector3 NodePosToWorldPos (Vector2Int nodePos) {
        return topLeftPos + new Vector3(nodePos.x, -nodePos.y, 0);
    }

    public Vector2Int WorldPosToTilePos (Vector3 worldPos) {
        worldPos -= topLeftPos;
        int x = Mathf.RoundToInt(worldPos.x);
        int y = -Mathf.RoundToInt(worldPos.y);

        x = Mathf.Clamp(x, 0, mapSize.x - 1);
        y = Mathf.Clamp(y, 0, mapSize.y - 1);

        return new Vector2Int(x, y);
    }

    public bool NodePosInMap (Vector2Int nodePos) {
        if (nodePos.x < 0 || nodePos.y < 0 || nodePos.x > mapSize.x - 1 || nodePos.y > mapSize.y - 1) return false;
        return true;
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

    public Vector2Int WorldPosToTilePos (Vector3 worldPos) {
        return data.WorldPosToTilePos(worldPos);
    }

    // NODE ALTERATION
    public void AlterNodes (ref NodeAlteration alteration) {
        ApplyAlteration(ref alteration);
    }

    void ApplyAlteration (ref NodeAlteration alteration) {
        int xLen = alteration.alterations.GetLength(0);
        int yLen = alteration.alterations.GetLength(1);
        for (int y = 0; y < yLen; y++) {
            for (int x = 0; x < xLen; x++) {
                bool alterAtPos = alteration.alterations[x, y]; 
                if (alterAtPos) {
                    Vector2Int pos = alteration.topLeftNodePos + new Vector2Int(x, y);
                    if (data.NodePosInMap(pos)) {
                        data.nodes[pos.x, pos.y] = alteration.subtract ? !alterAtPos : alterAtPos;
                    }
                }
            }
        }
        int startX = alteration.topLeftNodePos.x - 1;
        int startY = alteration.topLeftNodePos.y - 1;
        int endX = startX + xLen + 2;
        int endY = startY + yLen + 2;
        mapRenderer.UpdateNodes(ref data, startX, endX, startY, endY);
    }

    public struct NodeAlteration {
        public Vector2Int topLeftNodePos;
        public bool[,] alterations;
        public bool subtract; // if we are subtracting, alterations with true will subtract

        public NodeAlteration(Vector2Int topLeftNodePos, bool[,] alterations, bool subtract) {
            this.topLeftNodePos = topLeftNodePos;
            this.alterations = alterations;
            this.subtract = subtract;
        }
    }
}
