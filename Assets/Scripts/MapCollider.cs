using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapCollider : MonoBehaviour
{
    Map map;

    List<EdgeCollider2D> activeEdges = new List<EdgeCollider2D>();

    public void Initialize(Map map) {
        this.map = map;
    }

    public void Recalculate () {
        int assignedEdges = 0;
        // for each room, create a collider
        foreach (KeyValuePair<int, RoomData> pair in map.rooms) {
            foreach (List<Vector2> edgePoints in pair.Value.edgePoints) {
                // if we dont have enough instantiated edge colliders, create more
                while (activeEdges.Count < assignedEdges + 1) {
                    activeEdges.Add(gameObject.AddComponent<EdgeCollider2D>());
                }

                activeEdges[assignedEdges].enabled = true;
                activeEdges[assignedEdges].SetPoints(edgePoints);

                assignedEdges++;
            }
        }

        // if we have more edge colliders than we need, reduce amount by half the overage to avoid too many component creation and deletion
        int overage = activeEdges.Count - assignedEdges;
        if (overage > 0) {
            int targetCount = assignedEdges + Mathf.CeilToInt(overage / 2);
            while (activeEdges.Count > targetCount) {
                // remove the one at the end of the list
                Destroy(activeEdges[activeEdges.Count - 1]);
                activeEdges.RemoveAt(activeEdges.Count - 1);
            }

            // then disable all that arent assigned
            for (int i = assignedEdges; i < activeEdges.Count; i++) {
                activeEdges[i].enabled = false;
            }
        }
    }
}
