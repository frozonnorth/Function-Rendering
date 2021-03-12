using System;
using System.Collections.Generic;
using UnityEngine;

public class ImplicitTest : MonoBehaviour
{
    public bool hastime = false;
    float timer = 0f;

    MarchingCubes marchingCube = new MarchingCubes();

    Vector3 MarchingBoundingBoxSize = new Vector3(10, 10, 10);
    Vector3 MarchingBoundingBoxCenter = new Vector3(0, 0, 0);
    Vector3 BoundingBoxResolution = new Vector3(10, 10, 10);

    public float Timecycle = 1;
    public float TimeMinRange = 0;
    public float TimeMaxRange = 1;

    List<GameObject> meshes = new List<GameObject>();
    int vertsPerGO = 24000;//must be divisible by 3, ie 3 verts == 1 triangle

    //define the function here
    double samplingfunction(Vector3 position)
    {
        double x = position.x;
        double y = position.y;
        double z = -position.z;
        double t = timer * (TimeMaxRange - TimeMinRange) + TimeMinRange;

        //using >= 0 or is it <=?
        //Example of user input code is
        return ((4.8f*t) * (4.8f*t) - x * x - y * y - z * z);
    }

    public static void Setup(GameObject go,
        bool hastime, float Timecycle, float TimeMinRange, float TimeMaxRange,
        Vector3 MarchingBoundingBoxSize,
        Vector3 MarchingBoundingBoxCenter,
        Vector3 BoundingBoxResolution,
        int vertsPerGO)
    {
        ImplicitTest a = go.AddComponent<ImplicitTest>();
        a.hastime = hastime;
        a.Timecycle = Timecycle;
        a.TimeMinRange = TimeMinRange;
        a.TimeMaxRange = TimeMaxRange;
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

        //Main.Instance.viewcamera.SetExamineTarget(gameObject);

        addGridOverlay();
    }

    void Update()
    {
        if (hastime)
        {
            timer += Time.deltaTime / Mathf.Abs(Timecycle);
            if (timer > 1) timer = 0;

            GenerateMesh();
        }
    }
    void GenerateMesh()
    {
        marchingCube.Reset();
        marchingCube.MarchChunk(MarchingBoundingBoxCenter, MarchingBoundingBoxSize, BoundingBoxResolution);

        IList<Vector3> verts = marchingCube.GetVertices();
        IList<int> indices = marchingCube.GetIndices();

        //A mesh in unity can only be made up of 65000 verts.
        //Need to split the verts between multiple meshes.
        int maxVertsPerMesh = vertsPerGO; //must be divisible by 3, ie 3 verts == 1 triangle
        int numMeshes = verts.Count / maxVertsPerMesh + 1;

        //if(meshes.Count > numMeshes)
        //{
        //    meshes.RemoveRange(meshes.Count - numMeshes - 1, meshes.Count - numMeshes);
        //}
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

            Mesh mesh;
            if (meshes.Count <= i)
            {
                mesh = new Mesh();
                mesh.SetVertices(splitVerts);
                mesh.SetTriangles(splitIndices, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                GameObject go = new GameObject();
                go.transform.parent = transform;
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                go.GetComponent<Renderer>().material = GetComponent<Renderer>().material;
                go.GetComponent<MeshFilter>().mesh = mesh;
                meshes.Add(go);
            }
            else
            {
                mesh = meshes[i].GetComponent<MeshFilter>().mesh;
                mesh.Clear();
                mesh.SetVertices(splitVerts);
                mesh.SetTriangles(splitIndices, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }
        }
        for (int i = numMeshes; i < meshes.Count; i++)
        {
            meshes[i].GetComponent<MeshFilter>().mesh.Clear();
        }
    }
    void OnDestroy()
    {
        foreach (GameObject go in meshes)
        {
            Destroy(go);
        }
    }

    #region helper debug stuff for boundingbox visualization
    void addGridOverlay()
    {
        GridOverlayGizmo grid = gameObject.AddComponent<GridOverlayGizmo>();
        grid.Show = true;
        grid.Centralized = true;
        if (BoundingBoxResolution.x <= 10)
        {
            grid.GridsizeX = (int)BoundingBoxResolution.x;
            grid.GridSizeMultipllier = MarchingBoundingBoxSize.x / BoundingBoxResolution.x;
        }
        else
        {
            grid.GridsizeX = (int)BoundingBoxResolution.x / 10;
            grid.GridSizeMultipllier = MarchingBoundingBoxSize.x / (BoundingBoxResolution.x / 10);
        }
        if (BoundingBoxResolution.y <= 10)
        {
            grid.GridsizeY = (int)BoundingBoxResolution.y;
        }
        else
        {
            grid.GridsizeY = (int)BoundingBoxResolution.y / 10;
        }
        if (BoundingBoxResolution.z <= 10)
        {
            grid.GridsizeZ = (int)BoundingBoxResolution.z;
        }
        else
        {
            grid.GridsizeZ = (int)BoundingBoxResolution.z / 10;
        }
        grid.GridPosition = MarchingBoundingBoxCenter;
        grid.mainColor = Color.cyan * new Vector4(1, 1, 1, 75 / 255f);
    }
    #endregion
}