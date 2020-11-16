using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRenderer : MonoBehaviour
{
    // showing nodes
    public bool showNodes;
    ParticleSystem nodePS;
    ParticleSystem.Particle[] nodeParticles;
    Color nodeOnColor = new Color(1, 1, 1, 1);
    Color nodeOffColor = new Color(0.2f, 0.2f, 0.2f, 1);

    public void SetMap (ref MapData mapData) {
        if (showNodes) InitializeNodeVisuals(ref mapData);
    }

    public void UpdateNodes (ref MapData mapData) {
        if (showNodes) UpdateNodeVisuals(ref mapData);
    }

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
                nodeParticles[i].startColor = mapData.nodes[x, y] ? nodeOnColor : nodeOffColor;
                print(mapData.nodes[x, y]);
                i++;
            }
        }
        nodePS.SetParticles(nodeParticles);
    }
}
