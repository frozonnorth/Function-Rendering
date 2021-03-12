using UnityEngine;

//This is for the coordinate axes to scale bigger/smaller
//and also to appear or dissapear when user toggles from UI
public class CoordinateAxes : MonoBehaviour
{
    //similar function to camera
    //try to scale to target object bounds
    public void ScaleToObject(GameObject targetobject)
    {
        Bounds objectbounds = targetobject.getObjectBounds();

        //just take average
        float avgsize = (objectbounds.size.x + objectbounds.size.y + objectbounds.size.z)/3;
        float size = avgsize * 1.1f;
        transform.localScale = new Vector3(size, size, size);
    }
    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }
}
