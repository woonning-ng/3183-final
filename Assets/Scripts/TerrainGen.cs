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
                    GlobalParam.terrain_objs[i, j] = Objects(data[i, j], moisture[i, j]);

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


                //Instantiating objects
                switch (GlobalParam.terrain_objs[i, j])
                {
                    case Terrain_obj.TREE:
                        accessible[i, j] = false;
                        //traversal[i, j] = 0;
                        Instantiate(tree_prefab, new Vector3(pos.x, pos.y + 1, pos.z), Quaternion.identity);
                        break;
                    case Terrain_obj.SHELTER:
                        
                        safe[i, j] = true;
                        Instantiate(shelter_prefab, new Vector3(pos.x, pos.y + 1, pos.z), Quaternion.identity);
                        break;
                }
            }
        }
        //Spawn forced shelter here as we need the acessibility 
        if (GlobalParam.m_random || GlobalParam.simulatedRuns == 0)
        {

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
