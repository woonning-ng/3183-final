using UnityEngine;
using static Astar;


public enum NodeState
{
    NONE = 0,
    OPEN,
    CLOSED
}

public class Node
{
    //
    public GridPos gridPos;
    public Node parent;

    //used to store the cost for pathfinding 
    public float givenCost;
    public float finalCost;
    //stores the type of terrain for the node which is used for the weights in pathfinding which would affect the costs
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