using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

// https://discussions.unity.com/t/enum-as-a-function-param-in-a-button-onclick/214010
// https://dev.to/matthewodle/simple-3d-camera-movement-1lcj
// https://gist.github.com/david-hodgetts/aee82a4e9f22b14b1b63

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
    private float camSpeed = 5.0f;
    private float scrollSpeed = 20.0f;
    private float runMultiplier = 3.0f;

    private float moveSpeed = 10.0f;
    private float lookSpeed = 2.0f;

    public void ResetWhenTopDown() {
        // reset camera's params when top down pressed
        TerrainGen tg = terrainGen.GetComponent<TerrainGen>();
        Vector3 position = transform.position;

        // calculate y here based on how large the map is
        position.y = 120.0f; // temporary for now

        // x and z are straightforward
        position.x = tg.GetWidth() / 2.0f;
        position.z = tg.GetHeight() / 2.0f;

        transform.position = position;

        // reset rotation of camera as well
        transform.eulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
    }

    public void ResetWhenFree() {
        // reset camera's params when free pressed
        transform.position = new Vector3(-10.0f, 50.0f, -10.0f);
        transform.eulerAngles = new Vector3(46.0f, 46.0f, 0.0f);
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
            transform.position += scrollSpeed * new Vector3(0, -Input.GetAxis("Mouse ScrollWheel"), 0) * Time.deltaTime;
        }

        Vector3 position = transform.position;
        position.x = position.x + (totalSpd * horizontal) * Time.deltaTime;
        position.z = position.z + (totalSpd * vertical) * Time.deltaTime;
        transform.position = position;
    }

    // mimics the unity editor cam sans any obj picking
    private void FreeCameraMode()
    {
        if (Input.GetMouseButton(2))
        {
            float translateX = 0;
            float translateY = 0;

            translateY = Input.GetAxis("Mouse Y") * moveSpeed;
            translateX = Input.GetAxis("Mouse X") * moveSpeed;

            float zoom = Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
            transform.Translate(-translateX, -translateY, zoom);
        }
        else {
            // WASD check here for freecam
            // get direction vectors for camera (forward, right)

            // girl help why am i getting further
            Vector3 camForward = transform.forward.normalized;
            Vector3 camRight = transform.right.normalized;

            if (Input.GetKey(KeyCode.W))
            {
                transform.Translate(-camForward);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                transform.Translate(camForward);
            }

            // change x
            if (Input.GetKey(KeyCode.A))
            {
                transform.Translate(-camRight);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                transform.Translate(camRight);
            }
        }


        // rotation

        float rotationX = 0;
        float rotationY = 0;

        if (Input.GetMouseButton(1))
        {
            rotationX = Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY = Input.GetAxis("Mouse X") * lookSpeed;
        }

        transform.Rotate(0, rotationY, 0, Space.World);
        transform.Rotate(-rotationX, 0, 0);
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
            // free and default use the same
            case CameraMode.FREE:
            default:
                FreeCameraMode();
                break;

        }
    }
}
