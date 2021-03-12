/* TODO:
 * Add more modes of camera
 * Examine: Closely view the details of an object, zoom in/out, rotate around object
 * Editor: Mimic Unity Editor Camera,
 * Player: Camera of a virtual charater moving around the scene
 */
using UnityEngine;

public class EditorCamera : MonoBehaviour
{
    public enum Mode { Examine, Editor, Player }
    public Mode currentmode = Mode.Examine;

    //Camera Object variables
    Quaternion rotation;
    Vector3 position;

    //Refrenced Variables to ui Canvas to find mouseoverUI component
    public Canvas uicanvas;
    MouseOverUI[] mouseoveruis;
    

    //Examine mode Private Variables 
    GameObject ExamineTarget;
    Vector3 ExamineLookAtPosition;
    float currentDistance;
    float desiredDistance;
    float xDeg = 0;
    float yDeg = 0;

    //Examine Mode settings
    readonly float minimumcamdistance = 3.5f; //predefined minimum camera distance of 3.5f
    public Vector2 MouseSensitivity = new Vector2(200.0f, 200.0f);
    public Vector2 TouchSensitivity = new Vector2(5f, 5f);
    public float rotationDampening = 5.0f;
    public int zoomSensitivity = 40;
    public float zoomDampening = 5.0f;

    #region To Allow Wireframe Mode or not
    bool isWireframe = false;
    void OnPreRender()
    {
        if (isWireframe) GL.wireframe = true;
    }
    void OnPostRender()
    {
        if (isWireframe) GL.wireframe = false;
    }
    public void Setwireframe(bool value)
    {
        isWireframe = value;
    }
    #endregion

    void Awake()
    {
        //find all uis that has the mouseoverui component within the UI canvas
        mouseoveruis = uicanvas.GetComponentsInChildren<MouseOverUI>();
    }

    /// <summary>
    /// Set the target for the camera to "examine"
    /// This function zoom out and auto re-center on the targetobject
    /// This works by obtaining the object's size and calculating distance
    /// based off a predefined size to distance
    /// </summary>
    public void SetExamineTarget(GameObject targetobject)
    {
        // Because the object might have uneven shape, we want to get the current
        // Length/size of the mesh object based on the direction of camera
        // First we get the bounds of the object,
        // We obtain the cloest point on the bounds to the camera
        // We calculate the distance of the closest point on the bounds to the center
        // This gives us the size based on camera direction
        ExamineTarget = targetobject;
        Bounds objectbounds = targetobject.getObjectBounds();
        ExamineLookAtPosition = objectbounds.center;

        Vector3 currentposition = transform.position;

        Vector3 closestpoint = new Vector3(999, 999, 999);
        float size = 0;
        float distance = 0;
        do
        {
            if (closestpoint.Equals(currentposition))
            {
                currentposition = ExamineLookAtPosition - (rotation * Vector3.forward * distance);
            }
            closestpoint = objectbounds.ClosestPoint(currentposition);
            size = Vector3.Distance(closestpoint, ExamineLookAtPosition);
            if (size <= 1)
            {
                // Because we are using a 1x1x1 unit axis , 
                // We say that if the object size is smaller than size 1 
                // We just use the minimum distance
                distance = minimumcamdistance;
            }
            else
            {
                //We want to keep the camera in view with the coordinate axis model 
                //if the object is within 3x3x3 bounds from origin.
                float bounds = 3;
                float extradistancetoincludeorigin = 0;
                if (Vector3.Distance(ExamineLookAtPosition, Vector3.zero) <= bounds)
                {
                    extradistancetoincludeorigin = Vector3.Distance(ExamineLookAtPosition, Vector3.zero);
                }
                distance = minimumcamdistance * size + extradistancetoincludeorigin;
            }
        }
        while (closestpoint.Equals(currentposition));

        currentDistance = distance;
        desiredDistance = distance;
    }

    void LateUpdate()
    {
        //Detect if mouse is outside of window, 
        bool MouseOutside = false;
        Vector2 mousepos = Input.mousePosition;
        if (mousepos.x < 0 ||
            mousepos.x > Screen.width ||
            mousepos.y < 0 ||
            mousepos.y > Screen.height)
        {
            MouseOutside = true;
        }
        //Detect if mouse is in UI
        bool MouseInUI = false;
        foreach (MouseOverUI mouseoverui in mouseoveruis)
        {
            if (mouseoverui.isMouseOver)
            {
                MouseInUI = true;
                break;
            }
        }
        
        //Examine Camera Mode Logic
        if (currentmode == Mode.Examine)
        {
            //Do nothing if target have been set to examine
            if (ExamineTarget == null) return;

            float inputX = 0;
            float inputY = 0;

            if ((MouseOutside || MouseInUI) != true)    
            {
                #region Obtain Inputs for Rotation
                //Obtain Mouse Input for PC and MaC
                #if UNITY_STANDALONE || UNITY_WEBGL
                if (Input.GetMouseButton(0))
                {
                    inputX = Input.GetAxis("Mouse X") * MouseSensitivity.x;
                    inputY = Input.GetAxis("Mouse Y") * MouseSensitivity.y;
                }

                
                #endif
                #endregion
            }

            #if UNITY_ANDROID
            //Obtain Touch Input for Android Devices
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                //Get movement of the finger (delta) since last frame
                Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
                inputX = touchDeltaPosition.x * TouchSensitivity.x;
                inputY = touchDeltaPosition.y * TouchSensitivity.y;
            }
            #endif

            #region Apply Rotation
            //Rotation based on inputX and inputY
            xDeg += inputX * 0.02f;
            yDeg -= inputY * 0.02f;

            //Set the desiredRotatation 
            Quaternion desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);

            //Apply Rotation by slowling lerping towards the desired rotation using a rotation dampening value
            rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * rotationDampening);
            #endregion

            float inputzoom = 0;

            if ((MouseOutside || MouseInUI) != true)
            {
                #region Obtain Inputs for Zoom
                //Obtain Mouse Input for PC and MaC
                inputzoom = Input.GetAxis("Mouse ScrollWheel");

                //touch input for zoom
                if (Input.touchCount == 2)
                {
                    // Store both touches.
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);

                    // Find the position in the previous frame of each touch.
                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                    // Find the magnitude of the vector (the distance) between the touches in each frame.
                    float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                    // Find the difference in the distances between each frame.
                    float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                    inputzoom = Mathf.Sign(deltaMagnitudeDiff) * 0.03f;
                }
                #endregion
            }

            #region Apply Zoom
            // Change the desired distance based on zoom
            desiredDistance -= inputzoom * zoomSensitivity * Mathf.Abs(desiredDistance);

            // Lerp the current distance with desiredfFor smoothing of the zoom
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);

            // calculate position based on the currentDistance away from the lookattarget position
            position = ExamineLookAtPosition - (rotation * Vector3.forward * currentDistance);
            #endregion

            //Apply it to the camera object
            transform.rotation = rotation;
            transform.position = position;
        }
        //Editor Camera Mode Logic
        else if (currentmode == Mode.Editor)
        {
            //TODO
        }
    }
}