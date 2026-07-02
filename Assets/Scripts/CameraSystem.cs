using UnityEngine;
using UnityEngine.InputSystem;

// https://discussions.unity.com/t/enum-as-a-function-param-in-a-button-onclick/214010
// https://dev.to/matthewodle/simple-3d-camera-movement-1lcj

// very difficult to see human at a 128 x 128 scale. add markers on screen?
// is there a way to visualise the pathfinding, the agent's decision, etc?

public class CameraSystem : MonoBehaviour
{

    public enum CameraMode { 
        TOPDOWN = 0,    // top down mode to view the map
        FREE    = 1     // allowing user to move around
    }

    CameraMode mode = CameraMode.TOPDOWN; // default is top down

    public GameObject terrainGen;         // need this for sizing (camera)

    // params like zoom, moving, etc here idk
    private bool shiftPressed = false;
    private float camSpeed = 0.2f;
    private float scrollSpeed = 10.0f;
    private float runMultiplier = 3.0f;

    public void ResetWhenTopDown() {
        // reset camera's params when top down pressed
        TerrainGen tg = terrainGen.GetComponent<TerrainGen>();
        Vector3 position = transform.position;

        // calculate y here based on how large the map is
        position.y = 120.0f;

        // x and z are straightforward
        position.x = tg.GetWidth() / 2.0f;
        position.z = tg.GetHeight() / 2.0f;

        transform.position = position;

        // reset rotation of camera as well
        transform.eulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
    }

    public void ResetWhenFree() { 
        // reset camera's params when free pressed
    }

    public void SetMode(int newMode) {
        UnityEngine.Debug.Log(newMode);
        switch (newMode)
        {
            case 0:
                mode = CameraMode.TOPDOWN;
                ResetWhenTopDown();
                break;
            case 1:
            default:
                mode = CameraMode.FREE;
                ResetWhenFree();
                break;

        }
    }

    public string GetMode() {
        switch (mode)
        {
            case CameraMode.TOPDOWN:
                return "Top Down";
            default:               // free and default use the same
            case CameraMode.FREE:
                return "Free";

        }
    }

    private void TopDownCameraMode() {
        UnityEngine.Debug.Log("in top down camera mode");
        // at least allow for zooming here and moving UDLR if needed
        if (Input.GetKey(KeyCode.LeftShift)) {
            shiftPressed = true;
        }

        float totalSpd = camSpeed * (shiftPressed ? runMultiplier : 1.0f);
        float horizontal = 0.0f; float vertical = 0.0f;

        // change z
        if (Input.GetKey(KeyCode.W)) {
            vertical = 1.0f;
        } 
        else if (Input.GetKey(KeyCode.S))
        {
            vertical = -1.0f;
        }

        // change x
        if (Input.GetKey(KeyCode.A))
        {
            horizontal = -1.0f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            horizontal = 1.0f;
        }

        // change y
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            transform.position += scrollSpeed * new Vector3(0, -Input.GetAxis("Mouse ScrollWheel"), 0);
        }

        Vector3 position = transform.position;
        position.x = position.x + totalSpd * horizontal;
        position.z = position.z + totalSpd * vertical;
        transform.position = position;
    }

    // to put the camera back where it should be by default
    public void ResetCamera() {
        switch (mode)
        {
            case CameraMode.TOPDOWN:
                ResetWhenTopDown();
                break;
            default:               // free and default use the same
            case CameraMode.FREE:
                ResetWhenFree();
                break;

        }
    }

    private void FreeCameraMode() { 
        // usual unity camera shenanigans
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResetCamera();
    }

    // Update is called once per frame
    void Update()
    {
        switch (mode) {
            case CameraMode.TOPDOWN:
                TopDownCameraMode();
                break;
            default:               // free and default use the same
            case CameraMode.FREE:
                FreeCameraMode();
                break;

        }
    }
}
