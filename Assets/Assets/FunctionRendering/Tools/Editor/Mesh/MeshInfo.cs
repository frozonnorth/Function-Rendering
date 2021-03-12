using UnityEngine;
using UnityEditor;
using System.Collections;
 
public class MeshInfo : ScriptableObject
{   
    [MenuItem ("Utilities/Mesh/Show Mesh Info")]
    public static void ShowCount()
    {
        int triangles = 0;
        int vertices = 0;
        int meshCount = 0;
 
        foreach (GameObject go in Selection.GetFiltered(typeof(GameObject), SelectionMode.TopLevel))
        {
            Component[] skinnedMeshes = go.GetComponentsInChildren(typeof(SkinnedMeshRenderer)) ;
            Component[] meshFilters = go.GetComponentsInChildren(typeof(MeshFilter));
 
            ArrayList totalMeshes = new ArrayList(meshFilters.Length + skinnedMeshes.Length);
 
            foreach (Component t in meshFilters)
            {
                MeshFilter meshFilter = (MeshFilter)t;
                totalMeshes.Add(meshFilter.sharedMesh);
            }
 
            foreach (Component c in skinnedMeshes)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)c;
                totalMeshes.Add(skinnedMeshRenderer.sharedMesh);
            }
 
            foreach (object o in totalMeshes)
            {
                Mesh mesh = o as Mesh;
                if (mesh == null)
                {
                    Debug.LogWarning("You have a missing mesh in your scene.");
                    continue;
                }
                vertices += mesh.vertexCount;
                triangles += mesh.triangles.Length / 3;
                meshCount++;
            }
        }
 
        EditorUtility.DisplayDialog("Vertex and Triangle Count", vertices
            + " vertices in selection.  " + triangles + " triangles in selection.  "
            + meshCount + " meshes in selection." + (meshCount > 0 ? ("  Average of " + vertices / meshCount
            + " vertices and " + triangles / meshCount + " triangles per mesh.") : ""), "OK", "");
    }
}