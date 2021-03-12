//This script contains the copied portion that compiles the code to produce generated parametric mesh,
//found in FunctionGeneratedMesh.cs
//This may or may not be the updated code version, and is only used for testing purposes

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using UnityEngine;

public class ParametricTest : MonoBehaviour
{
    public bool isusingU;
    public bool isusingV;
    public bool isusingW;

    public float t = 0f;
    public bool hastime = false;

    public float uMinDomain;
    public float uMaxDomain;

    public float vMinDomain;
    public float vMaxDomain;

    public float wMinDomain;
    public float wMaxDomain;

    public int sampleresolution_U;
    public int sampleresolution_V;
    public int sampleresolution_W;

    public bool iswireframe;

    void Start()
    {
        isusingU = true;
        isusingV = true;
        isusingW = true;
        uMinDomain = 0;
        uMaxDomain = 1;
        vMinDomain = 0;
        vMaxDomain = 1;
        wMinDomain = 0;
        wMaxDomain = 1;
        sampleresolution_U = 6;
        sampleresolution_V = 6;
        sampleresolution_W = 6;
        iswireframe = false;

        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        GetComponent<MeshFilter>().mesh = CreateParametricObject(isusingU, isusingV, isusingW, t,
                                            uMinDomain, uMaxDomain,
                                            vMinDomain, vMaxDomain,
                                            wMinDomain, wMaxDomain,
                                            sampleresolution_U, sampleresolution_V, sampleresolution_W, iswireframe);

        sw.Stop();
        Debug.LogFormat("Generation took {0} seconds", sw.Elapsed.TotalSeconds);
        //Main.Instance.viewcamera.SetExamineTarget(gameObject);
    }

    void Update()
    {
        if (hastime)
        {
            t += Time.deltaTime;
            if (t > 1) t = 0;
            GetComponent<MeshFilter>().mesh = CreateParametricObject(isusingU, isusingV, isusingW, t,
                                            uMinDomain, uMaxDomain,
                                            vMinDomain, vMaxDomain,
                                            wMinDomain, wMaxDomain,
                                            sampleresolution_U, sampleresolution_V, sampleresolution_W, iswireframe);
        }
    }
    public static void Setup(GameObject go,
        bool isusingU, bool isusingV, bool isusingW, bool hastime,
        float uMinDomain, float uMaxDomain,
        float vMinDomain, float vMaxDomain,
        float wMinDomain, float wMaxDomain,
        int sampleresolution_U, int sampleresolution_V, int sampleresolution_W, bool iswireframe)
    {
        ParametricTest a = go.AddComponent<ParametricTest>();
        a.isusingU = isusingU;
        a.isusingV = isusingV;
        a.isusingW = isusingW;
        a.hastime = hastime;
        a.uMinDomain = uMinDomain;
        a.uMaxDomain = uMaxDomain;
        a.vMinDomain = vMinDomain;
        a.vMaxDomain = vMaxDomain;
        a.wMinDomain = wMinDomain;
        a.wMaxDomain = wMaxDomain;
        a.sampleresolution_U = sampleresolution_U;
        a.sampleresolution_V = sampleresolution_V;
        a.sampleresolution_W = sampleresolution_W;
        a.iswireframe = iswireframe;
    }

    public static Mesh CreateParametricObject
        (bool isusingU, bool isusingV, bool isusingW, float t,
        float uMinDomain, float uMaxDomain,
        float vMinDomain, float vMaxDomain,
        float wMinDomain, float wMaxDomain,
        int sampleresolution_U, int sampleresolution_V, int sampleresolution_W, bool iswireframe)
    {

        //Some checks before creating mesh
        int varUsed = 0;
        if (isusingU) varUsed++;
        else sampleresolution_U = 0;
        if (isusingV) varUsed++;
        else sampleresolution_V = 0;
        if (isusingW) varUsed++;
        else sampleresolution_W = 0;

        Mesh mesh = new Mesh();
        if (SystemInfo.supports32bitsIndexBuffer)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        else
        {
            //already using default Uint16
        }
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        sampleresolution_U += 1;
        sampleresolution_V += 1;
        sampleresolution_W += 1;

        for (int k = 0; k < sampleresolution_W; k++)
        {
            float w = uMinDomain + k * ((wMaxDomain - wMinDomain) / (sampleresolution_W - 1));
            for (int j = 0; j < sampleresolution_V; j++)
            {
                float v = uMinDomain + j * ((vMaxDomain - vMinDomain) / (sampleresolution_V - 1));
                for (int i = 0; i < sampleresolution_U; i++)
                {
                    float u = uMinDomain + i * ((uMaxDomain - uMinDomain) / (sampleresolution_U - 1));

                    double x = 0;
                    double y = 0;
                    double z = 0;
                    
                    //x = new Expression(u.ToString()).calculate();
                    //y = new Expression(v.ToString()).calculate();
                    //z = new Expression(w.ToString()).calculate();

                    //User input code goes here
                    x = u;
                    y = v;
                    z = w;
                    //x = mathparser.Parse(u.ToString());
                    //y = mathparser.Parse(v.ToString());
                    //z = mathparser.Parse(w.ToString());

                    //x = eval.Evaluate(u.ToString());
                    //y = eval.Evaluate(v.ToString());
                    //z = eval.Evaluate(w.ToString());

                    //this little code reverses the Z this changes left hand coordinates to right hand coordinates
                    z *= -1;

                    vertices.Add(new Vector3((float)x, (float)y, (float)z));

                    if (varUsed == 3)
                    {
                        if (k == 0 && i > 0 && j > 0)//front
                        {
                            if (iswireframe)
                            {
                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1);

                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);

                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1 - 1);
                            }
                            else
                            {
                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                            }
                        }
                        if (k == sampleresolution_U - 1 && i > 0 && j > 0)//back
                        {
                            if (iswireframe)
                            {
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - 1);

                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);

                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1);
                            }
                            else
                            {
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U);
                            }
                        }
                        if (k > 0 && i == 0 && j > 0) //left
                        {
                            if (iswireframe)
                            {
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1);
                            }
                            else
                            {
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U);
                            }
                        }
                        if (k > 0 && i == sampleresolution_U - 1 && j > 0) //right
                        {
                            if (iswireframe)
                            {
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1);

                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                            }
                            else
                            {
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_U);
                            }
                        }
                        if (k > 0 && j == 0 && i > 0) //bot
                        {
                            if (iswireframe)
                            {
                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_U);

                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1);

                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - 1);
                            }
                            else
                            {
                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1);
                            }

                        }
                        if (k > 0 && j == sampleresolution_U - 1 && i > 0) //top
                        {
                            if (iswireframe)
                            {
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);

                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_U);

                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - 1);

                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1);
                            }
                            else
                            {
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_U);
                                indices.Add(vertices.Count - 1 - 1);
                            }
                        }
                    }

                    //In order to allow for any parmetric u v w to be used in any order,
                    //we need to know which is used/not used for generating curves
                    //so that we can grab the correct loop index to do the triangle indexing

                    else if (varUsed == 2)
                    {
                        //i=u,j=v;k=w;
                        int loopindex1 = i;
                        int loopindex2 = j;
                        int sampleres = sampleresolution_U;
                        if (isusingU)
                        {
                            loopindex1 = i;
                            sampleres = sampleresolution_U;
                            if (isusingV)
                            {
                                loopindex2 = j;
                            }
                            else if (isusingW)
                            {
                                loopindex2 = k;
                            }
                        }
                        else
                        {
                            sampleres = sampleresolution_V;
                            loopindex1 = k;
                            loopindex2 = j;
                        }
                        if (loopindex1 > 0 && loopindex2 > 0)
                        {
                            if (iswireframe)
                            {
                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1);

                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleres);

                                indices.Add(vertices.Count - 1 - sampleres);
                                indices.Add(vertices.Count - 1 - 1 - sampleres);

                                indices.Add(vertices.Count - 1 - 1 - sampleres);
                                indices.Add(vertices.Count - 1 - 1);
                            }
                            else
                            {
                                indices.Add(vertices.Count - 1 - 1);
                                indices.Add(vertices.Count - 1);
                                indices.Add(vertices.Count - 1 - sampleres);
                                indices.Add(vertices.Count - 1 - 1 - sampleres);
                            }
                        }
                    }
                    else if (varUsed == 1)
                    {
                        indices.Add(vertices.Count - 1);
                    }
                }
            }
        }
        mesh.SetVertices(vertices);
        if (varUsed == 1)
        {
            mesh.SetIndices(indices.ToArray(), MeshTopology.LineStrip, 0);
        }
        else if (varUsed == 2)
        {
            if (iswireframe)
            {
                mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            }
            else
            {
                mesh.SetIndices(indices.ToArray(), MeshTopology.Quads, 0);
                mesh.RecalculateNormals();
            }
        }
        else if (varUsed == 3)
        {
            if (iswireframe)
            {
                mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            }
            else
            {
                mesh.SetIndices(indices.ToArray(), MeshTopology.Quads, 0);
                mesh.RecalculateNormals();
            }
        }
        mesh.RecalculateBounds();
        return mesh;
    }
    void OnDestroy()
    {
        GetComponent<MeshFilter>().mesh = null;
    }
}