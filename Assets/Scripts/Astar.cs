using System.Collections.Generic;
using System.Data;
using UnityEngine;

public enum Heuristic
{
    OCTILE = 0,
    CHEBYSHEV,
    MANHATTAN,
    EUCLIDEAN
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
    public bool newRequest;
}

public class Astar
{

    public Grid grid;

    private Node[,] nodes = new Node[GlobalParam.max_width, GlobalParam.max_height];

    private readonly List<Node> openHeap = new List<Node>(GlobalParam.max_width * GlobalParam.max_height);

    private GridPos startPos;
    private GridPos goalPos;

   
    public Astar(Grid gridRef)
    {
        //ensure that the grid used is the same as the one in TerrainGen to avoid multiple copies of data
        grid = gridRef;
        Init();
    }

    public void Init()
    {
        //Clear the heap before use
        openHeap.Clear();

        //loop thru the grid and initialize the nodes and assign their positions
        for (int row = 0; row < GlobalParam.max_height; row++)
        {
            for (int col = 0; col < GlobalParam.max_width; col++)
            {
                
                nodes[row, col] = new Node(GlobalParam.terrain_map[row, col]);
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

        //loop through grid and clear all the nodes
        for (int row = 0; row < GlobalParam.max_height; row++)
        {
            for (int col = 0; col < GlobalParam.max_width; col++)
            {
                nodes[row, col].Clear();
                
            }
        }
    }

    private bool Is_ValidGrid(GridPos pos)
    {
        // checks if the position passed is within the grid boundaries
        return pos.row >= 0 && pos.col >= 0 && pos.row < grid.max_row && pos.col < grid.max_col;
    }

    private bool CanMove(GridPos start, GridPos end)
    {
        //Check if valid grid pos
        if (!Is_ValidGrid(start))
        {
            return false;
        }
        //ensure that player doesnt end up in walls
        if (grid.IsWall(end))
        {
            return false;
        }

        int rowDiff = end.row - start.row;
        int colDiff = end.col - start.col;

        //check if the target is a diagonal cell 
        if (Mathf.Abs(rowDiff) == 1 && Mathf.Abs(colDiff) == 1)
        {
            
            GridPos sideA = new GridPos(start.row + rowDiff, start.col);
            GridPos sideB = new GridPos(start.row, start.col + colDiff);

            //check if these cells are valid and prevent moving in between walls
            if (!Is_ValidGrid(sideA) || !Is_ValidGrid(sideB) || grid.IsWall(sideA) || grid.IsWall(sideB))
            {
                return false;
            }

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

            default:
                return 0.0f;
        }
    }

    private float MovementCost(GridPos a, GridPos b)
    {
        //if diagonal, then movement cost is 2.0f
        if (Mathf.Abs(a.row - b.row) == 1 && Mathf.Abs(a.col - b.col) == 1)
        {
            return Mathf.Sqrt(2.0f);
        }

        return 1.0f;
    }

    private bool IsCheaper(Node a, Node b)
    {
        //checks if the cost is cheaper
        if (a.finalCost < b.finalCost)
        {
            return true;
        }
        //compares the 2 final costs and checks if they have a really small difference making them effectively the same, then if so use the
        // given cost to check. if final cost is the same, the node with a larger given cost will have a smaller heuristic cost.
        if (Mathf.Approximately(a.finalCost, b.finalCost) && a.givenCost > b.givenCost)
        {
            return true;
        }

        return false;
    }

    private void SwapHeap(int a, int b)
    {
        //swaps the heap index and node information
        Node temp = openHeap[a];

        openHeap[a] = openHeap[b];
        openHeap[b] = temp;

        openHeap[a].heapIndex = a;
        openHeap[b].heapIndex = b;
    }

    private void HeapifyUp(int index)
    {
        //used to move the node upwards to the root, as we want the node with the smallest final cost to be at the root
        //loop throug the heap
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            //compare the final costs
            if (!IsCheaper(openHeap[index], openHeap[parentIndex]))
            {
                break;
            }
            //swap nodes
            SwapHeap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        //used after popping the root node. when the root node is popped, the last node in the tree is moved to the root node. it needs to be moved downwards.
        //heapifyup does not handle this, as the root node has no parent
        while (true)
        {

            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int cheapest = index;

            //check its children and swap
            if (left < openHeap.Count && IsCheaper(openHeap[left], openHeap[cheapest]))
            {
                cheapest = left;
            }

            if (right < openHeap.Count && IsCheaper(openHeap[right], openHeap[cheapest]))
            {
                cheapest = right;
            }

            if (cheapest == index)
            {
                break;
            }

            SwapHeap(index, cheapest);
            index = cheapest;
        }
    }

    private void Push(Node node)
    {
        //add the node to the heap
        node.nodeState = NodeState.OPEN;
        node.heapIndex = openHeap.Count;

        openHeap.Add(node);
        HeapifyUp(node.heapIndex);
    }

    private Node PopCheapestNode()
    {
        //used to remove the root which is the cheapest node
        if (openHeap.Count == 0)
        {
            return null;
        }

        Node cheapest = openHeap[0];

        //remove the last node of the heap
        Node last = openHeap[openHeap.Count - 1];
        openHeap.RemoveAt(openHeap.Count - 1);

        //put the last node at the root after checking that there is at least one node still in the heap which is the root node. Assign the
        // data of the last node to the root node
        if (openHeap.Count > 0)
        {
            openHeap[0] = last;
            openHeap[0].heapIndex = 0;
            HeapifyDown(openHeap[0].heapIndex);
        }

        cheapest.heapIndex = -1;

        return cheapest;
    }


    private void BuildPath(Node goalNode, ref PathRequest request)
    {
        //clear the path just in case
        request.path.Clear();

        List<Vector3> reversedPath = new List<Vector3>();

        //set the goal node as the head of the list
        Node current = goalNode;

        //reconstructing path by moving backwards
        while (current != null)
        {
            reversedPath.Add(grid.Get_WorldPos(current.gridPos));
            current = current.parent;
        }
        //add the reconstructed path to the new path.
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
        //need at least 3 points for rubberbanding
        if (request.path.Count < 3)
        {
            return;
        }
        int current = 0;
        List<Vector3> path = new List<Vector3>(request.path);

        //loop through the positions in the path until hit the last 3rd position to avoid the index exceeding the limit
        while (current + 2 < path.Count)
        {
            GridPos a = grid.Get_GridPos(path[current]);
            GridPos c = grid.Get_GridPos(path[current + 2]);

            bool clear = true;

            int minRow = Mathf.Min(a.row, c.row);
            int maxRow = Mathf.Max(a.row, c.row);

            int minCol = Mathf.Min(a.col, c.col);
            int maxCol = Mathf.Max(a.col, c.col);

            //loop through the grid positions between the 2 positions
            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minCol; col <= maxCol; col++)
                {

                    GridPos check = new GridPos(row, col);
                    //check if position is valid, if not break as the path is blocked
                    if (!Is_ValidGrid(check) || grid.IsWall(check))
                    {
                        clear = false;
                        break;
                    }
                }

                if (!clear)
                    break;
            }
            //if path is clear of obstacles then remove the position in the middle. If not, move onto the next position
            if (clear)
            {
                path.RemoveAt(current + 1);
            }
            else
            {
                current++;
            }
        }
        //clear the current path
        request.path.Clear();

        //add in the new path
        foreach (Vector3 p in path)
        {
            request.path.Add(p);
        }
    }

    private void SmoothPath(ref PathRequest request)
    {
        //if only 1 position, just return, no need smoothing
        if (request.path.Count < 2)
        {
            return;
        }

        List<Vector3> points = new List<Vector3>(request.path);

        bool added = true;

        //the 
        while (added)
        {
            added = false;
            //loop thru the path until the last element to avoid running over the buffer limit when calling the next
            //position in the path in this current iteration
            for (int i = 0; i < points.Count - 1; i++)
            {
                
                Vector3 a = points[i];
                Vector3 b = points[i + 1];

                //get the distance between the 2, if more than a certain distance, insert a point in the middle after the current position to make it 
                //smoother.
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

        //need to add the first and last position in the path 2 times each. TO calculate the curve between 2 points, we need 4 points, 1 before the 2 pts
        // and 1 after the 2 pts. This means that for the first 2 points and last 2 points, we need to duplicate the first and last point to ensure that
        //there are enough points to calculate the curve between the 2 points.
        controlPoints.Add(points[0]);

        foreach (Vector3 p in points)
        {
            controlPoints.Add(p);
        }

        controlPoints.Add(points[points.Count - 1]);

        List<Vector3> smoothed = new List<Vector3>();
        //the start and ending point are added twice to ensure that the path starts and ends at the correct points
        smoothed.Add(points[0]);

        //interpolation positions which are the positions along each spline segment
        float[] scale = { 0.25f, 0.5f, 0.75f };

        for (int i = 0; i + 3 < controlPoints.Count; i++)
        {
            foreach (float s in scale)
            {
                //calculate the powers until s^3 as the catmull-rom uses cubic polymials as it needs 4 control points
                float s2 = s * s;
                float s3 = s2 * s;
                //calculate the new smoothed point using the weights
                Vector3 result = controlPoints[i] * (-0.5f * s3 + s2 - 0.5f * s) 
                            + controlPoints[i + 1] * (1.5f * s3 - 2.5f * s2 + 1.0f) 
                            + controlPoints[i + 2] * (-1.5f * s3 + 2.0f * s2 + 0.5f * s) 
                            + controlPoints[i + 3] *(0.5f * s3 - 0.5f * s2);

                smoothed.Add(result);
            }
        }

        smoothed.Add(points[points.Count - 1]);

        request.path.Clear();
        //add into the path
        foreach (Vector3 p in smoothed)
        {
            request.path.Add(p);
        }
    }

    public PathResult ComputePath(ref PathRequest request)
    {
        //if the request is a new request, clear up the grid and check if the points are valid
        if (request.newRequest)
        {
            
            Clear_Nodes();

            startPos = grid.Get_GridPos(request.start);
            goalPos = grid.Get_GridPos(request.goal);

            if (!Is_ValidGrid(startPos) || !Is_ValidGrid(goalPos)) {
                return PathResult.IMPOSSIBLE;
            }

            if (grid.IsWall(startPos) || grid.IsWall(goalPos)) {
                return PathResult.IMPOSSIBLE;
            }

            Node startNode = nodes[startPos.row, startPos.col];

            startNode.parent = null;
            startNode.givenCost = 0f;
            startNode.finalCost = CalculateHeuristic(startPos, goalPos, request.heuristic) * request.weight;

            Push(startNode);
        }
        //loop through the heap storing the open nodes
        while (openHeap.Count > 0)
        {
            //pop the root which is node with the min final cost. In the first iteration of a new request, this would be the starting node
            Node current = PopCheapestNode();

            if (current.gridPos == goalPos)
            {
                BuildPath(current, ref request);
                return PathResult.COMPLETE;
            }

            //Check the neighbouring nodes
            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                for (int colOffset = -1; colOffset <= 1; colOffset++)
                {
                    //skip the node itself
                    if (rowOffset == 0 && colOffset == 0)
                    {
                        continue;
                    }

                    
                    GridPos neighbourPos = new GridPos(current.gridPos.row + rowOffset,current.gridPos.col + colOffset);

                    //check if the neighbour is accessible
                    if (!CanMove(current.gridPos, neighbourPos))
                    {
                        continue;
                    }

                    Node neighbour = nodes[neighbourPos.row, neighbourPos.col];
                    float weight;
                    //set the weight of the node based on the tile's biome type which would affect the cost later on
                    switch (nodes[neighbourPos.row, neighbourPos.col].terrainType)
                    {
                        case Terrain_cus.SAND:
                            weight = GlobalParam.m_sandWeight;
                            break;
                        case Terrain_cus.GRASS:
                            weight = GlobalParam.m_grassWeight;
                            break;
                        case Terrain_cus.SNOW:
                            weight = GlobalParam.m_snowWeight;
                            break;
                        default:
                            weight = 0.0f;
                            break;

                    }
                    
                    float givenCost =current.givenCost + MovementCost(current.gridPos, neighbourPos) * weight;
                    //if node state is not assigned yet that means the node has not been travelled to, or that the givencost of the node is smaller
                    // than the neighbor's meaning that a cheaper route has been found
                    if (neighbour.nodeState == NodeState.NONE || givenCost < neighbour.givenCost)
                    {
                        //change the neighbour's parent to the current node, used in reconstruction later, and update the costs
                        neighbour.parent = current;
                        neighbour.givenCost = givenCost;

                        neighbour.finalCost = givenCost + CalculateHeuristic(neighbourPos, goalPos, request.heuristic) * request.weight;
                        //update the heap if the neighbor is already travelled to 
                        if (neighbour.nodeState == NodeState.OPEN)
                        {
                            HeapifyUp(neighbour.heapIndex);
                        }
                        else
                        {
                            Push(neighbour);
                        }
                    }
                }
            }

            current.nodeState = NodeState.CLOSED;

            if (request.singleStep)
            {
                return PathResult.PROCESSING;
            }
        }

        return PathResult.IMPOSSIBLE;
    }
}