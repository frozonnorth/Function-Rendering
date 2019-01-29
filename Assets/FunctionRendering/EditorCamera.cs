/* TODO:
 * Add more modes of camera
 * Examine: Closely view the details of an object, zoom in/out, rotate around object
 * Editor: Mimic Unity Editor Camera,
 * Player: Camera of a virtual charater moving around the scene
 */
using UnityEngine;

public class EditorCamera : MonoBehaviour
{
    public enum Mode {Examine,Editor,Player}
    public Mode currentmode = Mode.Examine;

    //Examine mode variables
    GameObject ExamineTarget;
    Vector3 ExamineLookAtPosition;
    float minimumcamdistance = 3.5f; //predefined minimum camera distance of 3.5f
    float currentDistance;
    float desiredDistance;
    //Examine mode Rotation variables 
    float xDeg;
    float yDeg;
    //Examine mode settings
    public Vector2 MouseSensitivity = new Vector2(200.0f, 200.0f);
    public Vector2 TouchSensitivity = new Vector2(0.15f, 0.15f);
    public float rotationDampening = 5.0f;
    public int zoomSensitivity = 40;
    public float zoomDampening = 5.0f;

    //Camera Object variables
    Quaternion rotation;
    Vector3 position;

    public void SetExamineTarget(GameObject targetobject)
    {
        // A little trick here to calculate size of mesh object
        // First we get the bounds of the object, next obtain the lookat position 3d coordinates.
        // We calculate the distance of the closest point on the bounds to the camera
        // With the closest point minus the center, will give us the size/distance of the object to the camera
        ExamineTarget = targetobject;
        Bounds objectbounds = getBounds(targetobject);
        ExamineLookAtPosition = objectbounds.center;
        float size = Vector3.Distance(objectbounds.ClosestPoint(transform.position), ExamineLookAtPosition);

        //Because we are using a 1x1x1 unit axis , if the object size is small we just use the minimum distance
        float distance;
        if (size <= 1)
        {
            distance = minimumcamdistance;
        }
        else
        {
            //We want to keep the camera in view of the origin(Vector3.zero) axis if the object is within 3x3x3 bounds.
            float bounds = 3;
            float extradistancetoincludeorigin = 0;
            if(Vector3.Distance(ExamineLookAtPosition, Vector3.zero) <= bounds)
            {
                extradistancetoincludeorigin = Vector3.Distance(ExamineLookAtPosition, Vector3.zero);
            }
            distance = minimumcamdistance * size + extradistancetoincludeorigin;
        }

        currentDistance = distance;
        desiredDistance = distance;

        xDeg = Vector3.Angle(Vector3.right, transform.right);
        yDeg = Vector3.Angle(Vector3.up, transform.up);
    }

    void LateUpdate()
    {
        //Examine Logic
        if (currentmode == Mode.Examine)
        {
            //Skip If no target have been set to examine
            if (ExamineTarget == null) return;

            float inputX = 0;
            float inputY = 0;

            #region Obtain Inputs for Rotation
            //Obtain Mouse Input for PC and MaC
            #if UNITY_STANDALONE_WIN
            if (Input.GetMouseButton(0))
            {
                inputX = Input.GetAxis("Mouse X") * MouseSensitivity.x;
                inputY = Input.GetAxis("Mouse Y") * MouseSensitivity.y;
            }
            #elif UNITY_ANDROID
            //Obtain Touch Input for Android Devices
            if (Input.touchCount == 1  && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                //Get movement of the finger (delta) since last frame
                Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
                inputx = touchDeltaPosition.x * TouchSensitity.x;
                inputy = touchDeltaPosition.y * TouchSensitity.y;
            }
            #endif
            #endregion

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

            #region Apply Zoom
            // Change the desired distance based on zoom
            desiredDistance -= inputzoom * Time.deltaTime * zoomSensitivity * Mathf.Abs(desiredDistance);

            // Lerp the current distance with desiredfFor smoothing of the zoom
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);

            // calculate position based on the currentDistance away from the lookattarget position
            position = ExamineLookAtPosition - (rotation * Vector3.forward * currentDistance);
            #endregion

            //Apply it to the gameobject
            transform.rotation = rotation;
            transform.position = position;
        }
        else if (currentmode == Mode.Editor)
        {
            //TODO
        }
    
        {
            //TODO
        }
    }

    //function to obtain the bounds of the current object, or the bounds of its children gameobjects
    //https://forum.unity.com/threads/getting-the-bounds-of-the-group-of-objects.70979/
    Bounds getBounds(GameObject objeto)
    {
        Bounds bounds;
        Renderer childRender;
        bounds = getRenderBounds(objeto);
        if (bounds.extents.x == 0)
        {
            bounds = new Bounds(objeto.transform.position, Vector3.zero);
            foreach (Transform child in objeto.transform)
            {
                childRender = child.GetComponent<Renderer>();
                if (childRender)
                {
                    bounds.Encapsulate(childRender.bounds);
                }
                else
                {
                    bounds.Encapsulate(getBounds(child.gameObject));
                }
            }
        }
        return bounds;
    }
    Bounds getRenderBounds(GameObject objeto)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        Renderer render = objeto.GetComponent<Renderer>();
        if (render != null)
        {
            return render.bounds;
        }
        return bounds;
    }
}