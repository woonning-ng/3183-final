using UnityEngine;
using static Astar;


public enum NodeState
{
    NONE = 0,
    OPEN,
    CLOSED
}
public enum NodeType {
    UNASSIGNED = 0,
    WATER,
    SAND,
    GRASS,
    SNOW
}
public class Node
{
    public GridPos gridPos;
    public Node parent;

    public float givenCost;
    public float finalCost;
    public Terrain_cus terrainType;

    public NodeState nodeState;

    // Binary heap
    public int heapIndex;

    public Node(Terrain_cus ndType)
    {
        this.terrainType= ndType;
    }
     
    public void Init(int row, int col)
    {
        gridPos = new GridPos(row, col);
        Clear();
    }

    public void Clear()
    {
        parent = null;
        givenCost = float.MaxValue;
        finalCost = float.MaxValue;
        nodeState = NodeState.NONE;
        heapIndex = -1;
    }
}