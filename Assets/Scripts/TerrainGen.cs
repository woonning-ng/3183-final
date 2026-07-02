using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static PerlinNoise;

//References:
//https://www.redblobgames.com/maps/terrain-from-noise/

enum Terrain_cus
{
    WATER,
    SAND,
    GRASS,
    SNOW
}

//64 x 64
public class TerrainGen : MonoBehaviour
{
    static int max_width = 128;
    static int max_height = 128;
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
    //2d array to store the perlin noise values
    //The 2d array is [x, z] and the float within is the y value
    //Y value have to be converted to int as we are going for minecraft style

    float[,] data = new float[max_width, max_height];
    float[,] moisture = new float[max_width, max_height];

    public float GetWidth() { return max_width; }
    public float GetHeight() { return max_height; }
    public float GetY(float x, float z) {
        return (int)(data[(int)x, (int)z] * 10.0f);
    }

    void GenerateRandomMap() {
        for (int i = 0; i < max_height; i++) {
            for (int j = 0; j < max_width; j++) {
                //Some other customization options:
                //1. Ridge noise (abs the values to make the elevation go higher?)

                //There are other stuff that can customise the terrain
                //Octave and frequency
                //For now, a simple implementation of Terrain gen with perlin noise
                //float nx = (float)j / max_width - 0.5f, ny = (float)i / max_height - 0.5f;
                float nx = (float)j / max_width * frequency, ny = (float)i/ max_height *frequency;
                data[i, j] = 1.0f * Ridgenoise(nx * 1.0f, ny * 1.0f);
                data[i,j] += 0.5f * Ridgenoise(nx * 2.0f, ny * 2.0f);
                data[i, j] += 0.25f * Ridgenoise(nx * 4.0f, ny * 4.0f);
                data[i, j] /= (1.0f + 0.5f + 0.25f);
                //data[i, j] = Mathf.Pow(data[i, j], exp);
                moisture[i, j] = Noise(nx, ny);
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
            if(moist <= -0.2f )
            {
                return Terrain_cus.SAND;
            }
            return Terrain_cus.GRASS;
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

    static float Ridgenoise(float nx, float nz)
    {
        return 2.0f * (0.5f - Mathf.Abs(0.5f - Noise(nx, nz)));
    }
    void Start(){
        
        GenerateRandomMap();
        for(int i=0; i< max_height; i++)
        {
            for(int j=0; j<max_width; j++)
            {
                //This will help to determine which terain to generate
                Terrain_cus a = Biome(data[i, j], moisture[i, j]);
                Vector3 pos = new Vector3(i, (int)(data[i, j]*10.0f), j);
                switch (a)
                {
                    case Terrain_cus.WATER:
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
    }

    void Update(){
        //Empty for now
    }
}
