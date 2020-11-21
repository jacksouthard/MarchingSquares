using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MapData {
    public Vector2Int mapSize;

    // -2 is filled
    // -1 is empty and unassigned tile
    // >= 0 is tile assigned to a room with this key
    public int[,] nodes;

    public Vector3 topLeftPos;
    public int lowestEmptyRowIndex;
    
    int leadingKey;
    public Dictionary<int, RoomData> rooms;

    public MapData(Vector2Int mapSize, int[,] nodes) {
        this.mapSize = mapSize;
        this.nodes = nodes;
        topLeftPos = new Vector3(-mapSize.x / 2f, mapSize.y / 2f, 0);
        rooms = new Dictionary<int, RoomData>();
        leadingKey = 0;
        lowestEmptyRowIndex = 0;
    }

    // actions
    public void SetNodeFilled(Vector2Int nodePos, bool filled) {
        SetNodeFilled(nodePos.x, nodePos.y, filled);
    }
    public void SetNodeFilled (int x, int y, bool filled) {
        nodes[x, y] = filled ? -2 : -1;
    }

    public void AssignNodeToRoom (Vector2Int nodePos, int roomKey, bool edge) {
        nodes[nodePos.x, nodePos.y] = roomKey;
        rooms[roomKey].tiles.Add(nodePos);
        if (edge) rooms[roomKey].edgeTiles.Add(nodePos);
    }

    public void ReassignEdgePointsToRoom (int roomKey, List<List<Vector2>> edgePoints) {
        rooms[roomKey].edgePoints.Clear();
        foreach (var edge in edgePoints) {
            rooms[roomKey].edgePoints.Add(edge);
        }
    }

    public void RemoveRoom (int roomKey) {
        rooms.Remove(roomKey);
    }
    public int CreateRoom () {
        int newKey = leadingKey++;
        rooms.Add(newKey, new RoomData(new List<Vector2Int>(), new List<Vector2Int>(), new List<List<Vector2>>()));
        return newKey;
    }

    // helpers
    public Vector3 NodePosToWorldPos (Vector2Int nodePos) {
        return topLeftPos + new Vector3(nodePos.x, -nodePos.y, 0);
    }
    public Vector3 NodePosToWorldPos(Vector2 nodePos) {
        return topLeftPos + new Vector3(nodePos.x, -nodePos.y, 0);
    }

    public Vector2Int WorldPosToTilePos (Vector3 worldPos) {
        worldPos -= topLeftPos;
        int x = Mathf.RoundToInt(worldPos.x);
        int y = -Mathf.RoundToInt(worldPos.y);

        x = Mathf.Clamp(x, 0, mapSize.x - 1);
        y = Mathf.Clamp(y, 0, mapSize.y - 1);

        return new Vector2Int(x, y);
    }

    public bool NodePosInMap (Vector2Int nodePos) {
        if (nodePos.x < 0 || nodePos.y < 0 || nodePos.x > mapSize.x - 1 || nodePos.y > mapSize.y - 1) return false;
        return true;
    }

    public bool GetNodeFilled(Vector2Int nodePos) {
        return GetNodeFilled(nodePos.x, nodePos.y);
    }
    public bool GetNodeFilled (int x, int y) {
        return nodes[x, y] == -2;
    }
    public bool GetNodeInMapAndFilled(Vector2Int nodePos) {
        return GetNodeInMapAndFilled(nodePos.x, nodePos.y);
    }
    public bool GetNodeInMapAndFilled(int x, int y) {
        if (!NodePosInMap(new Vector2Int(x, y))) return false;
        return nodes[x, y] == -2;
    }
    public bool GetNodeAssignedToRoom(Vector2Int nodePos) {
        return GetNodeAssignedToRoom(nodePos.x, nodePos.y);
    }
    public bool GetNodeAssignedToRoom(int x, int y) {
        return nodes[x, y] >= 0;
    }

    public bool ShouldBeEdge (Vector2Int nodePos) {
        int x = nodePos.x;
        int y = nodePos.y;
        if (GetNodeInMapAndFilled(x, y - 1) || GetNodeInMapAndFilled(x + 1, y) || GetNodeInMapAndFilled(x, y + 1) || GetNodeInMapAndFilled(x - 1, y)) return true;
        //if (GetNodeFilled(x, y - 1) || GetNodeFilled(x + 1, y - 1) || GetNodeFilled(x + 1, y) || GetNodeFilled(x + 1, y + 1) || GetNodeFilled(x, y + 1) || GetNodeFilled(x - 1, y + 1) || GetNodeFilled(x - 1, y) || GetNodeFilled(x - 1, y - 1)) return true;
        return false;
    }
    public bool GetNodeIsEdge (Vector2Int nodePos) {
        if (!GetNodeAssignedToRoom(nodePos)) return false;
        if (rooms[nodes[nodePos.x, nodePos.y]].edgeTiles.Contains(nodePos)) return true;
        return false;
    }
}

public struct RoomData {
    public List<Vector2Int> tiles;
    public List<Vector2Int> edgeTiles; // tiles with at lest 1/8 surrounding tiles filled
    public List<List<Vector2>> edgePoints; // on grid, but not ints bc they are at 0.5 values

    public RoomData(List<Vector2Int> tiles, List<Vector2Int> edgeTiles, List<List<Vector2>> edgePoints) {
        this.tiles = tiles;
        this.edgeTiles = edgeTiles;
        this.edgePoints = edgePoints;
    }
}

public class Map : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;

    MapData data;

    MapRenderer mapRenderer;
    MapCollider mapCollider;

    private void Start() {
        mapRenderer = GetComponent<MapRenderer>();
        mapCollider = GetComponent<MapCollider>();

        data = MapGenerator.GenerateMap(mapWidth, mapHeight);
        InitializeNodes();

        mapRenderer.SetMap(ref data);
        mapCollider.SetMap(ref data);
    }

    public Vector2Int WorldPosToTilePos (Vector3 worldPos) {
        return data.WorldPosToTilePos(worldPos);
    }

    // NODE INITIALIZATION
    void InitializeNodes () {
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                if (data.nodes[x, y] == -2) {
                    FillTile(new Vector2Int(x, y));
                } else {
                    EmptyTile(new Vector2Int(x, y));
                }
            }
        }
        UpdateRooms();
    }

    // NODE ALTERATION
    public void AlterNodes (ref NodeAlteration alteration) {
        ApplyAlteration(ref alteration);
    }

    void AlterNode (Vector2Int pos, bool newState) {
        if (data.GetNodeFilled(pos) == newState) return;
        if (newState) {
            FillTile(pos);
        } else {
            EmptyTile(pos);
        }
    }

    void FillTile (Vector2Int pos) {
        // if this tile is assigned to a room, remove it from the room
        // if there are no tiles left in the room, remove the room
        int roomKey = data.nodes[pos.x, pos.y];
        if (roomKey >= 0) {
            // this tile belongs to a room
            data.rooms[roomKey].tiles.Remove(pos);
            if (data.rooms[roomKey].tiles.Count == 0) {
                // the room has no remaining times
                data.RemoveRoom(roomKey);
            }
        }

        data.SetNodeFilled(pos, true);
    }

    void EmptyTile (Vector2Int pos) {
        data.SetNodeFilled(pos, false);
        newlyEmptyTiles.Add(pos);
    }

    void ApplyAlteration (ref NodeAlteration alteration) {
        int xLen = alteration.alterations.GetLength(0);
        int yLen = alteration.alterations.GetLength(1);
        for (int y = 0; y < yLen; y++) {
            for (int x = 0; x < xLen; x++) {
                bool alterAtPos = alteration.alterations[x, y]; 
                if (alterAtPos) {
                    Vector2Int pos = alteration.topLeftNodePos + new Vector2Int(x, y);
                    if (data.NodePosInMap(pos)) {
                        //data.nodes[pos.x, pos.y] = alteration.subtract ? !alterAtPos : alterAtPos;
                        AlterNode(pos, alteration.subtract ? !alterAtPos : alterAtPos);
                    }
                }
            }
        }
        int startX = alteration.topLeftNodePos.x - 1;
        int startY = alteration.topLeftNodePos.y - 1;
        int endX = startX + xLen + 2;
        int endY = startY + yLen + 2;

        UpdateRooms();

        mapRenderer.UpdateNodes(ref data, startX, endX, startY, endY);
    }

    public struct NodeAlteration {
        public Vector2Int topLeftNodePos;
        public bool[,] alterations;
        public bool subtract; // if we are subtracting, alterations with true will subtract

        public NodeAlteration(Vector2Int topLeftNodePos, bool[,] alterations, bool subtract) {
            this.topLeftNodePos = topLeftNodePos;
            this.alterations = alterations;
            this.subtract = subtract;
        }
    }

    // ROOM CALCULATIONS
    List<Vector2Int> newlyEmptyTiles = new List<Vector2Int>(); // all the tiles that have switched from filled to empty and have not been sorted into existing rooms / new rooms

    void UpdateRooms () {
        int processedNewlyEmptyTiles = 0;
        int floodRuns = 0;
        int edgeRecalculations = 0;
        int edgePathIterations = 0;

        // check the newly empty tiles to add them to existing rooms or make new ones
        foreach (var tile in newlyEmptyTiles) {
            // first check to make sure this tile is unassigned to a room
            if (!data.GetNodeAssignedToRoom(tile)) {
                processedNewlyEmptyTiles++;

                // flood fill from this tile until the flood completes and make a new room
                List<Vector2Int> newRoomTiles = new List<Vector2Int>();
                List<Vector2Int> floodQueTiles = new List<Vector2Int>();
                List<int> connectedRoomKeys = new List<int>(); // the indexes of any rooms which were found connected to the new room. These will all be combined into one room later
                floodQueTiles.Add(tile);
                while (true) {
                    floodRuns++;

                    // add the first tile from the que to the room list (map data not altered yet)
                    newRoomTiles.Add(floodQueTiles[0]);
                    // process all tiles around the new room tile
                    ProcessTile(floodQueTiles[0] + Vector2Int.up, ref newRoomTiles, ref floodQueTiles, ref connectedRoomKeys);
                    ProcessTile(floodQueTiles[0] + Vector2Int.right, ref newRoomTiles, ref floodQueTiles, ref connectedRoomKeys);
                    ProcessTile(floodQueTiles[0] + Vector2Int.down, ref newRoomTiles, ref floodQueTiles, ref connectedRoomKeys);
                    ProcessTile(floodQueTiles[0] + Vector2Int.left, ref newRoomTiles, ref floodQueTiles, ref connectedRoomKeys);

                    floodQueTiles.RemoveAt(0);
                    if (floodQueTiles.Count == 0) break;
                }
                
                // now we have to either create a new room, or combine ourselves with other rooms
                // either way we are reassigning tiles to a room key
                int roomKey;
                if (connectedRoomKeys.Count == 0) {
                    // create a new room
                    roomKey = data.CreateRoom();
                } else {
                    // we are connecting multiple rooms. 
                    // first find the largest room. We will merge all the other rooms including our new room tiles into this one
                    int mostTiles = 0;
                    roomKey = 0;
                    foreach (var connectedRoomKey in connectedRoomKeys) {
                        int newCount = data.rooms[connectedRoomKey].tiles.Count;
                        if (newCount > mostTiles) {
                            mostTiles = newCount;
                            roomKey = connectedRoomKey;
                        }
                    }
                    //print("Merging " + (connectedRoomKeys.Count - 1) + " existing rooms and 1 new room into room: " + roomKey);
                    // now that we have the largest room, add the tiles from all other rooms into our new room tiles list
                    foreach (var connectedRoomKey in connectedRoomKeys) {
                        if (connectedRoomKey != roomKey) { // ignore the room we are merging into
                            foreach (var roomTile in data.rooms[connectedRoomKey].tiles) {
                                newRoomTiles.Add(roomTile);
                            }
                            // then delete the room
                            data.RemoveRoom(connectedRoomKey);
                        }
                    }

                    // update the edges of the largest room
                    // we will only be removing edge tiles, bc you cannot have more than we started
                    for (int i = data.rooms[roomKey].edgeTiles.Count - 1; i >= 0; i--) {
                        edgeRecalculations++;
                        if (!data.ShouldBeEdge(data.rooms[roomKey].edgeTiles[i])) data.rooms[roomKey].edgeTiles.RemoveAt(i);
                    }
                }

                foreach (var newTile in newRoomTiles) {
                    // recalculate edges
                    bool edge = data.ShouldBeEdge(newTile);
                    data.AssignNodeToRoom(newTile, roomKey, edge);
                }

                // now calculate the edge points based on the edge tiles
                List<Vector2Int> remainingEdgeTiles = new List<Vector2Int>(data.rooms[roomKey].edgeTiles);
                List<List<Vector2>> allEdges = new List<List<Vector2>>();
                int escape = 3000;
                while (remainingEdgeTiles.Count > 0) {
                    if (escape-- < 0) {
                        Debug.LogError("Infinite loop when calculating edge points for room: " + roomKey);
                        break;
                    }


                    List<Vector2Int> nodesInNewEdge = new List<Vector2Int>();
                    List<List<Vector2>> newPaths = CalculateEdgePoints(remainingEdgeTiles[0], ref nodesInNewEdge, out int edgeCalculationIterations);

                    edgePathIterations += edgeCalculationIterations;

                    // then remove all overlap
                    foreach (var nodePos in nodesInNewEdge) {
                        remainingEdgeTiles.Remove(nodePos);
                    }
                    // then add new edge to list
                    foreach (var edge in newPaths) {
                        allEdges.Add(edge);
                    }
                }
                //Debug.Log("Calculated " + allEdges.Count + " edges for room: " + roomKey);

                // reassign the edges to the room
                data.ReassignEdgePointsToRoom(roomKey, allEdges);
            }
        }

        //Debug.Log("Updated rooms. Processed " + processedNewlyEmptyTiles + "/" + newlyEmptyTiles.Count + 
        //    " | " + floodRuns + " flooded tiles | " + edgeRecalculations + " edge node recalculations | " + edgePathIterations + " edge path iterations");

        newlyEmptyTiles.Clear();

        mapCollider.UpdateEdges(ref data);
    }

    void ProcessTile (Vector2Int tilePos, ref List<Vector2Int> newRoomTiles, ref List<Vector2Int> floodQueTiles, ref List<int> connectedRoomKeys) {
        // make sure the tile is in the map. If its not do nothing
        if (!data.NodePosInMap(tilePos)) return;
        // then make sure this tile is not in in the que or new room tiles
        if (floodQueTiles.Contains(tilePos) || newRoomTiles.Contains(tilePos)) return;

        int tileID = data.nodes[tilePos.x, tilePos.y];
        // if the tile ID is -2 (filled) ignore it
        // if the tile ID is -1 (empty and unassigned) add it to the que to be in this new room
        // if the tile is >= 0 (empty and assigned) add its room to the connected room keys
        if (tileID == -2) return;
        if (tileID == -1) {
            floodQueTiles.Add(tilePos);
        } else {
            if (!connectedRoomKeys.Contains(tileID)) connectedRoomKeys.Add(tileID); // add up to one instance
        }
    }

    // returns all the points along a circular walk of the room starting at a given position on the edge of the room
    List<List<Vector2>> CalculateEdgePoints (Vector2Int startPos, ref List<Vector2Int> includedEdgeNodes, out int runIterations) {
        runIterations = 0;

        List<List<Vector2>> paths = new List<List<Vector2>> {
            new List<Vector2>() // create the first path
        };
        int pathIndex = 0;

        includedEdgeNodes.Add(startPos);

        // calculate our starting direction which should be the direction pointing towards any neighboring filled tile
        int startDir = 0;
        while (!data.GetNodeInMapAndFilled(startPos + GetDirFromDirectionIndex(startDir))) {
            startDir++;
            if (startDir > 3) {
                Debug.LogError("Edge point has no neigboring filled nodes");
                return paths;
            }
        }
        bool lastPlacementDirAlligned = false;
        int lastPlacementDir = -1;

        int curDir = startDir;
        Vector2Int curPos = startPos;

        //print("start pos" + startPos + ", start dir: " + startDir);

        bool initial = true;
        bool previousWasOutsideMap = false;
        bool shouldBreak = false;
        while (runIterations++ < 1000 && !shouldBreak) {
            //print("pos: " + curPos + ", dir: " + curDir);
            if (initial) {
                initial = false;
            } else {
                shouldBreak = (curDir == startDir && curPos == startPos);
            }

            Vector2Int curDirVector = GetDirFromDirectionIndex(curDir);
            if (!data.NodePosInMap(curPos + curDirVector)) {
                // the cur node is outside the map
                if (!previousWasOutsideMap) {
                    // if the previous was in the map then we just hit an edge
                    // close off the prevous path
                    Vector2 mapPos = curPos + new Vector2(curDirVector.x / 2f, curDirVector.y / 2f);
                    paths[pathIndex].Add(data.NodePosToWorldPos(mapPos));
                    previousWasOutsideMap = true;
                }
                curDir = RotateClockwise(curDir);
            } else if (data.GetNodeFilled(curPos + curDirVector)) {
                if (previousWasOutsideMap) {
                    // we have to begin a new path
                    paths.Add(new List<Vector2>());
                    pathIndex++;
                    previousWasOutsideMap = false;

                    lastPlacementDirAlligned = false;
                    lastPlacementDir = -1;

                    // then rewind and add the previous point
                    int rewindDir = RotateCounterclockwise(curDir);
                    Vector2Int rewindDirVector = GetDirFromDirectionIndex(rewindDir);
                    Vector2 rewindPos = curPos + new Vector2(rewindDirVector.x / 2f, rewindDirVector.y / 2f);
                    paths[pathIndex].Add(data.NodePosToWorldPos(rewindPos));
                }

                // the node in direction is filled so we add the point halfway in this direction and rotate clockwise
                Vector2 mapPos = curPos + new Vector2(curDirVector.x / 2f, curDirVector.y / 2f);
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

                paths[pathIndex].Add(data.NodePosToWorldPos(mapPos));

                // special case for top of the map
                if (false && curDir == 0 && curPos.x == 0 && curPos.y == data.lowestEmptyRowIndex) {
                    // we are in the top left corner of the map facing left
                    // jump to the right side and point right
                    // need to add all the nodes along the top edge
                    curPos.x = data.mapSize.x - 1;
                    includedEdgeNodes.Add(curPos);
                    //while (curPos.x < data.mapSize.x - 1) {
                    //    curPos.x++;
                    //    includedEdgeNodes.Add(curPos);
                    //}
                } else {
                    // otherwise do as normal
                    curDir = RotateClockwise(curDir);
                }
            } else {
                // the node is empty so we should move in that direction and rotate counterclockwise
                curPos += curDirVector;
                curDir = RotateCounterclockwise(curDir);

                // every time we move we need to reset rotations in place and add the tile we moved onto to our list
                includedEdgeNodes.Add(curPos);
            }
        }

        return paths;
    }

    int RotateClockwise (int index) {
        index++;
        if (index > 3) index = 0;
        return index;
    }
    int RotateCounterclockwise (int index) {
        index--;
        if (index < 0) index = 3;
        return index;
    }

    Vector2Int GetDirFromDirectionIndex (int index) {
        if (index == 0) return Vector2Int.down;
        if (index == 1) return Vector2Int.right;
        if (index == 2) return Vector2Int.up;
        return Vector2Int.left;
    }
}
