using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapProfiler : MonoBehaviour
{
    public Map map;
    public int operationsPerSecond;
    public int maxOperations;

    public bool decendFromTop;
    public int operationsPerRow;
    int operationsUntilDecent;
    const int decendYRange = 5;

    int minY;
    int maxY;

    int completedOperations = 0;

    float delayBetweenOperations;
    float nextOperationTime = 0;

    // alterations
    bool[,] alteration1;
    bool[,] alteration2;
    bool[,] alteration3;
    bool[,] alteration4;
    bool[,] alteration5;

    const int alterationCount = 5;
    List<bool[,]> alterations = new List<bool[,]>();

    private void Start() {
        delayBetweenOperations = 1f / operationsPerSecond;

        alteration1 = new bool[1, 1] { { true } };
        alteration2 = new bool[3, 3] { { false, true, false }, { true, true, true }, { false, true, false } };
        alteration3 = new bool[3, 3] { { true, true, true }, { true, true, true }, { true, true, true } };
        alteration4 = new bool[5, 5] { { false, false, true, false, false }, { false, true, true, true, false }, { true, true, true, true, true }, { false, true, true, true, false }, { false, false, true, false, false } };
        alteration5 = new bool[5, 5] { { true, true, true, true, true }, { true, true, true, true, true }, { true, true, true, true, true }, { true, true, true, true, true }, { true, true, true, true, true } };

        alterations.Add(alteration1);
        alterations.Add(alteration2);
        alterations.Add(alteration3);
        alterations.Add(alteration4);
        alterations.Add(alteration5);

        minY = 0;
        if (decendFromTop) {
            maxY = decendYRange;
            operationsUntilDecent = operationsPerRow;
        } else {
            maxY = map.mapHeight - 1;
        }
    }

    private void Update() {
        if (completedOperations != maxOperations && Time.time > nextOperationTime) {
            nextOperationTime = Time.time + delayBetweenOperations;

            // do a random operation
            Vector2Int alterationPos = new Vector2Int(Random.Range(0, map.mapWidth - 1), Random.Range(minY, maxY));
            Map.NodeAlteration alteration = new Map.NodeAlteration(alterationPos, alterations[Random.Range(0,alterations.Count)], true);
            map.AlterNodes(ref alteration);

            completedOperations++;

            if (decendFromTop) {
                operationsUntilDecent--;
                if (operationsUntilDecent < 1) {
                    operationsUntilDecent = operationsPerRow;
                    if (maxY < map.mapHeight - 1) {
                        minY++;
                        maxY++;
                    }
                }
            }
        }
    }
}
