using System.Collections.Generic;
using UnityEngine;

public enum Heuristic
{
    OCTILE = 0,
    CHEBYSHEV,
    INCONSISTENT,
    MANHATTAN,
    EUCLIDEAN,
    NUM_ENTRIES
}

public enum PathResult
{
    COMPLETE,
    PROCESSING,
    IMPOSSIBLE
}

public struct PathRequest
{
    public Vector3 start;
    public Vector3 goal;
    public List<Vector3> path;

    public Heuristic heuristic;
    public float weight;

    public bool smoothing;
    public bool rubberBanding;
    public bool singleStep;
    public bool debugColoring;
    public bool newRequest;
}

public class Astar
{
    private const int GRID_SIZE = 40;
    private const int CLUSTER_SIZE = 10;

    private Grid grid;

    private Node[,] nodes = new Node[GRID_SIZE, GRID_SIZE];

    private readonly List<Node> openHeap = new List<Node>(GRID_SIZE * GRID_SIZE);

    private GridPos startPos;
    private GridPos goalPos;

    public Astar(Grid gridRef)
    {
        grid = gridRef;
        Init();
    }

    public void Init()
    {
        openHeap.Clear();

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                nodes[row, col] = new Node();
                nodes[row, col].Init(row, col);
            }
        }
    }

    public void Shutdown()
    {
        openHeap.Clear();
    }

    private void Clear_Nodes()
    {
        openHeap.Clear();

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                nodes[row, col].Clear();
            }
        }
    }

    private bool Is_ValidGrid(GridPos pos)
    {
        return pos.row >= 0 && pos.col >= 0 &&pos.row < grid.max_row &&pos.col < grid.max_col;
    }

    private bool CanMove(GridPos from, GridPos to)
    {
        if (!Is_ValidGrid(to)) return false;

        if (grid.IsWall(to)) return false;

        int rowDiff = to.row - from.row;
        int colDiff = to.col - from.col;

        if (Mathf.Abs(rowDiff) == 1 && Mathf.Abs(colDiff) == 1)
        {
            GridPos sideA = new GridPos(from.row + rowDiff, from.col);
            GridPos sideB = new GridPos(from.row, from.col + colDiff);

            if (!Is_ValidGrid(sideA) || !Is_ValidGrid(sideB)) return false;

            if (grid.IsWall(sideA) || grid.IsWall(sideB)) return false;
        }

        return true;
    }

    private float CalculateHeuristic(GridPos a, GridPos b, Heuristic type)
    {
        float dx = Mathf.Abs(a.col - b.col);
        float dy = Mathf.Abs(a.row - b.row);

        switch (type)
        {
            case Heuristic.MANHATTAN:
                return dx + dy;

            case Heuristic.EUCLIDEAN:
                return Mathf.Sqrt(dx * dx + dy * dy);

            case Heuristic.CHEBYSHEV:
                return Mathf.Max(dx, dy);

            case Heuristic.OCTILE:
                return Mathf.Min(dx, dy) * Mathf.Sqrt(2f) + Mathf.Max(dx, dy) - Mathf.Min(dx, dy);

            case Heuristic.INCONSISTENT:
                if ((a.row + a.col) % 2 != 0) return Mathf.Sqrt(dx * dx + dy * dy);

                return 0.0f;

            default:
                return 0.0f;
        }
    }

    private float MovementCost(GridPos a, GridPos b)
    {
        if (Mathf.Abs(a.row - b.row) == 1 && Mathf.Abs(a.col - b.col) == 1)
        {
            return Mathf.Sqrt(2.0f);
        }

        return 1.0f;
    }

    private bool IsCheaper(Node a, Node b)
    {
        if (a.finalCost < b.finalCost) return true;

        if (Mathf.Approximately(a.finalCost, b.finalCost) && a.givenCost > b.givenCost) return true;

        return false;
    }

    private void SwapHeap(int a, int b)
    {
        Node temp = openHeap[a];

        openHeap[a] = openHeap[b];
        openHeap[b] = temp;

        openHeap[a].heapIndex = a;
        openHeap[b].heapIndex = b;
    }

    private void HeapifyUp(Node node)
    {
        int index = node.heapIndex;

        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (!IsCheaper(openHeap[index], openHeap[parentIndex]))
                break;

            SwapHeap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void HeapifyDown(Node node)
    {
        int index = node.heapIndex;

        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int cheapest = index;

            if (left < openHeap.Count &&
                IsCheaper(openHeap[left], openHeap[cheapest]))
            {
                cheapest = left;
            }

            if (right < openHeap.Count &&
                IsCheaper(openHeap[right], openHeap[cheapest]))
            {
                cheapest = right;
            }

            if (cheapest == index)
                break;

            SwapHeap(index, cheapest);
            index = cheapest;
        }
    }

    private void Push(Node node)
    {
        node.nodeState = NodeState.OPEN;
        node.heapIndex = openHeap.Count;

        openHeap.Add(node);
        HeapifyUp(node);
    }

    private Node PopCheapestNode()
    {
        if (openHeap.Count == 0)
            return null;

        Node cheapest = openHeap[0];

        Node last = openHeap[openHeap.Count - 1];
        openHeap.RemoveAt(openHeap.Count - 1);

        if (openHeap.Count > 0)
        {
            openHeap[0] = last;
            openHeap[0].heapIndex = 0;
            HeapifyDown(openHeap[0]);
        }

        cheapest.heapIndex = -1;

        return cheapest;
    }

    private void UpdateHeap(Node node)
    {
        HeapifyUp(node);
    }

    private GridPos GetClusterCenter(GridPos pos)
    {
        int clusterRow = pos.row / CLUSTER_SIZE;
        int clusterCol = pos.col / CLUSTER_SIZE;

        int centerRow = clusterRow * CLUSTER_SIZE + CLUSTER_SIZE / 2;
        int centerCol = clusterCol * CLUSTER_SIZE + CLUSTER_SIZE / 2;

        centerRow = Mathf.Clamp(centerRow, 0, grid.max_row - 1);
        centerCol = Mathf.Clamp(centerCol, 0, grid.max_col - 1);

        return new GridPos(centerRow, centerCol);
    }

    private void BuildPath(Node goalNode, ref PathRequest request)
    {
        request.path.Clear();

        List<Vector3> reversedPath = new List<Vector3>();

        Node current = goalNode;

        while (current != null)
        {
            reversedPath.Add(grid.Get_WorldPos(current.gridPos));
            current = current.parent;
        }

        for (int i = reversedPath.Count - 1; i >= 0; i--)
        {
            request.path.Add(reversedPath[i]);
        }

        if (request.rubberBanding)
        {
            RubberbandPath(ref request);
        }

        if (request.smoothing)
        {
            SmoothPath(ref request);
        }
    }

    private void RubberbandPath(ref PathRequest request)
    {
        if (request.path.Count < 3) return;

        int current = 0;
        List<Vector3> path = new List<Vector3>(request.path);

        while (current + 2 < path.Count)
        {
            GridPos a = grid.Get_GridPos(path[current]);
            GridPos c = grid.Get_GridPos(path[current + 2]);

            bool clear = true;

            int minRow = Mathf.Min(a.row, c.row);
            int maxRow = Mathf.Max(a.row, c.row);

            int minCol = Mathf.Min(a.col, c.col);
            int maxCol = Mathf.Max(a.col, c.col);

            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minCol; col <= maxCol; col++)
                {
                    GridPos check = new GridPos(row, col);

                    if (!Is_ValidGrid(check) || grid.IsWall(check))
                    {
                        clear = false;
                        break;
                    }
                }

                if (!clear)
                    break;
            }

            if (clear)
                path.RemoveAt(current + 1);
            else
                current++;
        }

        request.path.Clear();

        foreach (Vector3 p in path)
        {
            request.path.Add(p);
        }
    }

    private void SmoothPath(ref PathRequest request)
    {
        if (request.path.Count < 2) return;

        List<Vector3> points = new List<Vector3>(request.path);

        bool added = true;

        while (added)
        {
            added = false;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];

                float dist = Vector3.Distance(a, b);

                if (dist > 1.5f)
                {
                    Vector3 mid = (a + b) * 0.5f;
                    points.Insert(i + 1, mid);

                    added = true;
                    break;
                }
            }
        }

        List<Vector3> controlPoints = new List<Vector3>();

        controlPoints.Add(points[0]);

        foreach (Vector3 p in points) controlPoints.Add(p);

        controlPoints.Add(points[points.Count - 1]);

        List<Vector3> smoothed = new List<Vector3>();

        smoothed.Add(points[0]);

        float[] scale = { 0.25f, 0.5f, 0.75f };

        for (int i = 0; i + 3 < controlPoints.Count; i++)
        {
            foreach (float s in scale)
            {
                float s2 = s * s;
                float s3 = s2 * s;

                Vector3 result = controlPoints[i] * (-0.5f * s3 + s2 - 0.5f * s) + controlPoints[i + 1] * (1.5f * s3 - 2.5f * s2 + 1.0f) + controlPoints[i + 2] *
                    (-1.5f * s3 + 2.0f * s2 + 0.5f * s) + controlPoints[i + 3] *(0.5f * s3 - 0.5f * s2);

                smoothed.Add(result);
            }
        }

        smoothed.Add(points[points.Count - 1]);

        request.path.Clear();

        foreach (Vector3 p in smoothed)
        {
            request.path.Add(p);
        }
    }

    public PathResult ComputePath(ref PathRequest request)
    {
        if (request.newRequest)
        {
            Clear_Nodes();

            startPos = grid.Get_GridPos(request.start);
            goalPos = grid.Get_GridPos(request.goal);

            if (!Is_ValidGrid(startPos) || !Is_ValidGrid(goalPos)) return PathResult.IMPOSSIBLE;

            if (grid.IsWall(startPos) || grid.IsWall(goalPos)) return PathResult.IMPOSSIBLE;

            GridPos startCluster = GetClusterCenter(startPos);
            GridPos goalCluster = GetClusterCenter(goalPos);

            Node startNode = nodes[startPos.row, startPos.col];

            startNode.parent = null;
            startNode.givenCost = 0f;
            startNode.finalCost = CalculateHeuristic(startPos, goalPos, request.heuristic) * request.weight;

            Push(startNode);
        }

        while (openHeap.Count > 0)
        {
            Node current = PopCheapestNode();

            if (current.gridPos == goalPos)
            {
                BuildPath(current, ref request);
                return PathResult.COMPLETE;
            }

            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                for (int colOffset = -1; colOffset <= 1; colOffset++)
                {
                    if (rowOffset == 0 && colOffset == 0)
                        continue;

                    GridPos neighbourPos = new GridPos(current.gridPos.row + rowOffset,current.gridPos.col + colOffset);

                    if (!CanMove(current.gridPos, neighbourPos)) continue;

                    Node neighbour = nodes[neighbourPos.row, neighbourPos.col];

                    float givenCost =current.givenCost + MovementCost(current.gridPos, neighbourPos);

                    if (neighbour.nodeState == NodeState.NONE || givenCost < neighbour.givenCost)
                    {
                        neighbour.parent = current;
                        neighbour.givenCost = givenCost;

                        neighbour.finalCost = givenCost + CalculateHeuristic(neighbourPos, goalPos,request.heuristic) *request.weight;

                        if (neighbour.nodeState == NodeState.OPEN)
                        {
                            UpdateHeap(neighbour);
                        }
                        else
                        {
                            Push(neighbour);
                        }
                    }
                }
            }

            current.nodeState = NodeState.CLOSED;

            if (request.singleStep) return PathResult.PROCESSING;
        }

        return PathResult.IMPOSSIBLE;
    }
}