using UnityEngine;
using System.Collections.Generic;

using MathParserTK;
using System;
using System.CodeDom.Compiler;
using System.Text;
using System.Reflection;
using Microsoft.CSharp;
using System.Text.RegularExpressions;
using System.Linq;

public class ParametricMesh : MonoBehaviour
{
    public int sampleresolution_U = 100;
    public int sampleresolution_V = 100;
    public int sampleresolution_W = 100;

    public float uMinDomain = 0;
    public float uMaxDomain = 1;

    public float vMinDomain = 0;
    public float vMaxDomain = 1;

    public float wMinDomain = 0;
    public float wMaxDomain = 1;

    public bool iswireframe = false;

    [SerializeField]
    string inputCompileCode;
    [SerializeField]
    string xExpression;
    [SerializeField]
    string yExpression;
    [SerializeField]
    string zExpression;
    [SerializeField]
    bool hastime = false;
    public float timer = 0f;

    MathParser mathparser = new MathParser();

    public enum GenerationType {StringMathParser,RunTimeCompile}
    [SerializeField]
    GenerationType meshgenerationType;
    public bool MeshGenerated = false;

    public void SetFomula(string xExpression, string yExpression, string zExpression)
    {
        meshgenerationType = GenerationType.StringMathParser;
        this.xExpression = xExpression;
        this.yExpression = yExpression;
        this.zExpression = zExpression;
        inputCompileCode = null;
    }
    public void SetFomula(string inputcode)
    {
        meshgenerationType = GenerationType.RunTimeCompile;
        inputCompileCode = inputcode;
        xExpression = null;
        yExpression = null;
        zExpression = null;
    }

    public void StartDrawing()
    {
        if(meshgenerationType == GenerationType.RunTimeCompile)
        {
            CompileInputCodeAndAttachToGO(inputCompileCode);
        }
        else if (meshgenerationType == GenerationType.StringMathParser)
        {
            DrawMeshFromMathParser();
        }
    }
    void Update()
    {
        if(hastime)
        {
            timer += Time.deltaTime;
            if (timer > 1) timer = 0;
            DrawMeshFromMathParser();
        }
    }
    public void DrawMeshFromMathParser()
    {
        GetComponent<MeshFilter>().mesh = CreateParametricObject_StringMathParser(
            xExpression,yExpression,zExpression, timer,
            uMinDomain, uMaxDomain,
            vMinDomain, vMaxDomain,
            wMinDomain, wMaxDomain, 
            sampleresolution_U, sampleresolution_V,  sampleresolution_W);
        MeshGenerated = true;
    }

    Mesh CreateParametricObject_StringMathParser(
                string x, string y, string z, float t,
                float uMinDomain, float uMaxDomain,
                float vMinDomain, float vMaxDomain,
                float wMinDomain, float wMaxDomain, 
                int sampleresolution_U, int sampleresolution_V, int sampleresolution_W)
    {
        bool isusingU = false;
        bool isusingV = false;
        bool isusingW = false;
        bool isusingT = false;
        // check for u v w usage
        string test = " " + x + " " + y + " " + z + " ";
        if (Regex.IsMatch(test, "([^a-zA-Z0-9]|[\\s])u([^a-zA-Z0-9]|[\\s])"))
        {
            isusingU = true;
        }
        if (Regex.IsMatch(test, "([^a-zA-Z0-9]|[\\s])v([^a-zA-Z0-9]|[\\s])"))
        {
            isusingV = true;
        }
        if (Regex.IsMatch(test, "([^a-zA-Z0-9]|[\\s])w([^a-zA-Z0-9]|[\\s])"))
        {
            isusingW = true;
        }
        //check for time t usage
        if (Regex.IsMatch(test, "([^a-zA-Z0-9]|[\\s])t([^a-zA-Z0-9]|[\\s])"))
        {
            isusingT = true;
        }
        
        int varUsed = 0;
        if (isusingU) varUsed++;
        if (isusingV) varUsed++;
        if (isusingW) varUsed++;
        hastime = isusingT;

        Mesh mesh = new Mesh();
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

                    string xexpression = x.Replace("u", "(" + u.ToString("R") + ")");
                    string yexpression = y.Replace("u", "(" + u.ToString("R") + ")");
                    string zexpression = z.Replace("u", "(" + u.ToString("R") + ")");
                    xexpression = xexpression.Replace("v", "(" + v.ToString("R") + ")");
                    yexpression = yexpression.Replace("v", "(" + v.ToString("R") + ")");
                    zexpression = zexpression.Replace("v", "(" + v.ToString("R") + ")");
                    xexpression = xexpression.Replace("w", "(-" + w.ToString("R") + ")");
                    yexpression = yexpression.Replace("w", "(-" + w.ToString("R") + ")");
                    zexpression = zexpression.Replace("w", "(-" + w.ToString("R") + ")");
                    if (hastime)
                    {
                        xexpression = xexpression.Replace("t", "(" + timer.ToString("R") + ")");
                        yexpression = yexpression.Replace("t", "(" + timer.ToString("R") + ")");
                        zexpression = zexpression.Replace("t", "(" + timer.ToString("R") + ")");
                    }

                    float xresult = 0;
                    float yresult = 0;
                    float zresult = 0;

                    try
                    {
                        xresult = (float)mathparser.Parse(xexpression, true);
                        yresult = (float)mathparser.Parse(yexpression, true);
                        zresult = (float)mathparser.Parse(zexpression, true);
                    }
                    catch
                    {
                    }

                    vertices.Add(new Vector3(xresult, yresult, zresult));
                    if (varUsed == 3)
                    {
                        if (k == 0 && i > 0 && j > 0)//front
                        {
                            indices.Add(vertices.Count - 1 - 1);
                            indices.Add(vertices.Count - 1);
                            indices.Add(vertices.Count - 1 - sampleresolution_U);
                            indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                        }
                        if (k == sampleresolution_U - 1 && i > 0 && j > 0)//back
                        {
                            indices.Add(vertices.Count - 1);
                            indices.Add(vertices.Count - 1 - 1);
                            indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                            indices.Add(vertices.Count - 1 - sampleresolution_U);
                        }
                        if (k > 0 && i == 0 && j > 0) //left
                        {
                            indices.Add(vertices.Count - 1);
                            indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                            indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_U);
                            indices.Add(vertices.Count - 1 - sampleresolution_U);
                        }
                        if (k > 0 && i == sampleresolution_U - 1 && j > 0) //right
                        {
                            indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                            indices.Add(vertices.Count - 1);
                            indices.Add(vertices.Count - 1 - sampleresolution_U);
                            indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_U);
                        }
                        if (k > 0 && j == 0 && i > 0) //bot
                        {
                            indices.Add(vertices.Count - 1 - 1);
                            indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_U);
                            indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                            indices.Add(vertices.Count - 1);
                        }
                        if (k > 0 && j == sampleresolution_U - 1 && i > 0) //top
                        {
                            indices.Add(vertices.Count - 1);
                            indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_U);
                            indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_U);
                            indices.Add(vertices.Count - 1 - 1);
                        }
                    }
                    else if (varUsed == 2 && i > 0 && j > 0)
                    {
                        indices.Add(vertices.Count - 1 - 1);
                        indices.Add(vertices.Count - 1);
                        indices.Add(vertices.Count - 1 - sampleresolution_U);
                        indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                    }
                    else if (varUsed == 1)
                    {
                        indices.Add(vertices.Count - 1);
                    }
                }
                if (varUsed == 1)
                {
                    break;
                }
            }
            if (varUsed == 1 || varUsed == 2)
            {
                break;
            }
        }
        mesh.SetVertices(vertices);
        if (varUsed == 1)
        {
            mesh.SetIndices(indices.ToArray(), MeshTopology.LineStrip, 0);
        }
        else if (varUsed == 2)
        {
            mesh.SetIndices(indices.ToArray(), MeshTopology.Quads, 0);
        }
        else if (varUsed == 3)
        {
            mesh.SetIndices(indices.ToArray(), MeshTopology.Quads, 0);
        }
        return mesh;
    }

    void CompileInputCodeAndAttachToGO(string inputcode)
    {
        bool isusingU = false;
        bool isusingV = false;
        bool isusingW = false;
        bool isusingT = false;

        // check for u v w usage
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9]|[\\s])u([^a-zA-Z0-9]|[\\s])"))
        {
            isusingU = true;
        }
      
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9]|[\\s])v([^a-zA-Z0-9]|[\\s])"))
        {
            isusingV = true;
        }
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9]|[\\s])w([^a-zA-Z0-9]|[\\s])"))
        {
            isusingW = true;
        }
        //check for time t usage
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9]|[\\s])t([^a-zA-Z0-9]|[\\s])"))
        {
            isusingT = true;
        }

        Dictionary<string, string> replacestrings =
            new Dictionary<string, string>
            {
                { "cos", "Mathf.Cos" },
                { "sin", "Mathf.Sin" },
                { "tan", "Mathf.Tan" },
                
                { "acos", "Mathf.Acos" },
                { "asin", "Mathf.Asin" },
                { "atan", "Mathf.Atan" },

                { "abs", "Mathf.Abs" },
                { "sqrt", "Mathf.Sqrt" },
                { "pow", "Mathf.Pow" },

                { "floor", "Mathf.Floor" },
                { "ceil", "Mathf.Ceil" },
                { "log", "Mathf.Log" },
                { "exp", "Mathf.Exp" },

                { "pi", "Mathf.PI" },
            };
        foreach (KeyValuePair<string, string> entry in replacestrings)
        {
            inputcode = Regex.Replace(inputcode, "([^a-zA-Z0-9]|[\\s])" + entry.Key + "([^a-zA-Z0-9]|[\\s])", "$1" + entry.Value + "$2");
        }

        var assembly = Compile(@"
            using UnityEngine;
            using System.Collections.Generic;

            public class CompliedMesh : MonoBehaviour
            {
                public float t = 0f;
                public bool hastime = false;

                public int varUsed;
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
                    GetComponent<MeshFilter>().mesh = CreateParametricObject(varUsed, t,
                                                        uMinDomain, uMaxDomain,
                                                        vMinDomain, vMaxDomain,
                                                        wMinDomain, wMaxDomain,
                                                        sampleresolution_U,  sampleresolution_V,  sampleresolution_W,iswireframe);
                    Main.Instance.viewcamera.SetTarget(gameObject);
                }
                
                void Update()
                {
                    if(hastime)
                    {
                        t += Time.deltaTime;
                        if (t > 1) t = 0;
                        GetComponent<MeshFilter>().mesh = CreateParametricObject(varUsed, t,
                                                        uMinDomain, uMaxDomain,
                                                        vMinDomain, vMaxDomain,
                                                        wMinDomain, wMaxDomain,
                                                        sampleresolution_U,  sampleresolution_V,  sampleresolution_W,iswireframe);
                    }
                }
                public static void Setup(GameObject go,
                    int varUsed,bool hastime,
                    float uMinDomain,float uMaxDomain,
                    float vMinDomain,float vMaxDomain,
                    float wMinDomain,float wMaxDomain,
                    int sampleresolution_U, int sampleresolution_V, int sampleresolution_W,bool iswireframe)
                {
                    CompliedMesh a =  go.AddComponent<CompliedMesh>();
                    a.varUsed = varUsed;
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

                    go.GetComponent<MeshFilter>().mesh = CreateParametricObject(
                                                        varUsed, 0,
                                                        uMinDomain, uMaxDomain,
                                                        vMinDomain, vMaxDomain,
                                                        wMinDomain, wMaxDomain,
                                                        sampleresolution_U,  sampleresolution_V,  sampleresolution_W,iswireframe);
                }

                public static Mesh CreateParametricObject
                    (int varUsed,float t,
                    float uMinDomain,float uMaxDomain,
                    float vMinDomain,float vMaxDomain,
                    float wMinDomain,float wMaxDomain,
                    int sampleresolution_U, int sampleresolution_V, int sampleresolution_W,bool iswireframe)
                {
                    Mesh mesh = new Mesh();
                    if(SystemInfo.supports32bitsIndexBuffer)
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

                                float x = 0;
                                float y = 0;
                                float z = 0;

                                " + inputcode + @"

                                vertices.Add(new Vector3(x, y, z));

                                if (varUsed == 3)
                                {
                                    if (k == 0 && i > 0 && j > 0)//front
                                    {
                                        if(iswireframe)
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
                                        if(iswireframe)
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
                                        if(iswireframe)
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
                                        if(iswireframe)
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
                                        if(iswireframe)
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
                                        if(iswireframe)
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
                                else if (varUsed == 2 && i > 0 && j > 0)
                                {
                                    if(iswireframe)
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
                                else if (varUsed == 1)
                                {
                                    indices.Add(vertices.Count - 1);
                                }
                            }
                            if (varUsed == 1)
                            {
                                break;
                            }
                        }
                        if (varUsed == 1 || varUsed == 2)
                        {
                            break;
                        }
                    }
                    mesh.SetVertices(vertices);
                    if (varUsed == 1)
                    {
                        mesh.SetIndices(indices.ToArray(), MeshTopology.LineStrip, 0);
                    }
                    else if (varUsed == 2)
                    {
                        if(iswireframe)
                        {
                            mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
                        }
                        else
                        {
                            mesh.SetIndices(indices.ToArray(), MeshTopology.Quads, 0);
                            //mesh.RecalculateNormals(45);
                            mesh.RecalculateNormals();
                        }
                    }
                    else if (varUsed == 3)
                    {
                        if(iswireframe)
                        {
                            mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
                        }
                        else
                        {
                            mesh.SetIndices(indices.ToArray(), MeshTopology.Quads, 0);
                            //mesh.RecalculateNormals(45);
                            mesh.RecalculateNormals();
                        }
                    }
                    //mesh.CreateUV();
                    //mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    return mesh;
                }
            }"
        );


        //get the assembly at runtime
        var runtimeType = assembly.GetType("CompliedMesh");
        //get the method to execute and create a delegate so that we can execute it
        var method = runtimeType.GetMethod("Setup");
        var del = (Func<GameObject,int, bool, float,float,float,float,float,float, int, int, int,bool>)
                      Delegate.CreateDelegate(
                          typeof(Func<GameObject, int, bool, float, float, float, float, float, float, int, int, int, bool>),
                          method);
        int varUsed = 0;
        if (isusingU) varUsed++;
        if (isusingV) varUsed++;
        if (isusingW) varUsed++;

        del.Invoke(gameObject, varUsed, isusingT, uMinDomain, uMaxDomain, vMinDomain, vMaxDomain, wMinDomain, wMaxDomain, sampleresolution_U, sampleresolution_V, sampleresolution_W, iswireframe);
        MeshGenerated = true;
    }

    delegate void Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12,T13>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8, T9 p9, T10 p10, T11 p11, T12 p12, T13 p13);

    static Assembly Compile(string source)
    {
        AppDomain domain = AppDomain.CurrentDomain;
        string[] assemblyReferences = domain
            .GetAssemblies()
            .Where(a => !(a is System.Reflection.Emit.AssemblyBuilder) && !string.IsNullOrEmpty(a.Location))
            .Select(a => a.Location)
            .ToArray();

        CompilerParameters param = new CompilerParameters();
        param.GenerateExecutable = false;
        param.GenerateInMemory = true;
        param.ReferencedAssemblies.AddRange(assemblyReferences);

        // Compile the source
        //var provider = new CSharpCodeProvider(); from .net 
        var compiler = new CodeCompiler(); // from aeroson
        var result = compiler.CompileAssemblyFromSource(param, source);

        StringBuilder msg = new StringBuilder();
        foreach (CompilerError error in result.Errors)
        {
            if(error.ErrorNumber == "CS0219")
            {
                //CS0219 = decalred varaible but is unused suppress this warning
                continue;
            }
            msg.AppendFormat("Error ({0}): {1}\n",
                error.ErrorNumber, error.ErrorText);
        }
        if (msg.Length != 0)
        {
            throw new Exception(msg.ToString());
        }

        // Return the assembly
        return result.CompiledAssembly;
    }
}
