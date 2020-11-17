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

    int leadingKey;
    public Dictionary<int, RoomData> rooms;

    public MapData(Vector2Int mapSize, int[,] nodes) {
        this.mapSize = mapSize;
        this.nodes = nodes;
        topLeftPos = new Vector3(-mapSize.x / 2f, mapSize.y / 2f, 0);
        rooms = new Dictionary<int, RoomData>();
        leadingKey = 0;
    }

    // actions
    public void SetNodeFilled(Vector2Int nodePos, bool filled) {
        SetNodeFilled(nodePos.x, nodePos.y, filled);
    }
    public void SetNodeFilled (int x, int y, bool filled) {
        nodes[x, y] = filled ? -2 : -1;
    }

    public void AssignNodeToRoom (Vector2Int nodePos, int roomKey) {
        nodes[nodePos.x, nodePos.y] = roomKey;
        rooms[roomKey].tiles.Add(nodePos);
    }

    public void RemoveRoom (int roomKey) {
        rooms.Remove(roomKey);
    }
    public int CreateRoom () {
        int newKey = leadingKey++;
        rooms.Add(newKey, new RoomData(new List<Vector2Int>(), new List<Vector2>()));
        return newKey;
    }

    // helpers
    public Vector3 NodePosToWorldPos (Vector2Int nodePos) {
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
    public bool GetNodeAssignedToRoom(Vector2Int nodePos) {
        return GetNodeAssignedToRoom(nodePos.x, nodePos.y);
    }
    public bool GetNodeAssignedToRoom(int x, int y) {
        return nodes[x, y] >= 0;
    }
}

public struct RoomData {
    public List<Vector2Int> tiles;
    public List<Vector2> edgePoints; // on grid, but not ints bc they are at 0.5 values

    public RoomData(List<Vector2Int> tiles, List<Vector2> edgePoints) {
        this.tiles = tiles;
        this.edgePoints = edgePoints;
    }
}

public class Map : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;

    MapData data;

    MapRenderer mapRenderer;

    private void Start() {
        mapRenderer = GetComponent<MapRenderer>();

        data = MapGenerator.GenerateMap(mapWidth, mapHeight);

        mapRenderer.SetMap(ref data);
    }

    public Vector2Int WorldPosToTilePos (Vector3 worldPos) {
        return data.WorldPosToTilePos(worldPos);
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
        // check the newly empty tiles to add them to existing rooms or make new ones
        foreach (var tile in newlyEmptyTiles) {
            // first check to make sure this tile is unassigned to a room
            if (!data.GetNodeAssignedToRoom(tile)) {
                // flood fill from this tile until the flood completes and make a new room
                List<Vector2Int> newRoomTiles = new List<Vector2Int>();
                List<Vector2Int> floodQueTiles = new List<Vector2Int>();
                List<int> connectedRoomKeys = new List<int>(); // the indexes of any rooms which were found connected to the new room. These will all be combined into one room later
                floodQueTiles.Add(tile);
                while (true) {
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
                    //Debug.Log("Merging " + (connectedRoomKeys.Count - 1) + " existing rooms and 1 new room into room: " + roomKey);
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
                }

                foreach (var newTile in newRoomTiles) {
                    data.AssignNodeToRoom(newTile, roomKey);
                    //data.nodes[newTile.x, newTile.y] = roomKey;
                    //data.rooms[roomKey].tiles.Add(newTile);
                }
            }
        }

        newlyEmptyTiles.Clear();

        // UNOPTIMIZED
        // recalculate the edges of all rooms
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
}
