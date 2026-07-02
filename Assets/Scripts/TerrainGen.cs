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
                //There are other stuff that can customise the terrain
                //Octave and frequency
                //For now, a simple implementation of Terrain gen with perlin noise
                float nx = (float)j / max_width - 0.5f, ny = (float)i / max_height - 0.5f;
                data[i, j] = Noise(nx, ny);
                moisture[i, j] = Noise(nx, ny);
            }
        }

    }
    //Biome function will return the enum? of the gameobject to spawn?
    Terrain_cus Biome(float height, float moist){
        //Values can be customizable
        //-1 to 1 range
        if (height < 0.6f && moist > 0.6f) return Terrain_cus.WATER;
        else if (height < 0.6f && moist < -0.2f) return Terrain_cus.SAND;
        else if (height > 0.6f && moist > 0.6f) return Terrain_cus.SNOW;
        else return Terrain_cus.GRASS;
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
