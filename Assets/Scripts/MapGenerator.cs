﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static MapData GenerateMap (int width, int height) {
        Vector2Int size = new Vector2Int(width, height);
        bool[,] nodes = new bool[width, height];

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                nodes[x, y] = Random.value < 0.5f; // randomize nodes for now
            }
        }

        return new MapData(size, nodes);
    }
}