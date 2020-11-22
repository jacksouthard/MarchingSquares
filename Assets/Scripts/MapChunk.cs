using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapChunk : MonoBehaviour
{
    Map map;
    int chunkX;
    int chunkY;

    public void Initialize (Map map, int chunkX, int chunkY) {
        this.map = map;
        this.chunkX = chunkX;
        this.chunkY = chunkY;
    }

    public void Recalculate () {

    }
}
