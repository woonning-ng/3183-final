/*
* @file      HumanVisualiser.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file handles the transformation of the visual associated with the human.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
 */

using UnityEngine;

public class HumanVisualiser : MonoBehaviour
{
    // Get the tile to translate, and the terrainGen obj to reference
    // for obtaining Y
    // Thsi script is attached to the human itself, so there is no need to
    // query for the human
    public GameObject tileVisualiser;
    public GameObject terrainGen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Obtain Y value from TerrainGen
        float yVal = terrainGen.GetComponent<TerrainGen>().GetY(transform.position.x, transform.position.z);

        // Get the current position of human, set the Y pos to what was obtained
        // above with a displacement of 0.5f (half block) + 0.1f (so that it
        // overlays on top without cutting in too much). Then set the tile
        // visualiser's transform to this
        Vector3 tilePos = tileVisualiser.GetComponent<Transform>().position;
        tilePos.x = (int)transform.position.x;
        tilePos.y = yVal + 0.5f + 0.01f;
        tilePos.z = (int)transform.position.z;
        tileVisualiser.GetComponent<Transform>().position = tilePos;
    }
}
