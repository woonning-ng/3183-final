using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
//using UnityEngine.UIElements;
using static PerlinNoise;
using static GlobalParam;
using System.IO;


//References:
//https://www.redblobgames.com/maps/terrain-from-noise/

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

//128 x 128

public class TerrainGen : MonoBehaviour
{
    //static int max_width = 128;
    //static int max_height = 128;
    //this frequency is to make it more rough or smoother
    //The higher the frequency value is, the more rough the terrain would be
    //The lower frequency value would make the terrain more smooth
    //By that I meant the variety would be smaller if the frequency value is lower
    static float frequency = 3.0f;
    //static float exp = 0.25f;
    //static float octave = 
    public GameObject water_prefab;
    public GameObject sand_prefab;
    public GameObject grass_prefab;
    public GameObject snow_prefab;
    public GameObject tree_prefab;
    public GameObject shelter_prefab;

    int shelter_count = 0;
    static int max_shelter = 3;

    public Pathfinding pathfinding;

    //Add some boolean to check if it is toggled?
    //public GameObject m_toggle;

    //public Toggle m_toggle;
    //2d array to store the perlin noise values
    //The 2d array is [x, z] and the float within is the y value
    //Y value have to be converted to int as we are going for minecraft style

    float[,] data = new float[GlobalParam.max_width, GlobalParam.max_height];
    float[,] moisture = new float[GlobalParam.max_width, GlobalParam.max_height];
    bool[,] accessible = new bool[GlobalParam.max_width, GlobalParam.max_height];
    bool[,] safe = new bool[GlobalParam.max_width, GlobalParam.max_height];

    //for map traversal checks
    int[,] traversal = new int[GlobalParam.max_width, GlobalParam.max_height];



    //public float GetWidth() { return max_width; }
    //public float GetHeight() { return max_height; }
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

        float[] octaveOffset = new float[3];
        octaveOffset[0] = 1.0f;
        octaveOffset[1] = 0.5f;
        octaveOffset[2] = 0.25f;
        //Randomize_permutation();
        //Randomize_permutation();
        //Randomize_permutation();
        for (int i = 0; i < max_height; i++)
        {
            for (int j = 0; j < max_width; j++)
            {
                //Some other customization options:
                //1. Ridge noise (abs the values to make the elevation go higher?)

                //There are other stuff that can customise the terrain
                //Octave and frequency
                //For now, a simple implementation of Terrain gen with perlin noise
                //float nx = (float)j / max_width - 0.5f, ny = (float)i / max_height - 0.5f;
                float nx = (float)j / max_width * frequency, ny = (float)i / max_height * frequency;
                //data[i, j] = 1.0f * Ridgenoise(nx * 1.0f, ny * 1.0f);
                //data[i, j] += 0.5f * Ridgenoise(nx * 2.0f, ny * 2.0f);
                //data[i, j] += 0.25f * Ridgenoise(nx * 4.0f, ny * 4.0f);
                //data[i, j] /= (1.0f + 0.5f + 0.25f);

                float octaveTotal = 0.0f;

                for(int k = 0; k < octaveOffset.Length; k++)
                {
                    float octaveInv = 1.0f/ octaveOffset[k];
                    data[i, j] += octaveOffset[k] * Ridgenoise(nx * octaveInv, ny * octaveInv);
                    octaveTotal += octaveOffset[k];
                }

                data[i, j] /= octaveTotal;

                //data[i, j] = Mathf.Pow(data[i, j], exp);
                moisture[i, j] = Noise(nx, ny);
                //set all to true first
                accessible[i, j] = true;
                //set all to false first
                safe[i, j] = false;
                //set all to 1 first
                traversal[i, j] = 1;
            }
        }

    }
    //Biome function will return the enum? of the gameobject to spawn?
    Terrain_cus Biome(float height, float moist){
        //Values can be customizable
        //-1 to 1 range

        //Water moisture to be at least 0.4f? 

        //Customize it to make it so that water does not only exist in the lower height?
        //Might be hard as water going to lower level make sense, they congest at the bottom
        //

        //These values are pretty good for now


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
            //return Terrain_cus.GRASS;
        }
        else if(height >= 0.5f)
        {
            //if(moist >= 0.4f)
            //{
                return Terrain_cus.SNOW;
            //}
            //return Terrain_cus.GRASS;
        }

        
        
        return Terrain_cus.GRASS;
        //if (height < 0.5f && moist > 0.4f) return Terrain_cus.WATER;
        //else if (height < 0.5f && moist < -0.2f) return Terrain_cus.SAND;
        //else if (height >= 0.5f && moist > 0.4f) return Terrain_cus.SNOW;
        //else return Terrain_cus.GRASS;
    }

    Terrain_obj Objects(float height, float moist)
    {
        if(height >= -0.3f && height < 0.5f)
        {
            if (moist>-0.1f)
            {
                //okay time to think of how to spawn tree and shelters
                //maybe for now a randomizer?
                //probability have to be low tho
                //0.1% prob for now?
                int tree = UnityEngine.Random.Range(1, 10);
                if (tree == 2)
                {
                    
                    return Terrain_obj.TREE;
                }
                
            }
        }

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
                //No matter if its random or not, have to populate the data if simulatedruns is 0
                if (GlobalParam.m_random || GlobalParam.simulatedRuns == 0)
                {
                    GlobalParam.terrain_map[i, j] = Biome(data[i, j], moisture[i, j]);
                    //Terrain_obj b = Objects(data[i, j], moisture[i, j]);
                    GlobalParam.terrain_objs[i, j] = Objects(data[i, j], moisture[i, j]);

                    //if shelter count is zero, spawn in a shelter at a random location
                    if(shelter_count == 0)
                    {
                        int x = UnityEngine.Random.Range(0, GlobalParam.max_height-1);
                        int z = UnityEngine.Random.Range(0, GlobalParam.max_height - 1);

                        while (!accessible[x, z])
                        {
                            x = UnityEngine.Random.Range(0, GlobalParam.max_height - 1);
                            z = UnityEngine.Random.Range(0, GlobalParam.max_height - 1);
                        }

                        GlobalParam.terrain_objs[x, z] = Terrain_obj.SHELTER;
                        shelter_count++;
                    }
                }
                //No matter if its random or not, have to populate the data
                //if(GlobalParam.simulatedRuns == 0)
                //{
                //    GlobalParam.terrain_map[i, j] = Biome(data[i, j], moisture[i, j]);
                //    //Terrain_obj b = Objects(data[i, j], moisture[i, j]);
                //    GlobalParam.terrain_objs[i, j] = Objects(data[i, j], moisture[i, j]);
                //}



                Vector3 pos = new Vector3(i, (int)(data[i, j] * 10.0f), j);
                switch (GlobalParam.terrain_map[i,j])
                {
                    case Terrain_cus.WATER:
                        //Only water would make the path inaccessible
                        accessible[i, j] = false;
                        traversal[i, j] = 0;
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

                if (GlobalParam.m_random || simulatedRuns==0)
                {
                    //if shelter count is zero, spawn in a shelter at a random location
                    if (shelter_count == 0)
                    {
                        int x = UnityEngine.Random.Range(0, 127);
                        int z = UnityEngine.Random.Range(0, 127);

                        while (!accessible[x, z])
                        {
                            x = UnityEngine.Random.Range(0, 127);
                            z = UnityEngine.Random.Range(0, 127);
                        }

                        GlobalParam.terrain_objs[x, z] = Terrain_obj.SHELTER;
                        shelter_count++;
                    }
                }

                

                switch (GlobalParam.terrain_objs[i, j])
                {
                    case Terrain_obj.TREE:
                        accessible[i, j] = false;
                        traversal[i, j] = 0;
                        Instantiate(tree_prefab, new Vector3(pos.x, pos.y + 1, pos.z), Quaternion.identity);
                        break;
                    case Terrain_obj.SHELTER:
                        
                        safe[i, j] = true;
                        Instantiate(shelter_prefab, new Vector3(pos.x, pos.y + 1, pos.z), Quaternion.identity);
                        break;
                }
            }
        }
    }
    

    //reference
    //https://www.enjoyalgorithms.com/blog/flood-fill-problem
    void floodfill()
    {

        
       

        int[] offsets = new int[] { -1, 0, 1, 0, -1 };

        Queue<(int x, int z)> queue = new Queue<(int x, int z)>();

        //ok something is wrong with the queue
        queue.Enqueue(new (0,0));
        //UnityEngine.Debug.Log("YESSSSSS\n");
        while (queue.Count > 0)
        {
            
            (int x, int z) = queue.Dequeue();
            //int curr_x = x + neighbouroffsets[0].x;
            if (traversal[x,z] == 2)
            {
                continue;
            }

            if (traversal[x,z] != 0)
            {
                traversal[x, z] = 2;
            }
             

            for(int i =0; i<offsets.Length-1; i++)
            {
                if (x + offsets[i] >=0 && x + offsets[i] < GlobalParam.max_width
                    && z + offsets[i+1] >=0 && z + offsets[i+1] < GlobalParam.max_height&&
                    traversal[x + offsets[i],z + offsets[i+1]] == 1)
                {
                    queue.Enqueue(new(x + offsets[i], z + offsets[i + 1]));
                }
            }
            
        }

        //UnityEngine.Debug.Log("ENDDDDDDD\n");



    }

    void Awake(){ // awake
        

        if (m_random)
        {
            Randomize_permutation();
            
        }
        GenerateRandomMap();

        InstantiateTerrain();
        //UnityEngine.Debug.Log("THIS IS THE OUTPUT!!!!\n");

    }

    private string filepath;


    void Start()
    {
        //UnityEngine.Debug.Log("START OF TERRAINGEN"); // ?????
        // note: can probably invoke pathfinding here to test out terrain validity...?
        //Flood fill works now but now to build the actual bridge

        //Honestly maybe can just merge the start and awake for this as I dont really need pathfinding?
        //ok merge the start and awake to just build a completed map instead.
        floodfill();

        //FOR DEBUG, CAN REMOVE ONCE ITS STABLE
        //______________________________________________________________
        
        //filepath = "C:/Users/Lemin/Documents/TerrainGen.txt";

        //string output = "";
        //for(int i=0; i< GlobalParam.max_width; i++)
        //{

        //    output += "| ";
        //    for(int j=0; j<GlobalParam.max_height; j++)
        //    {

        //        output += traversal[i, j] + " ";
        //    }

        //    output += "|\n";
        //}
        //System.IO.File.AppendAllText(filepath, output);
        //________________________________________________________________

    }

    void Update(){
        //Empty for now
    }

}
