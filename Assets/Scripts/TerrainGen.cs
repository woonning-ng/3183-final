using Unity.VisualScripting;
using UnityEngine;
using static PerlinNoise;

//References:
//https://www.redblobgames.com/maps/terrain-from-noise/

enum Terrain
{
    WATER,
    SAND,
    GRASS,
    SNOW
}

//64 x 64
public class TerrainGen : MonoBehaviour
{
    static int max_width = 64;
    static int max_height = 64;
    //2d array to store the perlin noise values
    //The 2d array is [x, z] and the float within is the y value
    //Y value have to be converted to int as we are going for minecraft style

    float[,] data = new float[max_width, max_height];
    float[,] moisture = new float[max_width, max_height];


    void GenerateRandomMap() {
        for (int i = 0; i < max_height; i++) {
            for (int j = 0; j < max_width; j++) {
                //There are other stuff that can customise the terrain
                //Octave and frequency
                //For now, a simple implementation of Terrain gen with perlin noise
                float nx = j / max_width - 0.5f, ny = i / max_height - 0.5f;
                data[i, j] = Noise(nx, ny);
                moisture[i, j] = Noise(nx, ny);
            }
        }

    }
    //Biome function will return the enum? of the gameobject to spawn?
    Terrain Biome(float height, float moist){
        //Values can be customizable
        if (height < 0.1f && moist > 0.8f) return Terrain.WATER;
        else if (height < 0.5f && moist < 0.3f) return Terrain.SAND;
        else if (height > 0.8f && moist > 0.8f) return Terrain.SNOW;
        else return Terrain.GRASS;
    }
    void Start(){
        GenerateRandomMap();
        for(int i=0; i< max_height; i++)
        {
            for(int j=0; j<max_width; j++)
            {
                Biome(data[i, j], moisture[i, j]);
            }
        }
    }

    void Update(){

    }
}
