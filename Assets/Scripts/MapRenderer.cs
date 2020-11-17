using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRenderer : MonoBehaviour
{
    // showing nodes
    public bool showNodes;
    ParticleSystem nodePS;
    ParticleSystem.Particle[] nodeParticles;
    Color nodeFilledColor = new Color(1, 1, 1, 1);
    Color nodeUnassignedColor = new Color(0.2f, 0.2f, 0.2f, 1);

    // marching squares
    Mesh mesh;
    Vector3[] allVerts;
    int allVertsWidth;
    int allVertsHeight;
    GridSquare[,] gridSquares;
    int gridSquaresWidth;
    int gridSquaresHeight;

    MeshFilter mf;
    MeshRenderer mr;

    bool initialized = false;
    void Initialize () {
        initialized = true;

        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
    }

    public void SetMap (ref MapData mapData) {
        if (!initialized) Initialize();
        InitializeMarchingSquares(ref mapData);

        if (showNodes) InitializeNodeVisuals(ref mapData);
    }

    public void UpdateNodes(ref MapData mapData) {
        UpdateNodes(ref mapData, 0, gridSquaresWidth, 0, gridSquaresHeight);
    }
    public void UpdateNodes (ref MapData mapData, int startX, int endX, int startY, int endY) {
        if (showNodes) UpdateNodeVisuals(ref mapData);
        UpdateGridSquares(ref mapData, startX, endX, startY, endY);
    }

    // MARCHING SQUARES
    void InitializeMarchingSquares (ref MapData mapData) {
        int vertsX = 2 * mapData.mapSize.x - 1;
        int vertsY = 2 * mapData.mapSize.y - 1;
        allVerts = new Vector3[vertsX * vertsY];
        allVertsWidth = vertsX;
        allVertsHeight = vertsY;

        int i = 0;         
        for (int y = 0; y < vertsY; y++) {
            for (int x = 0; x < vertsX; x++) {
                allVerts[i] = mapData.topLeftPos + new Vector3(x / 2f, -y / 2f, 0);
                i++;
            }
        }

        mesh = new Mesh();
        mf.sharedMesh = mesh;
        mesh.SetVertices(allVerts);
         
        gridSquaresWidth = mapData.mapSize.x - 1;   
        gridSquaresHeight = mapData.mapSize.y - 1;
        gridSquares = new GridSquare[gridSquaresWidth, gridSquaresHeight];
        for (int y = 0; y < gridSquaresHeight; y++) {
            for (int x = 0; x < gridSquaresWidth; x++) {
                gridSquares[x, y] = new GridSquare(0b11111111, null);
            }
        }
        UpdateGridSquares(ref mapData, 0, gridSquaresWidth, 0, gridSquaresHeight);
    }

    void UpdateGridSquares (ref MapData mapData, int startX, int endX, int startY, int endY) {
        startX = Mathf.Clamp(startX, 0, gridSquaresWidth);
        endX = Mathf.Clamp(endX, 0, gridSquaresWidth);
        startY = Mathf.Clamp(startY, 0, gridSquaresHeight);
        endY = Mathf.Clamp(endY, 0, gridSquaresHeight);

        int triangleCalcCount = 0;
        int configCalcCount = 0;
        for (int y = startY; y < endY; y++) {
            for (int x = startX; x < endX; x++) {
                byte config = 0;
                // top left
                if (mapData.GetNodeFilled(x, y)) config += 8; // 1000
                // top right
                if (mapData.GetNodeFilled(x + 1, y)) config += 4; // 0100
                // bottom right
                if (mapData.GetNodeFilled(x + 1, y + 1)) config += 2; // 0010
                // bottom left
                if (mapData.GetNodeFilled(x, y + 1)) config += 1; // 0001
                configCalcCount++;

                if (gridSquares[x, y].config != config) {
                    // the config changed so update the data and recalculate triangles
                    gridSquares[x, y].config = config;
                    gridSquares[x, y].tris = CalculateTrianglesForConfig(ref mapData, x, y, config);
                    triangleCalcCount++;
                }
            }
        }
        //Debug.Log("Recalculated " + configCalcCount + " configs and " + triangleCalcCount + " triangles");
        UpdateTriangles(ref mapData);
    }

    struct GridSquare {
        public byte config;
        public int[] tris;

        public GridSquare(byte config, int[] tris) {
            this.config = config;
            this.tris = tris;
        }
    }

    void UpdateTriangles (ref MapData mapData) {
        List<int> allTriangles = new List<int>();
        int[] configTrisBuf;
        for (int y = 0; y < gridSquaresHeight; y++) {
            for (int x = 0; x < gridSquaresWidth; x++) {
                configTrisBuf = gridSquares[x, y].tris;
                for (int i = 0; i < configTrisBuf.Length; i++) {
                    allTriangles.Add(configTrisBuf[i]);
                }
            }
        }

        mesh.SetTriangles(allTriangles.ToArray(), 0);
    }

    int[] CalculateTrianglesForConfig (ref MapData mapData, int x, int y, byte config) {
        switch(config) {
            // 0 points
            case 0:
                return new int[0];
            // 1 points
            case 1:
                return new int[] { GetBottomLeftIndex(x, y), GetLeftIndex(x, y), GetBottomIndex(x, y) };
            case 2:
                return new int[] { GetBottomRightIndex(x, y), GetBottomIndex(x, y), GetRightIndex(x, y) };
            case 4:
                return new int[] { GetTopRightIndex(x, y), GetRightIndex(x, y), GetTopIndex(x, y) };
            case 8:
                return new int[] { GetTopLeftIndex(x, y), GetTopIndex(x, y), GetLeftIndex(x, y) };
            // 2 points
            case 3:
                return new int[] { GetBottomLeftIndex(x, y), GetLeftIndex(x, y), GetRightIndex(x, y), GetBottomRightIndex(x,y), GetBottomLeftIndex(x,y), GetRightIndex(x,y) };
            case 6:
                return new int[] { GetBottomRightIndex(x, y), GetBottomIndex(x, y), GetTopIndex(x, y), GetTopRightIndex(x, y), GetBottomRightIndex(x, y), GetTopIndex(x, y) };
            case 9:
                return new int[] { GetBottomLeftIndex(x, y), GetTopLeftIndex(x, y), GetBottomIndex(x, y), GetTopLeftIndex(x, y), GetTopIndex(x, y), GetBottomIndex(x, y) };
            case 12:
                return new int[] { GetTopLeftIndex(x, y), GetRightIndex(x, y), GetLeftIndex(x, y), GetTopRightIndex(x, y), GetRightIndex(x, y), GetTopLeftIndex(x, y) };
            case 5:
                return new int[] { GetTopIndex(x, y), GetTopRightIndex(x, y), GetRightIndex(x, y), GetTopIndex(x, y), GetRightIndex(x, y), GetBottomIndex(x, y), GetTopIndex(x,y), GetBottomIndex(x,y), GetBottomLeftIndex(x,y), GetTopIndex(x,y), GetBottomLeftIndex(x,y), GetLeftIndex(x,y) };
            case 10:
                return new int[] { GetTopLeftIndex(x,y), GetTopIndex(x,y), GetRightIndex(x,y), GetTopLeftIndex(x,y), GetRightIndex(x,y), GetBottomRightIndex(x,y), GetTopLeftIndex(x,y), GetBottomRightIndex(x,y), GetBottomIndex(x,y), GetTopLeftIndex(x,y), GetBottomIndex(x,y), GetLeftIndex(x,y) };
            // 3 points
            case 7:
                return new int[] { GetTopIndex(x, y), GetTopRightIndex(x, y), GetBottomRightIndex(x, y), GetTopIndex(x, y), GetBottomRightIndex(x, y), GetBottomLeftIndex(x, y), GetTopIndex(x, y), GetBottomLeftIndex(x, y), GetLeftIndex(x, y) };
            case 11:
                return new int[] { GetTopLeftIndex(x,y), GetTopIndex(x,y), GetRightIndex(x,y), GetTopLeftIndex(x,y), GetRightIndex(x,y), GetBottomRightIndex(x,y), GetTopLeftIndex(x,y), GetBottomRightIndex(x,y), GetBottomLeftIndex(x,y) };
            case 13:
                return new int[] { GetTopLeftIndex(x, y), GetTopRightIndex(x, y), GetRightIndex(x, y), GetTopLeftIndex(x, y), GetRightIndex(x, y), GetBottomIndex(x, y), GetTopLeftIndex(x, y), GetBottomIndex(x, y), GetBottomLeftIndex(x, y) };
            case 14:
                return new int[] { GetTopLeftIndex(x, y), GetTopRightIndex(x, y), GetBottomRightIndex(x, y), GetTopLeftIndex(x, y), GetBottomRightIndex(x, y), GetBottomIndex(x, y), GetTopLeftIndex(x, y), GetBottomIndex(x, y), GetLeftIndex(x, y) };
            // 4 points
            case 15:
                return new int[] { GetTopLeftIndex(x,y), GetTopRightIndex(x,y), GetBottomRightIndex(x,y), GetTopLeftIndex(x,y), GetBottomRightIndex(x,y), GetBottomLeftIndex(x,y) };
        }
        return new int[0];
    }

    int Flatten2DIndex (int x, int y, int width) {
        return y * width + x;
    }

    int GetTopLeftIndex (int x, int y) {
        return Flatten2DIndex(x * 2, y * 2, allVertsWidth);
    }
    int GetTopIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 1, y * 2, allVertsWidth);
    }
    int GetTopRightIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 2, y * 2, allVertsWidth);
    }
    int GetRightIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 2, y * 2 + 1, allVertsWidth);
    }
    int GetBottomRightIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 2, y * 2 + 2, allVertsWidth);
    }
    int GetBottomIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 1, y * 2 + 2, allVertsWidth);
    }
    int GetBottomLeftIndex(int x, int y) {
        return Flatten2DIndex(x * 2, y * 2 + 2, allVertsWidth);
    }
    int GetLeftIndex(int x, int y) {
        return Flatten2DIndex(x * 2, y * 2 + 1, allVertsWidth);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        //if (allVerts != null && allVerts.Length > 0) {
        //    Vector3 scale = Vector3.one * 0.1f;
        //    for (int i = 0; i < allVerts.Length; i++) {
        //        Gizmos.DrawWireCube(allVerts[i], scale);
        //    }
        //}
        //if (mesh != null) {
        //    Vector3[] meshVerts = mesh.vertices;
        //    Vector3 scale = Vector3.one * 0.1f;
        //    for (int i = 0; i < meshVerts.Length; i++) {
        //        Gizmos.DrawWireCube(meshVerts[i], scale);
        //    }

        //    int[] tris = mesh.triangles;
        //    int trisCount = tris.Length / 3;
        //    for (int i = 0; i < trisCount; i++) {
        //        Gizmos.DrawLine(meshVerts[tris[i * 3]], meshVerts[tris[i * 3 + 1]]);
        //        Gizmos.DrawLine(meshVerts[tris[i * 3+1]], meshVerts[tris[i * 3 + 2]]);
        //        Gizmos.DrawLine(meshVerts[tris[i * 3 + 2]], meshVerts[tris[i * 3 ]]);
        //    }
        //}
    }
#endif

    // SHOWING NODES (DEBUG)
    void InitializeNodeVisuals (ref MapData mapData) {
        GameObject psPrefab = Resources.Load<GameObject>("NodeParticles");
        nodePS = Instantiate(psPrefab, transform).GetComponent<ParticleSystem>();

        nodeParticles = new ParticleSystem.Particle[mapData.mapSize.x * mapData.mapSize.y];
        nodePS.Emit(nodeParticles.Length);
        nodePS.GetParticles(nodeParticles);

        int i = 0;
        for (int y = 0; y < mapData.mapSize.y; y++) {
            for (int x = 0; x < mapData.mapSize.x; x++) {
                nodeParticles[i].position = mapData.NodePosToWorldPos(new Vector2Int(x, y));
                i++;
            }
        }
        UpdateNodeVisuals(ref mapData);
    }

    void UpdateNodeVisuals (ref MapData mapData) {
        int i = 0;
        for (int y = 0; y < mapData.mapSize.y; y++) {
            for (int x = 0; x < mapData.mapSize.x; x++) {
                int nodeID = mapData.nodes[x, y];
                Color color;
                if (nodeID == -2) color = nodeFilledColor;
                else if (nodeID == -1) color = nodeUnassignedColor;
                else color = new Color((nodeID % 5 + 1) / 5f, ((nodeID * 2) % 5 + 1) / 5f, ((nodeID * 3) % 5 + 1) / 5f, 1);

                nodeParticles[i].startColor = color;
                i++;
            }
        }
        nodePS.SetParticles(nodeParticles);
    }
}
