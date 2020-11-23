using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapChunk : MonoBehaviour
{
    public Map map { get; private set; }

    // CHUNK DATA
    Vector2Int chunkPos;
    Vector2Int topLeftNodePos;

    // EDGES
    List<Vector2Int> edgeNodes = new List<Vector2Int>();
    public List<List<Vector2>> allEdgePaths { get; private set; } = new List<List<Vector2>>();

    // TRANSLATIONS
    Vector3 topLeftPos;

    // MAP COMPONENTS
    MapRenderer mapRenderer;
    MapCollider mapCollider;

    public void Initialize (Map map, int chunkX, int chunkY) {
        // chunk data
        this.map = map;
        chunkPos = new Vector2Int(chunkX, chunkY);
        topLeftNodePos = chunkPos * Map.chunkTileSize;
        topLeftPos = map.NodePosToWorldPos(topLeftNodePos);

        // references
        // map components
        mapRenderer = GetComponent<MapRenderer>();
        mapCollider = GetComponent<MapCollider>();
        mapRenderer.Initialize(this);
        mapCollider.Initialize(this);
    }

    public void Recalculate () {
        UpdateEdges();

        mapRenderer.Recalculate();
        mapCollider.Recalculate();
    }

    #region ALTERATIONS

    List<Vector2Int> newlyEmptiedLocalNodes = new List<Vector2Int>(); // all the tiles that have switched from filled to empty and have not been sorted into existing rooms / new rooms
    public void NodeEmptied (Vector2Int localPos) {
        newlyEmptiedLocalNodes.Add(localPos);
    }

    #endregion ALTERATIONS

    void UpdateEdges() {
        // iterate through the edge nodes and remove any that are no longer edges
        for (int i = edgeNodes.Count - 1; i >= 0; i--) {
            if (!ShouldBeEdge(edgeNodes[i])) {
                // remove it from the edge nodes
                edgeNodes.RemoveAt(i);
            }
        }

        // iterate through the newly emptied nodes and add the ones that are edges to the edge nodes list
        foreach (var localEmptiedNode in newlyEmptiedLocalNodes) {
            if (ShouldBeEdge(localEmptiedNode)) edgeNodes.Add(localEmptiedNode);
        }

        // recalculate the edge paths
        List<Vector2Int> remainingEdgeTiles = new List<Vector2Int>(edgeNodes);
        allEdgePaths.Clear();
        int escape = 3000;
        while (remainingEdgeTiles.Count > 0) {
            if (escape-- < 0) {
                Debug.LogError("Infinite loop when calculating edge points for " + name);
                break;
            }

            List<Vector2Int> nodesInNewEdgePath = new List<Vector2Int>();
            List<List<Vector2>> newEdgePaths = CalculateEdgePoints(remainingEdgeTiles[0], ref nodesInNewEdgePath, out int edgeCalculationIterations);
            
            // then remove all overlap
            foreach (var localPos in nodesInNewEdgePath) {
                remainingEdgeTiles.Remove(localPos);
            }
            // then add new edge to list
            foreach (var newEdgePath in newEdgePaths) {
                allEdgePaths.Add(newEdgePath);
            }
        }

        newlyEmptiedLocalNodes.Clear();
    }

    #region EDGEPATHS
    // returns all the points along a circular walk of the room starting at a given position on the edge of the room
    List<List<Vector2>> CalculateEdgePoints(Vector2Int localStartPos, ref List<Vector2Int> includedEdgeNodes, out int runIterations) {
        runIterations = 0;

        List<List<Vector2>> paths = new List<List<Vector2>> {
            new List<Vector2>() // create the first path
        };
        int pathIndex = 0;

        includedEdgeNodes.Add(localStartPos);

        // calculate our starting direction which should be the direction pointing towards any neighboring filled tile
        int startDir = 0;
        while (!GetNodeInChunkAndFilled(localStartPos + GetDirFromDirectionIndex(startDir))) {
            startDir++;
            if (startDir > 3) {
                Debug.LogError("Edge point has no neigboring filled nodes");
                return paths;
            }
        }
        bool lastPlacementDirAlligned = false;
        int lastPlacementDir = -1;

        int curDir = startDir;
        Vector2Int curLocalPos = localStartPos;

        //print("Start calc " + localStartPos + " D: " + startDir);

        bool initial = true;
        bool previousWasOutsideChunk = false;
        bool shouldBreak = false;
        while (runIterations++ < 10000 && !shouldBreak) {
            //print("pos: " + curPos + ", dir: " + curDir);
            if (initial) {
                initial = false;
            } else {
                shouldBreak = curDir == startDir && curLocalPos == localStartPos;
            }

            Vector2Int curDirVector = GetDirFromDirectionIndex(curDir);
            if (!IsNodeInChunk(curLocalPos + curDirVector)) {
                // the cur node is outside the chunk
                if (!previousWasOutsideChunk) {
                    //print("Hit edge: " + curLocalPos + " D: " + curDir);
                    // if the previous was in the chunk then we just hit an edge
                    previousWasOutsideChunk = true;
                }
                curDir = RotateClockwise(curDir);
            } else if (GetNodeFilled(curLocalPos + curDirVector)) {
                if (previousWasOutsideChunk) {
                    //print("Refound filled: " + curLocalPos + " D: " + curDir);
                    if (paths[pathIndex].Count < 2) {
                        //print("Removed last");
                        paths.RemoveAt(pathIndex);
                        pathIndex--;
                    }
                    // we have to begin a new path
                    paths.Add(new List<Vector2>());
                    pathIndex++;
                    previousWasOutsideChunk = false;

                    lastPlacementDirAlligned = false;
                    lastPlacementDir = -1;
                }

                // the node in direction is filled so we add the point halfway in this direction and rotate clockwise
                Vector2 mapLocalPos = curLocalPos + new Vector2(curDirVector.x / 2f, curDirVector.y / 2f);
                // if we are placing the node in the same direction as the last one, we can simply move the last placed one to this one
                if (curDir == lastPlacementDir) {
                    if (lastPlacementDirAlligned) {
                        paths[pathIndex].RemoveAt(paths[pathIndex].Count - 1);
                    } else {
                        lastPlacementDirAlligned = true;
                    }
                } else {
                    lastPlacementDirAlligned = false;
                }
                lastPlacementDir = curDir;

                paths[pathIndex].Add(LocalPosToWorldPos(mapLocalPos));

                curDir = RotateClockwise(curDir);
            } else {
                // the node is empty so we should move in that direction and rotate counterclockwise
                curLocalPos += curDirVector;
                curDir = RotateCounterclockwise(curDir);

                // every time we move we need to reset rotations in place and add the tile we moved onto to our list
                includedEdgeNodes.Add(curLocalPos);
            }
        }
        if (paths[pathIndex].Count < 2) {
            //print("Removed last final");
            paths.RemoveAt(pathIndex);
        }
        //print("Calculated new paths: " + paths.Count);
        return paths;
    }

    int RotateClockwise(int index) {
        index++;
        if (index > 3) index = 0;
        return index;
    }
    int RotateCounterclockwise(int index) {
        index--;
        if (index < 0) index = 3;
        return index;
    }

    Vector2Int GetDirFromDirectionIndex(int index) {
        if (index == 0) return Vector2Int.down;
        if (index == 1) return Vector2Int.right;
        if (index == 2) return Vector2Int.up;
        return Vector2Int.left;
    }
    #endregion EDGEPATHS

    #region MAPDATA
    public bool IsNodeInMap(Vector2Int localPos) {
        return map.IsNodeInMap(topLeftNodePos + localPos);
    }
    public bool IsNodeInChunk(Vector2Int localPos) {
        if (localPos.x < 0 || localPos.y < 0 || localPos.x > Map.chunkTileSize || localPos.y > Map.chunkTileSize) return false;
        return true;
    }

    public bool GetNodeFilled(Vector2Int localPos) {
        return map.GetNodeFilled(topLeftNodePos.x + localPos.x, topLeftNodePos.y + localPos.y);
    }
    public bool GetNodeFilled(int x, int y) {
        return map.GetNodeFilled(topLeftNodePos.x + x, topLeftNodePos.y + y);
    }

    public bool GetNodeInChunkAndFilled(Vector2Int localPos) {
        if (!IsNodeInChunk(localPos)) return false;
        return GetNodeFilled(localPos);
    }
    public bool ShouldBeEdge(Vector2Int localPos) {
        if (GetNodeInChunkAndFilled(localPos + Vector2Int.down) || GetNodeInChunkAndFilled(localPos + Vector2Int.right) || GetNodeInChunkAndFilled(localPos + Vector2Int.up) || GetNodeInChunkAndFilled(localPos + Vector2Int.left)) return true;
        return false;
    }
    public bool GetNodeIsEdge(Vector2Int localPos) {
        return edgeNodes.Contains(localPos);
    }
    #endregion MAPDATA

    #region TRANSLATIONS
    public Vector3 GetTopLeftPos() {
        return topLeftPos;
    }

    public Vector2Int NodePosToLocalPos(Vector2Int nodePos) {
        return nodePos - topLeftNodePos;
    }
    public Vector2Int LocalPosToNodePos (Vector2Int localPos) {
        return topLeftNodePos + localPos;
    }

    public Vector3 LocalPosToWorldPos(int x, int y) {
        return map.NodePosToWorldPos(topLeftNodePos + new Vector2Int(x, y));
    }
    public Vector3 LocalPosToWorldPos(Vector2Int nodePos) {
        return map.NodePosToWorldPos(topLeftNodePos + nodePos);
    }
    public Vector3 LocalPosToWorldPos(Vector2 nodePos) {
        return topLeftPos + new Vector3(nodePos.x, -nodePos.y, 0);
    }

    #endregion TRANSLATIONS
}
