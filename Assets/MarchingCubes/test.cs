using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public Material material;

    List<GameObject> meshes = new List<GameObject>();
    Vector3 MarchingBoundingBoxSize = new Vector3(1, 1, 1);
    Vector3 MarchingBoundingBoxCenter = new Vector3(0, 0, 0);
    Vector3 BoundingBoxResolution = new Vector3(50, 50, 50);

    #region define the function here
    double samplingfunction(Vector3 position)
    {
        float x = position.x;
        float y = position.y;
        float z = position.z;

        //using >= 0
        return (.5 * .5 - x * x - y * y - z * z);
    }
    #endregion

    void Start()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        MarchingCubes marchingCube = new MarchingCubes();
        marchingCube.sampleProc = samplingfunction;//function goes here
        marchingCube.interpolate = true;

        marchingCube.Reset();

        //marchingCube.MarchChunk(MarchingBoundingBoxCenter,(int)BoundingBoxResolution.x, MarchingBoundingBoxSize.x/ BoundingBoxResolution.x);
        marchingCube.MarchChunk(MarchingBoundingBoxCenter, MarchingBoundingBoxSize, BoundingBoxResolution);

        IList<Vector3> verts = marchingCube.GetVertices();
        //IList<int> indices = marchingCube.GetIndices();

        //A mesh in unity can only be made up of 65000 verts.
        //Need to split the verts between multiple meshes.

        int maxVertsPerMesh = 3000; //must be divisible by 3, ie 3 verts == 1 triangle
        int numMeshes = verts.Count / maxVertsPerMesh + 1;

        for (int i = 0; i < numMeshes; i++)
        {
            List<Vector3> splitVerts = new List<Vector3>();
            List<int> splitIndices = new List<int>();

            for (int j = 0; j < maxVertsPerMesh; j++)
            {
                int idx = i * maxVertsPerMesh + j;

                if (idx < verts.Count)
                {
                    splitVerts.Add(verts[idx]);
                    splitIndices.Add(j);
                }
            }

            if (splitVerts.Count == 0) continue;

            Mesh mesh = new Mesh();
            mesh.SetVertices(splitVerts);
            mesh.SetTriangles(splitIndices, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            GameObject go = new GameObject("Mesh");
            go.transform.parent = transform;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.GetComponent<Renderer>().material = material;
            go.GetComponent<MeshFilter>().mesh = mesh;

            meshes.Add(go);
        }

        sw.Stop();
        Debug.LogFormat("Generation took {0} seconds", sw.Elapsed.TotalSeconds);
    }
}
