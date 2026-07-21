/*
* @file      CameraSystem.cs
* @author    Ng Woon Ning (woonning.ng@digipen.edu)
* @brief
*   This file contains the Behaviour Tree implementation of the
*   Human entity.
*
* @copyright Copyright (C) 2026 DigiPen Institute of Technology
*/

using UnityEngine;

// Resources:
// https://discussions.unity.com/t/enum-as-a-function-param-in-a-button-onclick/214010
// https://dev.to/matthewodle/simple-3d-camera-movement-1lcj
// https://gist.github.com/david-hodgetts/aee82a4e9f22b14b1b63
// https://discussions.unity.com/t/fly-cam-simple-cam-script/428226
// https://stackoverflow.com/questions/71232279/how-to-make-the-camera-follow-the-player-in-unity
// https://discussions.unity.com/t/rotate-camera-around-game-object-and-zoom-camera-into-same-game-object/720654/5
// https://github.com/Aiuces/Camera-Like-Genshin-Impact/blob/main/Assets/Scripts/GenshinCameraController.cs

// very difficult to see human at a 128 x 128 scale. add markers on screen?
// is there a way to visualise the pathfinding, the agent's decision, etc?

public class CameraSystem : MonoBehaviour
{

    // Camera Mode Enums
    public enum CameraMode { 
        TOPDOWN = 0,    // top down mode to view the map
        FREE    = 1,    // allowing user to move around
        FOLLOW  = 2     // follow user 
    }

    CameraMode mode = CameraMode.TOPDOWN; // default is top down

    // Game Object references for Camera settings
    public GameObject terrainGen;         // need this for sizing (camera)
    public GameObject humanTarget;        // follow human here


    // Fields that can be modified in the Editor (per camera mode)
    [Header("------- Top Down Camera Parameters -------")]
    [SerializeField, Header("Speed when Moving")]
    private float camSpeed = 5.0f;

    [SerializeField, Header("Speed Multiplier when Running")]
    private float runMultiplier = 40.0f;

    [SerializeField, Header("Speed when Scrolling")]
    private float scrollSpeed = 80.0f;

    private bool shiftPressed = false;


    [Header(" ------- Free Camera Parameters -------")]
    [SerializeField, Header("Speed when Running")]
    private float moveSpeed = 10.0f;

    [SerializeField, Header("Speed when Looking Around")]
    private float lookSpeed = 2.0f;

    [SerializeField, Header("Maximum Speed when Running")]
    private float maxShift = 100.0f;

    private bool isRotating = false;
    private float rotationY = 0.0f;
    private float speedMultiplier = 0.0f;


    [Header(" ------- Follow Camera Parameters -------")]
    [SerializeField, Header("Speed when Looking Around")]
    private float followLookSpeed = 5.0f;

    [SerializeField, Header("Default Offset")]
    private Vector3 offsetFromTarget = new Vector3(0.0f, 1.0f, 0.0f);

    [SerializeField, Header("Distance between Camera and Target")]
    private float distFromTarget = 10.0f;

    private float lookAngle = 45.0f;
    private float lookHeight = 5.0f;


    // Used to retain the camera's last transform before sim is
    // rerun or a different camera mode is used
    public bool bypassModeDefaults = false;
    private bool previousBypass = false;

    // ------------ CAMERA MODE DEFAULT BYPASS FUNCTIONS ------------
    // Both SetBypass and ReturnPreviousBypass are actually called by the
    // Camera mode toggles (helper functions to retain data)
    public void SetBypass(bool bypass) {
        previousBypass = bypassModeDefaults;
        bypassModeDefaults = bypass;
    }

    public void ReturnPreviousBypass() {
        bypassModeDefaults = previousBypass;
    }

    // ------------ RESET FUNCTIONS ------------
    public void ResetWhenTopDown() {
        if (!bypassModeDefaults)
        {
            // reset camera's params when top down pressed
            TerrainGen tg = terrainGen.GetComponent<TerrainGen>();
            Vector3 position = transform.position;

            // calculate y here based on how large the map is
            position.y = 120.0f; // temporary for now

            // x and z are straightforward
            //position.x = tg.GetWidth() / 2.0f;
            //position.z = tg.GetHeight() / 2.0f;

            // using the globalparam max width and max height
            position.x = GlobalParam.max_width / 2.0f;
            position.z = GlobalParam.max_height / 2.0f;

            transform.position = position;

            // reset rotation of camera as well
            transform.eulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
        }
    }

    public void ResetWhenFree()
    {
        // reset camera's params when free pressed
        if (!bypassModeDefaults) {
            transform.position = new Vector3(-10.0f, 50.0f, -10.0f);
            transform.eulerAngles = new Vector3(46.0f, 46.0f, 0.0f);
        }
    }

    public void ResetWhenFollow() {
        // reset camera's params when follow pressed
        transform.position = humanTarget.transform.position + offsetFromTarget;

        if (!bypassModeDefaults) { 
            
        }
    }

    // ------------ MODE SETTER AND GETTER FUNCTIONS ------------
    public void SetMode(int newMode) {
        //UnityEngine.Debug.Log(newMode);
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
            case 2:
                mode = CameraMode.FOLLOW;
                ResetWhenFollow();
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
            case CameraMode.FOLLOW:
                return "Follow";

        }
    }

    public int GetModeInt() { return (int)mode; }

    // ------------ TOP DOWN CAMERA MODE FUNCTIONS ------------
    private void TopDownCameraMode() {
        // at least allow for zooming here and moving UDLR if needed
        if (Input.GetKey(KeyCode.LeftShift))
        {
            shiftPressed = true;
        }
        else {
            shiftPressed = false;
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

    // ------------ FREE CAMERA MODE FUNCTIONS ------------
    private Vector3 GetBaseInput()
    {
        Vector3 baseVel = new Vector3(0.0f, 0.0f, 0.0f);

        if (Input.GetKey(KeyCode.W))
        {
            baseVel += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            baseVel += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            baseVel += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            baseVel += new Vector3(1, 0, 0);
        }

        return baseVel;
    }

    // mimics the unity editor cam sans any obj picking
    private void FreeCameraMode()
    {
        // Angryboy: Hold right-mouse button to rotate
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }
        if (isRotating)
        {
            // Made by LookForward
            // Angryboy: Replaced min/max Y with numbers, not sure why we had variables in the first place
            float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * lookSpeed;
            rotationY += Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -90, 90);
            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0.0f);
        }

        //Keyboard commands
        float f = 0.0f;
        Vector3 p = GetBaseInput();
        if (Input.GetKey(KeyCode.LeftShift))
        {
            //totalRun += Time.deltaTime;
            p = p * runMultiplier;
            p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
            p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
            p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
            // Angryboy: Use these to ensure that Y-plane is affected by the shift key as well
            speedMultiplier = runMultiplier * Time.deltaTime;
            speedMultiplier = Mathf.Clamp(speedMultiplier, -maxShift, maxShift);
        }
        else
        {
            p = p * moveSpeed;
            speedMultiplier = moveSpeed * Time.deltaTime; // Angryboy: More "correct" speed
        }

        p = p * Time.deltaTime;

        // Angryboy: Removed key-press requirement, now perma-locked to the Y plane
        Vector3 newPosition = transform.position;//If player wants to move on X and Z axis only
        transform.Translate(p);
        newPosition.x = transform.position.x;
        newPosition.z = transform.position.z;

        // Angryboy: Manipulate Y plane by using Q/E keys
        if (Input.GetKey(KeyCode.Q))
        {
            newPosition.y += -speedMultiplier;
        }
        if (Input.GetKey(KeyCode.E))
        {
            newPosition.y += speedMultiplier;
        }

        transform.position = newPosition;
    }

    // ------------ FOLLOW CAMERA MODE FUNCTIONS ------------
    public void FollowCameraMode() {
        // needs to check for zoom AND moving around

        //transform.position = humanTarget.transform.position + offsetFromTarget;

        // rotate around based on mouse pos
        //transform.RotateAround(humanTarget.transform.position, new Vector3(0.0f, -1.0f, 0.0f), 20 * Time.deltaTime * followLookSpeed);

        // zoom in and out based on mouse wheel

        // Get the mouse input for rotation
        float horizontal = Input.GetAxis("Mouse X") * followLookSpeed;
        float vertical = Input.GetAxis("Mouse Y") * followLookSpeed;

        // Rotate the camera around the target
        lookAngle += horizontal;
        lookHeight -= vertical;

        // Clamp the height to the allowable range
        lookHeight = Mathf.Clamp(lookHeight, -20.0f, 80.0f);

        // Calculate the position and rotation of the camera
        Quaternion rotation = Quaternion.Euler(lookHeight, lookAngle, 0f);
        Vector3 position = humanTarget.transform.position - rotation * Vector3.forward * distFromTarget;

        // Move the camera towards the target
        transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * moveSpeed);

        // Look at the target
        transform.LookAt(humanTarget.transform);
    }

    // ------------ RESET CAMERA ------------
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
            case CameraMode.FOLLOW:
                ResetWhenFollow();
                break;

        }
    }

    // ------------ START, UPDATE, FIXEDUPDATE ------------
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // call reset camera once
        ResetCamera();
    }

    // Update is called once per frame
    void Update()
    {
        // call the corresponding mode's update function
        switch (mode) {
            case CameraMode.TOPDOWN:
                TopDownCameraMode();
                break;
            // free and default use the same
            case CameraMode.FREE:
            default:
                FreeCameraMode();
                break;
            case CameraMode.FOLLOW:
                FollowCameraMode();
                break;

        }
    }

    private void FixedUpdate()
    {
        // only follow needs this since it is the only one following a target
        switch (mode) {
            case CameraMode.TOPDOWN:
            case CameraMode.FREE:
            default:
                break;
            case CameraMode.FOLLOW:
                // Get the input for zooming
                float zoom = Input.GetAxis("Mouse ScrollWheel") * followLookSpeed;

                // Zoom the camera in or out
                distFromTarget -= zoom;
                distFromTarget = Mathf.Clamp(distFromTarget, 3.0f, 15.0f); // curr dist, min, max OTL
                break;
        }
    }
}