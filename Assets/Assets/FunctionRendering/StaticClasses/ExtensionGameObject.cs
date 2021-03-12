using UnityEngine;

public static class ExtensionGameObject
{
    //function to obtain the bounds of the current object, or the bounds of its children gameobjects
    //https://forum.unity.com/threads/getting-the-bounds-of-the-group-of-objects.70979/
    public static Bounds getObjectBounds(this GameObject go)
    {
        Bounds bounds;
        Renderer childRender;
        bounds = getMeshBounds(go);
        if (bounds.extents.x == 0 && bounds.extents.y == 0 && bounds.extents.z == 0)
        {
            bounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (Transform child in go.transform)
            {
                childRender = child.GetComponent<Renderer>();
                if (childRender)
                {
                    bounds.Encapsulate(childRender.bounds);
                }
                else
                {
                    bounds.Encapsulate(getObjectBounds(child.gameObject));
                }
            }   
        }
        return bounds;
    }
    public static Bounds getMeshBounds(this GameObject go)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        MeshRenderer meshrender = go.GetComponent<MeshRenderer>();
        if (meshrender != null)
        {
            return meshrender.bounds;
        }
        return bounds;
    }
}
