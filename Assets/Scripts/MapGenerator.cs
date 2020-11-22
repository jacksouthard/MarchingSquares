using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static int[,] GenerateMap (int width, int height) {
        Vector2Int size = new Vector2Int(width, height);
        int[,] nodes = new int[width, height];

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                nodes[x, y] = (y == 0 || y == height - 1) ? -1 : -2;
                //nodes[x, y] = Random.value < 0.5f ? -1 : -2;
            }
        }

        return nodes;
    }
}
