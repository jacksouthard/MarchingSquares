﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRenderer : MonoBehaviour
{
    MapChunk chunk;

    // showing nodes
    public bool showNodes;
    ParticleSystem nodePS;
    ParticleSystem.Particle[] nodeParticles;
    Color nodeFilledColor = new Color(1, 1, 1, 1);
    Color nodeUnassignedColor = new Color(0.2f, 0.2f, 0.2f, 1);

    // marching squares
    Mesh mesh;
    Vector3[] allVerts;
    int allVertsSideLength;
    GridSquare[,] gridSquares;
    int gridSquaresWidth;
    int gridSquaresHeight;

    MeshFilter mf;
    MeshRenderer mr;

    public void Initialize (MapChunk chunk) {
        this.chunk = chunk;

        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();

        InitializeMarchingSquares();

        if (showNodes) InitializeNodeVisuals();
    }

    public void Recalculate() {
        Recalculate(0, gridSquaresWidth, 0, gridSquaresHeight);
    }
    public void Recalculate (int startX, int endX, int startY, int endY) {
        if (showNodes) UpdateNodeVisuals();
        UpdateGridSquares(startX, endX, startY, endY);
    }

    // MARCHING SQUARES
    void InitializeMarchingSquares () {
        allVertsSideLength = 2 * Map.chunkSize - 1;
        allVerts = new Vector3[allVertsSideLength * allVertsSideLength];

        int i = 0;         
        for (int y = 0; y < allVertsSideLength; y++) {
            for (int x = 0; x < allVertsSideLength; x++) {
                allVerts[i] = chunk.GetTopLeftPos() + new Vector3(x / 2f, -y / 2f, 0);
                i++;
            }
        }

        mesh = new Mesh();
        mf.sharedMesh = mesh;
        mesh.SetVertices(allVerts);
         
        gridSquaresWidth = Map.chunkTileSize;   
        gridSquaresHeight = Map.chunkTileSize;
        gridSquares = new GridSquare[gridSquaresWidth, gridSquaresHeight];
        for (int y = 0; y < gridSquaresHeight; y++) {
            for (int x = 0; x < gridSquaresWidth; x++) {
                gridSquares[x, y] = new GridSquare(0b11111111, null);
            }
        }
        //UpdateGridSquares(0, gridSquaresWidth, 0, gridSquaresHeight);
    }

    void UpdateGridSquares (int startX, int endX, int startY, int endY) {
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
                if (chunk.GetNodeFilled(x, y)) config += 8; // 1000
                // top right
                if (chunk.GetNodeFilled(x + 1, y)) config += 4; // 0100
                // bottom right
                if (chunk.GetNodeFilled(x + 1, y + 1)) config += 2; // 0010
                // bottom left
                if (chunk.GetNodeFilled(x, y + 1)) config += 1; // 0001
                configCalcCount++;

                if (gridSquares[x, y].config != config) {
                    // the config changed so update the data and recalculate triangles
                    gridSquares[x, y].config = config;
                    gridSquares[x, y].tris = CalculateTrianglesForConfig(x, y, config);
                    triangleCalcCount++;
                }
            }
        }
        //Debug.Log("Recalculated " + configCalcCount + " configs and " + triangleCalcCount + " triangles");
        UpdateTriangles();
    }

    struct GridSquare {
        public byte config;
        public int[] tris;

        public GridSquare(byte config, int[] tris) {
            this.config = config;
            this.tris = tris;
        }
    }

    void UpdateTriangles () {
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

    int[] CalculateTrianglesForConfig (int x, int y, byte config) {
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
        return Flatten2DIndex(x * 2, y * 2, allVertsSideLength);
    }
    int GetTopIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 1, y * 2, allVertsSideLength);
    }
    int GetTopRightIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 2, y * 2, allVertsSideLength);
    }
    int GetRightIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 2, y * 2 + 1, allVertsSideLength);
    }
    int GetBottomRightIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 2, y * 2 + 2, allVertsSideLength);
    }
    int GetBottomIndex(int x, int y) {
        return Flatten2DIndex(x * 2 + 1, y * 2 + 2, allVertsSideLength);
    }
    int GetBottomLeftIndex(int x, int y) {
        return Flatten2DIndex(x * 2, y * 2 + 2, allVertsSideLength);
    }
    int GetLeftIndex(int x, int y) {
        return Flatten2DIndex(x * 2, y * 2 + 1, allVertsSideLength);
    }

    // SHOWING NODES (DEBUG)
    void InitializeNodeVisuals () {
        GameObject psPrefab = Resources.Load<GameObject>("NodeParticles");
        nodePS = Instantiate(psPrefab, transform).GetComponent<ParticleSystem>();

        nodeParticles = new ParticleSystem.Particle[Map.chunkSize * Map.chunkSize];
        nodePS.Emit(nodeParticles.Length);
        nodePS.GetParticles(nodeParticles);

        int i = 0;
        for (int y = 0; y < Map.chunkSize; y++) {
            for (int x = 0; x < Map.chunkSize; x++) {
                nodeParticles[i].position = chunk.LocalPosToWorldPos(new Vector2Int(x, y));
                i++;
            }
        }
    }

    void UpdateNodeVisuals () {
        int i = 0;
        for (int y = 0; y < Map.chunkSize; y++) {
            for (int x = 0; x < Map.chunkSize; x++) {
                Vector2Int localPos = new Vector2Int(x, y);
                Vector2Int nodePos = chunk.LocalPosToNodePos(localPos);
                Color color;
                if (chunk.map.GetNodeFilled(nodePos)) color = nodeFilledColor;
                else color = nodeUnassignedColor;

                if (chunk.GetNodeIsEdge(localPos)) color = Color.Lerp(color, Color.red, 0.5f);

                nodeParticles[i].startColor = color;
                i++;
            }
        }
        nodePS.SetParticles(nodeParticles);
    }
}
