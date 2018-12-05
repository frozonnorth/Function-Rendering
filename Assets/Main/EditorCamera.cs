using UnityEngine;

public class EditorCamera : MonoBehaviour
{
    public enum Mode {Examine/*,Editor*/}
    public Mode currentmode = Mode.Examine;

    //Mouse speed
    public float mousexSpeed = 200.0f;
    public float mouseySpeed = 200.0f;
    //Examine mode
    public Renderer lookattarget;
    public Vector3 lookatpos;
    public int yMinLimit = -80;
    public int yMaxLimit = 80;
    public float mouseDampening = 5.0f;
    public float distance = 5.0f;
    public int zoomRate = 40;
    public float zoomDampening = 5.0f;

    float xDeg = 0.0f;
    float yDeg = 0.0f;
    float currentDistance;
    float desiredDistance;
    Quaternion desiredRotation;
    Quaternion rotation;
    Vector3 position;



    public void SetTarget(GameObject targetobject)
    {
        lookattarget = targetobject.GetComponent<Renderer>();
        lookatpos = lookattarget.bounds.center;

        float minimum = 3.5f;//predefined minimum distance is 3.5 distance away

        
        // a little trickery here. we get the cloeset point on the bounding box to the camera
        // with the cloest point minus the center, will give the size, where the camera is currently looking at
        float size = Vector3.Distance(lookattarget.bounds.ClosestPoint(transform.position), lookatpos);
        if(size < 1)
        {
            distance = minimum;
        }
        else
        {
            //special addition to see if object is within 5x5x5 bounds to increse distance
            float bounds = 3;
            float extradistancetoincludeorigin = 0;
            if(Vector3.Distance(lookatpos,Vector3.zero)<= bounds)
            {
                extradistancetoincludeorigin = Vector3.Distance(lookatpos, Vector3.zero);
            }
            distance = minimum * size + extradistancetoincludeorigin;
        }

        currentDistance = distance;
        desiredDistance = distance;

        //be sure to grab the current rotations as starting points.
        position = transform.position;
        rotation = transform.rotation;
        desiredRotation = transform.rotation;

        xDeg = Vector3.Angle(Vector3.right, transform.right);
        yDeg = Vector3.Angle(Vector3.up, transform.up);
    }

    void LateUpdate()
    {
        if (currentmode == Mode.Examine)
        {
            if (!lookattarget) return;

            float inputx = 0;
            float inputy = 0;
            //Mouse input only for PC and MC
            #if UNITY_STANDALONE_WIN
            if (Input.GetMouseButton(0))
            {
                inputx = Input.GetAxis("Mouse X");
                inputy = Input.GetAxis("Mouse Y");
            }
            #elif UNITY_ANDROID
            //touch input
            if (Input.touchCount == 1  && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
             //Get movement of the finger since last frame
                Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;

                Vector2 sensitity = new Vector2(0.15f, 0.15f);
                inputx = touchDeltaPosition.x * sensitity.x;
                inputy = touchDeltaPosition.y * sensitity.y;
            }
            #endif

            #region rotation
            //Rotation
            xDeg += inputx * mousexSpeed * 0.02f;
            yDeg -= inputy * mouseySpeed * 0.02f;
            //Clamp the vertical axis (up down)
            // yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);
            // set camera rotation 
            desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
            //Apply dampening
            rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * mouseDampening);
            #endregion


            #region zoom
            float inputzoom = 0;

            //for mouse input
            inputzoom += Input.GetAxis("Mouse ScrollWheel");

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

                inputzoom += deltaMagnitudeDiff;
            }

            // affect the desired Zoom distance if we roll the scrollwheel
            desiredDistance -= inputzoom * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
            // For smoothing of the zoom, lerp distance
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);
            // calculate position based on the new currentDistance 
            position = lookatpos - (rotation * Vector3.forward * currentDistance);
            #endregion

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}