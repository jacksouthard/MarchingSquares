using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    // MAPDATA
    // dimensions
    public int width { get; private set; } // the number of nodes per row
    public int height { get; private set; } // the number of nodes per collumn
    public int tilesPerRow { get; private set; } // the number of grid tiles per row
    public int tilesPerCollumn { get; private set; } // the number of grid tiles per collumn
    // nodes
    // true is filled, false is empty
    public bool[,] Nodes { get; private set; }

    // CHUNKS
    public int chunkCountX;
    public int chunkCountY;
    public const int chunkSize = 8; // the number of nodes per chunk side
    public const int chunkTileSize = chunkSize - 1;
    MapChunk[,] chunks;

    // TRANSLATIONS
    Vector3 topLeftPos;

    #region INITIALIZATION
    private void Start() {
        // variable initialization
        width = chunkCountX * chunkSize - (chunkCountX - 1);
        height = chunkCountY * chunkSize - (chunkCountY - 1);
        tilesPerRow = width - 1;
        tilesPerCollumn = height - 1;
        topLeftPos = new Vector3(-width / 2f, height / 2f, 0);

        InitializeChunks();

        Nodes = MapGenerator.GenerateMap(width, height);

        InitializeNodes();
        RecalculateAllChunks();
    }

    // NODE INITIALIZATION
    void InitializeNodes () {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (Nodes[x, y]) {
                    //FillTile(new Vector2Int(x, y));
                } else {
                    EmptyTile(new Vector2Int(x, y));
                }
            }
        }
    }
    #endregion INITIALIZATION

    #region CHUNKS

    void InitializeChunks () {
        GameObject chunkPrefab = Resources.Load<GameObject>("MapChunk");
        chunks = new MapChunk[chunkCountX, chunkCountY];
        for (int y = 0; y < chunkCountY; y++) {
            for (int x = 0; x < chunkCountX; x++) {
                chunks[x, y] = Instantiate(chunkPrefab, transform).GetComponent<MapChunk>();
                chunks[x, y].Initialize(this, x, y);
                chunks[x, y].name = "Chunk " + new Vector2Int(x, y);
            }
        }
    }

    void RecalculateAllChunks () {
        RecalculateChunks(0, chunkCountX - 1, 0, chunkCountY - 1);
    }
    void RecalculateChunks (int chunkStartX, int chunkEndX, int chunkStartY, int chunkEndY) {
        for (int y = chunkStartY; y <= chunkEndY; y++) {
            for (int x = chunkStartX; x <= chunkEndX; x++) {
                chunks[x, y].Recalculate();
            }
        }
    }

    void RecalculateChunksInAlteration (int lowestAlteredX, int highestAlteredX, int lowestAlteredY, int highestAlteredY) {
        int lowestAlteredChunkX = Mathf.Max(0, (lowestAlteredX - 1) / chunkTileSize);
        int lowestAlteredChunkY = Mathf.Max(0, (lowestAlteredY - 1) / chunkTileSize);
        int highestAlteredChunkX = Mathf.Min(chunkCountX - 1, (highestAlteredX + 1) / chunkTileSize);
        int highestAlteredChunkY = Mathf.Min(chunkCountY - 1, (highestAlteredY + 1) / chunkTileSize);
        RecalculateChunks(lowestAlteredChunkX, highestAlteredChunkX, lowestAlteredChunkY, highestAlteredChunkY);
    }

    static Vector2Int NodePosToChunkLocalPos (Vector2Int nodePos, Vector2Int chunkPos) {
        return nodePos - chunkPos * chunkTileSize;
    }
    // not accurate if on boundary
    static Vector2Int NodePosToChunkPos (Vector2Int nodePos) {
        return nodePos / chunkTileSize;
    }

    public struct PosChunkPair {
        public Vector2Int chunkPos;
        public Vector2Int localPos;

        public PosChunkPair(Vector2Int chunkPos, Vector2Int nodePos) {
            this.chunkPos = chunkPos;
            this.localPos = NodePosToChunkLocalPos(nodePos, chunkPos);
        }
    } 
    List<PosChunkPair> ConvertNodePosToChunkPairs (Vector2Int nodePos) {
        bool onBoundaryX = nodePos.x % chunkTileSize == 0;
        bool onBoundaryY = nodePos.y % chunkTileSize == 0;
        if (!onBoundaryX && !onBoundaryY) {
            // only in one chunk
            return new List<PosChunkPair> { new PosChunkPair(NodePosToChunkPos(nodePos), nodePos) };
        } else {
            // on at least one boundary
            List<PosChunkPair> posChunkPairs = new List<PosChunkPair>();

            Vector2Int boundaryIndex = nodePos / chunkTileSize;
            bool hasLowerChunkX = boundaryIndex.x > 0;
            bool hasLowerChunkY = boundaryIndex.y > 0;
            bool hasUpperChunkX = boundaryIndex.x < chunkCountX;
            bool hasUpperChunkY = boundaryIndex.y < chunkCountY;

            if (hasUpperChunkX && hasUpperChunkY) {
                posChunkPairs.Add(new PosChunkPair(boundaryIndex, nodePos));
            }
            if (hasUpperChunkY && hasLowerChunkX) {
                posChunkPairs.Add(new PosChunkPair(boundaryIndex + Vector2Int.left, nodePos));
            }
            if (hasUpperChunkX && hasLowerChunkY) {
                posChunkPairs.Add(new PosChunkPair(boundaryIndex + Vector2Int.down, nodePos));
            }
            if (hasLowerChunkX && hasLowerChunkY) {
                posChunkPairs.Add(new PosChunkPair(boundaryIndex + new Vector2Int(-1,-1), nodePos));
            }

            return posChunkPairs;
        }
    }

    #endregion CHUNKS

    #region ALTERATIONS
    public void AlterNodes (ref NodeAlteration alteration) {
        ApplyAlteration(ref alteration);
    }

    void AlterNode (Vector2Int pos, bool newState) {
        if (GetNodeFilled(pos) == newState) return;
        if (newState) {
            Debug.LogWarning("No current support for node filling");
        } else {
            EmptyTile(pos);
        }
    }

    void EmptyTile (Vector2Int nodePos) {
        SetNodeFilled(nodePos, false);
        List<PosChunkPair> posChunkPairs = ConvertNodePosToChunkPairs(nodePos);
        foreach (var pair in posChunkPairs) {
            chunks[pair.chunkPos.x, pair.chunkPos.y].NodeEmptied(pair.localPos);
        }
    }

    void ApplyAlteration (ref NodeAlteration alteration) {
        int xLen = alteration.alterations.GetLength(0);
        int yLen = alteration.alterations.GetLength(1);
        for (int y = 0; y < yLen; y++) {
            for (int x = 0; x < xLen; x++) {
                bool alterAtPos = alteration.alterations[x, y]; 
                if (alterAtPos) {
                    Vector2Int pos = alteration.topLeftNodePos + new Vector2Int(x, y);
                    if (IsNodeInMap(pos)) {
                        //data.nodes[pos.x, pos.y] = alteration.subtract ? !alterAtPos : alterAtPos;
                        AlterNode(pos, alteration.subtract ? !alterAtPos : alterAtPos);
                    }
                }
            }
        }
        int startX = alteration.topLeftNodePos.x - 1;
        int startY = alteration.topLeftNodePos.y - 1;
        int endX = startX + xLen + 2;
        int endY = startY + yLen + 2;

        RecalculateChunksInAlteration(startX, endX, startY, endY);
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
    #endregion ALTERATIONS

    #region MAPDATA

    #region MAPDATAGET
    public bool IsNodeInMap(Vector2Int nodePos) {
        if (nodePos.x < 0 || nodePos.y < 0 || nodePos.x > width - 1 || nodePos.y > height - 1) return false;
        return true;
    }

    public bool GetNodeFilled(Vector2Int nodePos) {
        return GetNodeFilled(nodePos.x, nodePos.y);
    }
    public bool GetNodeFilled(int x, int y) {
        return Nodes[x, y];
    }
    public bool GetNodeInMapAndFilled(Vector2Int nodePos) {
        return GetNodeInMapAndFilled(nodePos.x, nodePos.y);
    }
    public bool GetNodeInMapAndFilled(int x, int y) {
        if (!IsNodeInMap(new Vector2Int(x, y))) return false;
        return Nodes[x, y];
    }

    public bool ShouldBeEdge(Vector2Int nodePos) {
        int x = nodePos.x;
        int y = nodePos.y;
        if (GetNodeInMapAndFilled(x, y - 1) || GetNodeInMapAndFilled(x + 1, y) || GetNodeInMapAndFilled(x, y + 1) || GetNodeInMapAndFilled(x - 1, y)) return true;
        return false;
    }
    #endregion MAPDATAGET

    #region MAPDATASET
    public void SetNodeFilled(Vector2Int nodePos, bool filled) {
        Nodes[nodePos.x, nodePos.y] = filled;
    }
    public void SetNodeFilled(int x, int y, bool filled) {
        Nodes[x, y] = filled;
    }

    #endregion MAPDATASET

    #endregion MAPDATA

    #region TRANSLATIONS
    public Vector3 GetTopLeftPos () {
        return topLeftPos;
    }

    public Vector3 NodePosToWorldPos(int x, int y) {
        return topLeftPos + new Vector3(x, y, 0);
    }
    public Vector3 NodePosToWorldPos(Vector2Int nodePos) {
        return topLeftPos + new Vector3(nodePos.x, -nodePos.y, 0);
    }
    public Vector3 NodePosToWorldPos(Vector2 nodePos) {
        return topLeftPos + new Vector3(nodePos.x, -nodePos.y, 0);
    }

    public Vector2Int WorldPosToNodePos(Vector3 worldPos) {
        worldPos -= topLeftPos;
        int x = Mathf.RoundToInt(worldPos.x);
        int y = -Mathf.RoundToInt(worldPos.y);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return new Vector2Int(x, y);
    }

    #endregion TRANSLATIONS
}
