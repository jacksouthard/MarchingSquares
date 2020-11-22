using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMouseTester : MonoBehaviour
{
    public Map map;

    bool add = false;

    Camera mainCam;

    bool[,] curAlteration;

    bool[,] alteration1;
    bool[,] alteration2;
    bool[,] alteration3;
    bool[,] alteration4;
    bool[,] alteration5;

    private void Start() {
        mainCam = Camera.main;

        alteration1 = new bool[1, 1] {{ true }};
        alteration2 = new bool[3, 3] { { false, true, false }, { true, true, true }, { false, true, false } };
        alteration3 = new bool[3, 3] { { true, true, true }, { true, true, true }, { true, true, true } };
        alteration4 = new bool[5, 5] { { false, false, true, false, false }, { false, true, true, true, false }, { true, true, true, true, true }, { false, true, true, true, false }, { false, false, true, false, false } };
        alteration5 = new bool[5, 5] { { true, true, true, true, true }, { true, true, true, true, true }, { true, true, true, true, true }, { true, true, true, true, true }, { true, true, true, true, true } };

        curAlteration = alteration1;
    }

    private void Update() {
        //if (Input.GetKeyDown(KeyCode.A)) add = true;
        //if (Input.GetKeyDown(KeyCode.S)) add = false;

        if (Input.GetKeyDown(KeyCode.Alpha1)) curAlteration = alteration1;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) curAlteration = alteration2;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) curAlteration = alteration3;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) curAlteration = alteration4;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) curAlteration = alteration5;

        if (Input.GetMouseButtonDown(0)) {
            Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int alterationCenterNodePos = map.WorldPosToNodePos(worldPos);
            Vector2Int alterationTopLeft = alterationCenterNodePos - new Vector2Int(curAlteration.GetLength(0) / 2, curAlteration.GetLength(1) / 2);
            Map.NodeAlteration alteration = new Map.NodeAlteration(alterationTopLeft, curAlteration, !add);
            map.AlterNodes(ref alteration);
        }
    }
}
