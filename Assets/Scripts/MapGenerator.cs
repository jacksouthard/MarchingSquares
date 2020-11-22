using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static bool[,] GenerateMap (int width, int height) {
        bool[,] nodes = new bool[width, height];

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                nodes[x, y] = !(y == 0 || y == height - 1);
                //nodes[x, y] = Random.value < 0.5f ? -1 : -2;
            }
        }

        return nodes;
    }
}
