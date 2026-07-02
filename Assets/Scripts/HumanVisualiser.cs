using UnityEngine;

public class HumanVisualiser : MonoBehaviour
{
    public GameObject tileVisualiser;
    public GameObject terrainGen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //UnityEngine.Debug.Log("current cell pos: "
        //    + transform.position.x + " " + transform.position.z);

        float yVal = terrainGen.GetComponent<TerrainGen>().GetY(transform.position.x, transform.position.z);

        UnityEngine.Debug.Log("current cell y pos: " + yVal);

        Vector3 tilePos = tileVisualiser.GetComponent<Transform>().position;
        tilePos.x = (int)transform.position.x;
        tilePos.y = yVal + 0.5f + 0.01f;
        tilePos.z = (int)transform.position.z;
        tileVisualiser.GetComponent<Transform>().position = tilePos;
    }
}
