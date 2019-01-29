using System.Collections.Generic;
using UnityEngine;

public class CompliedMesh : MonoBehaviour
{
    public float t = 0f;
    public bool hastime = false;

    Material material;
    List<GameObject> meshes = new List<GameObject>();
    Vector3 MarchingBoundingBoxSize = new Vector3(10, 10, 10);
    Vector3 MarchingBoundingBoxCenter = new Vector3(0, 0, 0);
    Vector3 BoundingBoxResolution = new Vector3(10, 10, 10);
    int vertsPerGO = 3000;//must be divisible by 3, ie 3 verts == 1 triangle

    MarchingCubes marchingCube = new MarchingCubes();

    #region define the function here
    float samplingfunction(Vector3 position)
    {
        float x = position.x;
        float y = position.y;
        float z = position.z;

        //using >= 0
        return ((4.9f*t) * (4.9f*t) - x * x - y * y - z * z);
    }
    #endregion

    void Setup(GameObject go, Material material,
        bool hastime,
        Vector3 MarchingBoundingBoxSize,
        Vector3 MarchingBoundingBoxCenter,
        Vector3 BoundingBoxResolution,
        int vertsPerGO)
    {
        CompliedMesh a = go.AddComponent<CompliedMesh>();
        a.material = material;
        a.hastime = hastime;
        a.MarchingBoundingBoxSize = MarchingBoundingBoxSize;
        a.MarchingBoundingBoxCenter = MarchingBoundingBoxCenter;
        a.BoundingBoxResolution = BoundingBoxResolution;
        a.vertsPerGO = vertsPerGO;//must be divisible by 3, ie 3 verts == 1 triangle
    }
    void Start()
    {
        marchingCube.sampleProc = samplingfunction;//function goes here
        marchingCube.interpolate = true;

        GenerateMesh();
    }

    void Update()
    {
        if (hastime)
        {
            t += Time.deltaTime;
            if (t > 1) t = 0;
            GenerateMesh();
        }
    }
    void GenerateMesh()
    {
        marchingCube.Reset();
        marchingCube.MarchChunk(MarchingBoundingBoxCenter, (int)BoundingBoxResolution.x, MarchingBoundingBoxSize.x / BoundingBoxResolution.x);

        IList<Vector3> verts = marchingCube.GetVertices();
        //IList<int> indices = marchingCube.GetIndices();

        //A mesh in unity can only be made up of 65000 verts.
        //Need to split the verts between multiple meshes.
        int maxVertsPerMesh = vertsPerGO; //must be divisible by 3, ie 3 verts == 1 triangle
        int numMeshes = verts.Count / maxVertsPerMesh + 1;

        if(meshes.Count > numMeshes)
        {
            meshes.RemoveRange(meshes.Count - numMeshes - 1, meshes.Count - numMeshes);
        }
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

            if (meshes.Count <= i)
            {
                GameObject go = new GameObject("Mesh");
                go.transform.parent = transform;
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                go.GetComponent<Renderer>().material = material;
                go.GetComponent<MeshFilter>().mesh = mesh;
                meshes.Add(go);
            }
            else
            {
                meshes[i].GetComponent<MeshFilter>().mesh = mesh;
            }
        }
    }
}
