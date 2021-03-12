using System;
using System.Collections.Generic;
using UnityEngine;

//PavelKouril.MarchingCubesGPU

public class DensityFieldGenerator : MonoBehaviour
{
    public int BBoxResolution;
    public float BBoxSize;

    private GPUMarchingCubes mc;

    private Texture3D densityTexture;
    private Color[] colors;

    private void Start()
    {
        mc = GetComponent<GPUMarchingCubes>();
        densityTexture = new Texture3D(BBoxResolution, BBoxResolution, BBoxResolution, TextureFormat.RFloat, false);
        densityTexture.wrapMode = TextureWrapMode.Clamp;
        colors = new Color[BBoxResolution * BBoxResolution * BBoxResolution];

        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        GenerateSoil();
    }

    private void Update()
    {
        GenerateSoil();
    }

    private void GenerateSoil()
    {
        var idx = 0;
        double X, Y, Z;
        double t = Mathf.Sin(Time.time+0.5f);
        for (var z = 0; z < BBoxResolution; ++z)
        {
            for (var y = 0; y < BBoxResolution; ++y)
            {
                for (var x = 0; x < BBoxResolution; ++x, ++idx)
                {
                    X = x * (BBoxSize / BBoxResolution) - BBoxSize / 2;
                    Y = y * (BBoxSize / BBoxResolution) - BBoxSize / 2;
                    Z = z * (BBoxSize / BBoxResolution) - BBoxSize / 2;

                    //multiply by to *-1 to change from >=0 to <=0
                    var amount = samplingfunction(X,Y,Z,t)*-1;
                    colors[idx].r = (float)amount;
                }
            }
        }
        densityTexture.SetPixels(colors);
        densityTexture.Apply();

        mc.DensityTexture = densityTexture;
    }

    // Create a delegate so that other classess inject their function inside
    public delegate double SamplingFunction(double posX, double posY, double posZ, double t);
    public SamplingFunction samplingfunction;
}