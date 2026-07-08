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
    public GridPos gridPos;
    public Node parent;

    public float givenCost;
    public float finalCost;

    public NodeState nodeState;

    // Binary heap
    public int heapIndex;

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