using UnityEngine;
public struct GridPos
{
    public int row;
    public int col;

    public GridPos(int r, int c)
    {
        row = r;
        col = c;
    }

    public static bool operator ==(GridPos a, GridPos b)
    {
        return a.row == b.row && a.col == b.col;
    }

    public static bool operator !=(GridPos a, GridPos b)
    {
        return !(a == b);
    }

}

public class Grid : MonoBehaviour
{
    [Header("Grid Size")]
    public int max_row = 128;
    public int max_col = 128;

    [Header("World Size")]
    public float blockSize = 1f;
    private float mapSizeInWorld => max_row * blockSize; // assumes square grid

    private Vector3[,] positions;
    private bool[,] walls;

    public static Grid Instance;
    public TerrainGen terrainGen; // needed for querying

    private void Awake()
    {
        Instance = this;

        positions = new Vector3[max_row, max_col];
        walls = new bool[max_row, max_col];

        GeneratePositions();
    }

    private void Start()
    {
        // set the walls here
        for (int row = 0; row < max_row; row++)
        {

            for (int col = 0; col < max_col; col++)
            {
                SetWall(row, col, !terrainGen.GetAccessible(row, col)); // invert condition
            }
        }
    }

    public void GeneratePositions()
    {
        float offset = mapSizeInWorld / max_row * 0.5f;

        float xOffset = mapSizeInWorld / max_row;
        float zOffset = mapSizeInWorld / max_col;

        for (int row = 0; row < max_row; row++)
        {
            float x = row * xOffset;

            for (int col = 0; col < max_col; col++)
            {
                float z = col * zOffset;

                positions[row, col] = new Vector3( x + offset, 0.0f, z + offset);
            }
        }
    }

    public Vector3 Get_WorldPos(int row, int col)
    {
        return positions[row, col];
    }

    public Vector3 Get_WorldPos(GridPos pos)
    {
        return positions[pos.row, pos.col];
    }

    public GridPos Get_GridPos(Vector3 worldPos)
    {
        int row = Mathf.Clamp((int)(worldPos.x / mapSizeInWorld * max_row),0, max_row - 1);

        int col = Mathf.Clamp((int)(worldPos.z / mapSizeInWorld * max_col), 0, max_col - 1);

        return new GridPos(row, col);
    }

    public bool IsWall(GridPos pos)
    {
        return walls[pos.row, pos.col];
    }

    public bool IsWall(int row, int col)
    {
        return walls[row, col];
    }

    public void SetWall(int row, int col, bool wall)
    {
        walls[row, col] = wall;
    }

    public bool IsValid(GridPos pos)
    {
        return pos.row >= 0 && pos.col >= 0 && pos.row < max_row && pos.col < max_col;
    }

    public void ClearWalls()
    {
        for (int r = 0; r < max_row; r++)
        {
            for (int c = 0; c < max_col; c++)
            {
                walls[r, c] = false;
            }
        }
    }

    Vector3 TryGetValidSpawn() {
        for (int i = 0; i < 100; i++) { 
            
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (positions == null)
            return;

        Gizmos.color = Color.gray;

        for (int r = 0; r < max_row; r++)
        {
            for (int c = 0; c < max_col; c++)
            {
                Gizmos.DrawWireCube(positions[r, c], Vector3.one * (mapSizeInWorld / max_row));
            }
        }
    }
}

