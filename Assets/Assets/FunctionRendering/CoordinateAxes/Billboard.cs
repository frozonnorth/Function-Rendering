//Different variation found at https://wiki.unity3d.com/index.php/CameraFacingBillboard

using UnityEngine;

public class Billboard : MonoBehaviour
{
    //Auto billboards towards current camera (Camera.main)
    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,Camera.main.transform.rotation * Vector3.up);
    }
}