using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static GlobalParam;
using static PerlinNoise;
using static UnityEditor.PlayerSettings;


//References:
//https://www.redblobgames.com/maps/terrain-from-noise/
//https://amanpriyanshu.github.io/The-Mountains-of-Madness/
//https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Random.Range.html

public enum Terrain_cus
{
    WATER,
    SAND,
    GRASS,
    SNOW
}

public enum Terrain_obj
{
    SHELTER,
    TREE,
    NONE
}

//128 x 128 map size

public class TerrainGen : MonoBehaviour
{
    //this frequency is to make it more rough or smoother
    //The higher the frequency value is, the more rough the terrain would be
    //The lower frequency value would make the terrain more smooth
    //By that I meant the variety would be smaller if the frequency value is lower
    static float frequency = 3.0f;

    //Prefabs for instantianting terrains
    public GameObject water_prefab;
    public GameObject sand_prefab;
    public GameObject grass_prefab;
    public GameObject snow_prefab;
    public GameObject tree_prefab;
    public GameObject shelter_prefab;

    //For object placements
    int shelter_count = 0;
    static int max_shelter = 3;

    //2d array to store the perlin noise values
    //The 2d array is [x, z] and the float within is the y value
    //Y value have to be converted to int as we are going for minecraft style

    float[,] data = new float[GlobalParam.max_width, GlobalParam.max_height];
    //Moisture to help with biome determination
    float[,] moisture = new float[GlobalParam.max_width, GlobalParam.max_height];
    //TO check if the tile is walkable or have any blockage (like water and trees are not accessible)
    bool[,] accessible = new bool[GlobalParam.max_width, GlobalParam.max_height];
    //To check if the tile contains safehouses
    bool[,] safe = new bool[GlobalParam.max_width, GlobalParam.max_height];

    List<(int x, int z)> objects = new List<(int x, int z)> ();
    List<Vector2> objectsPlace = new List<Vector2>();

    

    public float GetY(float x, float z) {
        return (int)(data[(int)x, (int)z] * 10.0f);
    }

    public bool GetAccessible(float x, float z)
    {
        return accessible[(int)x, (int)z];
    }

    public bool GetSafe(float x, float z)
    {
        return safe[(int)x, (int)z];
    }

    public List<Vector3> GetAllSafe() {
        List<Vector3> safehouses = new List<Vector3>();

        for (int i = 0; i < max_height; i++)
        {
            for (int j = 0; j < max_width; j++)
            {
                if (GetSafe(i, j)) { safehouses.Add(new Vector3(i, GetY(i, j), j)); }
            }
        }

        return safehouses;
    }

    void GenerateRandomMap() {

        //for octaves
        float[] octaveOffset = new float[3];
        octaveOffset[0] = 1.0f;
        octaveOffset[1] = 0.5f;
        octaveOffset[2] = 0.25f;
        for (int i = 0; i < max_height; i++)
        {
            for (int j = 0; j < max_width; j++)
            {
                
                float nx = (float)j / max_width , ny = (float)i / max_height;
                //Frequency makes the terrain rougher or smoother
                if (GlobalParam.m_frequency)
                {
                    nx *= frequency;
                    ny *= frequency;
                }
                

                //Octave to make terrain more vary by adding noise in different frequencies
                if (GlobalParam.m_octave)
                {
                    float octaveTotal = 0.0f;

                    for (int k = 0; k < octaveOffset.Length; k++)
                    {
                        float octaveInv = 1.0f / octaveOffset[k];
                        //Rdige functions are use to amplify the heights
                        if (GlobalParam.m_ridge)
                        {
                            data[i, j] += octaveOffset[k] * Ridgenoise(nx * octaveInv, ny * octaveInv);
                        }
                        else
                        {
                            data[i, j] += octaveOffset[k] * Noise(nx * octaveInv, ny * octaveInv);

                        }
                            octaveTotal += octaveOffset[k];
                    }

                    data[i, j] /= octaveTotal;
                }
                else
                {
                    //Rdige functions are use to amplify the heights
                    if (GlobalParam.m_ridge)
                    {
                        data[i, j] = Ridgenoise(nx, ny);
                    }
                    else
                    {
                        data[i, j] = Noise(nx, ny);
                    }
                        
                }
                //moisture will just use the normal noise function as we do not need it to vary a lot
                moisture[i, j] = Noise(nx, ny);
                //set all to true first
                accessible[i, j] = true;
                //set all to false first
                safe[i, j] = false;
                
            }
        }

    }
    //Biome function will return the enum of the gameobject to spawn
    Terrain_cus Biome(float height, float moist){
        //Values can be customizable
        //-1 to 1 range

        if(height < -0.3f)
        {
            
            return Terrain_cus.WATER;
        }
        else if(height >= -0.3f && height < 0.5f)
        {
            if(moist <= -0.1f )
            {
                return Terrain_cus.SAND;
            }
        }
        else if(height >= 0.5f)
        {

            return Terrain_cus.SNOW;
;
        }

        
        
        return Terrain_cus.GRASS;
    }

    //Function for object placements 
    Terrain_obj Objects(float height, float moist)
    {
        //If the terrain is either sand or grass based on the Biome function
        if(height >= -0.3f && height < 0.5f)
        {
            if (moist>-0.1f)
            {
                //Using random function 
                int tree = UnityEngine.Random.Range(1, 10);
                if (tree == 2)
                {
                    
                    return Terrain_obj.TREE;
                }
                
            }
        }
        //Shelter generation using random 
        if(shelter_count < max_shelter && height >= -0.3f)
        {
            int shelter = UnityEngine.Random.Range(1, 1000);
            if (shelter == 5)
            {
                shelter_count++;
                return Terrain_obj.SHELTER;
            }
        }

        

        return Terrain_obj.NONE;
    }
    


    int[,] grid = new int[GlobalParam.max_width, GlobalParam.max_height];

    //Check for if the tile is valid to put a point for object there
    bool IsValid(int x, int z, ref int[,]grid, ref List<(int,int)> samples, ref int radius)
    {
        //Checking the neighbours to see if the point is within the distance 
        //If it is within the distance, return false to not add the point for spawning object
        //Else return true to allow the object to spawn
        for (int di = -2; di <= 2; di++)
        {
            for (int dj = -2; dj <= 2; dj++)
            {
                int ci = x + di;
                int cj = z + dj;
                if (ci < 0 || cj < 0 || ci >= GlobalParam.max_width || cj >= GlobalParam.max_height)
                {
                    continue;
                }
                //Getting the index
                int idx = grid[ci, cj];
                //If index is not a valid value, just continue
                if (idx == -1)
                {
                    continue;
                }

                (int dx, int dz) = samples[idx];

                dx -= x;
                dz -= z;
                //Distance check
                if (dx * dx + dz * dz < radius * radius)
                {
                    return false;
                }
            }
        }
        return true;
    }

    void PoissonDisk(int startx, int startz,int radius,int k = 30)
    {
        
        //The active list for the index that was added
        var active = new List<int>();


        void Add(int x, int z)
        {
            grid[x, z] = objects.Count;
            objects.Add((x, z));
            active.Add(objects.Count - 1);
        }

        //Early checks to ensure that the starting point is valid to add
        if (!accessible[startx, startz] || GlobalParam.terrain_map[startx, startz] == Terrain_cus.SAND 
            || GlobalParam.terrain_map[startx, startz] == Terrain_cus.SNOW || !IsValid(startx, startz, ref grid, ref objects, ref radius))
        {
            //Early exit if the point is not valid
            return;
        }

        Add(startx, startz);


        while (active.Count > 0)
        {
            //Randomly access the points in active 
            int ri = UnityEngine.Random.Range(0, active.Count);
            (int px,int pz) = objects[active[ri]];
            bool found = false;
            for(int n =0; n<k; n++)
            {
                float angle = UnityEngine.Random.value * Mathf.PI * 2;
                float dist = radius + UnityEngine.Random.value * radius;
                int newx = Mathf.RoundToInt(px + dist * Mathf.Cos(angle));
                int newz = Mathf.RoundToInt(pz + dist * Mathf.Sin(angle));

                //Checks to ensure that the point is valid and no other points are within distance 
                if(newx < 0 || newx >= GlobalParam.max_width || newz <0 || newz >= GlobalParam.max_height 
                    || !accessible[newx, newz] || !IsValid(newx, newz, ref grid, ref objects, ref radius) ||
                    GlobalParam.terrain_map[newx, newz] == Terrain_cus.SAND || GlobalParam.terrain_map[newx, newz] == Terrain_cus.SNOW)
                {
                    continue;
                }
                
                Add(newx, newz);
                found = true;
                break;

            }
            //Remove from active once a point is found and added to objects
            if (!found) active.RemoveAt(ri);

        }
    }

    

    //By using absolute from noise function to amplify the values
    static float Ridgenoise(float nx, float nz)
    {
        return 2.0f * (0.5f - Mathf.Abs(0.5f - Noise(nx, nz)));
    }

    void InstantiateTerrain()
    {
        for (int i = 0; i < max_height; i++)
        {
            for (int j = 0; j < max_width; j++)
            {
                //This will help to determine which terain to generate
                //Terrain_cus a = Biome(data[i, j], moisture[i, j]);
                //No matter if its random or not, have to populate the data if it is the first run
                if (GlobalParam.m_random || GlobalParam.simulatedRuns == 0)
                {
                    GlobalParam.terrain_map[i, j] = Biome(data[i, j], moisture[i, j]);
                    //Terrain_obj b = Objects(data[i, j], moisture[i, j]);
                    //GlobalParam.terrain_objs[i, j] = Objects(data[i, j], moisture[i, j]);
                    GlobalParam.terrain_objs[i, j] = Terrain_obj.NONE;

                }


                //Get the position te prepare for instantiating
                Vector3 pos = new Vector3(i, (int)(data[i, j] * 10.0f), j);
                switch (GlobalParam.terrain_map[i,j])
                {
                    case Terrain_cus.WATER:
                        //Only water would make the path inaccessible
                        accessible[i, j] = false;
                        Instantiate(water_prefab, pos, Quaternion.identity);
                        break;
                    case Terrain_cus.SAND:
                        Instantiate(sand_prefab, pos, Quaternion.identity);
                        break;
                    case Terrain_cus.GRASS:
                        Instantiate(grass_prefab, pos, Quaternion.identity);

                        break;
                    case Terrain_cus.SNOW:
                        Instantiate(snow_prefab, pos, Quaternion.identity);
                        break;


                }


            }
        }
        //Spawn forced shelter here as we need the acessibility 
        //The whole object instantiating here as accessibility is required

        if (GlobalParam.m_random || GlobalParam.simulatedRuns == 0)
        {
           //Loop through all grids to initiate it to -1
            for (int i = 0; i < GlobalParam.max_width; i++)
            {
                for (int j = 0; j < GlobalParam.max_height; j++)
                {
                    grid[i, j] = -1;
                }
            }
            //Loop through all tiles and calling PoissonDisk to ensure that no spots are missed
            for (int i = 0; i < GlobalParam.max_width; i++)
            {
                for (int j = 0; j < GlobalParam.max_height; j++)
                {
                    PoissonDisk(i, j, 5);
                }
            }


            for (int i=0; i< objects.Count; i++)
            {
                (int x, int z) = objects[i];
                
                Vector3 pos = new Vector3(x, (int)(data[x, z] * 10.0f), z);
                int rand = UnityEngine.Random.Range(0, 1000);
                if(rand == 5)
                {
                    if(shelter_count < max_shelter)
                    {
                        safe[x, z] = true;
                        GlobalParam.terrain_objs[x, z] = Terrain_obj.SHELTER;
                        shelter_count++;
                        Instantiate(shelter_prefab, new Vector3(pos.x, pos.y + 1, pos.z), Quaternion.identity);
                        continue;
                    }
                    
                }
                accessible[x, z] = false;
                GlobalParam.terrain_objs[x, z] = Terrain_obj.TREE;
                Instantiate(tree_prefab, new Vector3(pos.x, pos.y + 1, pos.z), Quaternion.identity);

            }

            //if shelter count is zero, force spawn in a shelter at a random location
            if (shelter_count == 0)
            {
                int x = UnityEngine.Random.Range(0, GlobalParam.max_height - 1);
                int z = UnityEngine.Random.Range(0, GlobalParam.max_height - 1);
                //Loop to check if the tile is accessible or not
                while (!accessible[x, z])
                {
                    x = UnityEngine.Random.Range(0, GlobalParam.max_height - 1);
                    z = UnityEngine.Random.Range(0, GlobalParam.max_height - 1);
                }

                GlobalParam.terrain_objs[x, z] = Terrain_obj.SHELTER;
                safe[x, z] = true;
                Vector3 pos = new Vector3(x, (int)(data[x, z] * 10.0f), z);
                Instantiate(shelter_prefab, new Vector3(pos.x, pos.y + 1, pos.z), Quaternion.identity);
                shelter_count++;
            }
        }
    }

    

    void Awake(){ // awake
        
        //Check if rnadom toggle is on
        if (m_random)
        {
            //Shuffle the permuatation array if it is on
            Randomize_permutation();
            
        }
        GenerateRandomMap();

        InstantiateTerrain();
       

    }


    void Start()
    {
        //Empty for now

    }

    void Update(){
        //Empty for now
    }

}
