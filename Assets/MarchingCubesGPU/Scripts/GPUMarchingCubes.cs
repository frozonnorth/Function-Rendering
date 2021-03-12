using UnityEngine;

//PavelKouril.MarchingCubesGPU

public class GPUMarchingCubes : MonoBehaviour
{
    public int BBoxResolution;
    public int BBoxSize;
    
    public Material mat;
    public ComputeShader MarchingCubesCS;

    public Texture3D DensityTexture { get; set; }

    private int kernelMC;

    private ComputeBuffer appendVertexBuffer;
    private ComputeBuffer argBuffer;

    private void Start()
    {
        kernelMC = MarchingCubesCS.FindKernel("MarchingCubes");
        appendVertexBuffer = new ComputeBuffer((BBoxResolution - 1) * (BBoxResolution - 1) * (BBoxResolution - 1) * 5, sizeof(float) * 18, ComputeBufferType.Append);
        argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

        MarchingCubesCS.SetInt("_gridSize", BBoxResolution);
        MarchingCubesCS.SetFloat("_isoLevel", 0.5f);

        MarchingCubesCS.SetBuffer(kernelMC, "triangleRW", appendVertexBuffer);
    }

    private void Update()
    {
        MarchingCubesCS.SetTexture(kernelMC, "_densityTexture", DensityTexture);
        appendVertexBuffer.SetCounterValue(0);

        MarchingCubesCS.Dispatch(kernelMC, BBoxResolution / 8, BBoxResolution / 8, BBoxResolution / 8);

        int[] args = new int[] { 0, 1, 0, 0 };
        argBuffer.SetData(args);

        ComputeBuffer.CopyCount(appendVertexBuffer, argBuffer, 0);

        argBuffer.GetData(args);
        args[0] *= 3;
        argBuffer.SetData(args);

        //Debug.Log("Vertex count:" + args[0]);
    }

    private void OnRenderObject()
    {
        mat.SetPass(0);
        mat.SetBuffer("triangles", appendVertexBuffer);
        mat.SetMatrix("model", Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(BBoxSize, BBoxSize, BBoxSize)));
        Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, argBuffer);
    }

    private void OnDestroy()
    {
        appendVertexBuffer.Release();
        argBuffer.Release();
    }
}