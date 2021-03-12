using UnityEngine;

using System;
using System.CodeDom.Compiler;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

//Original marching cube 
//https://github.com/ttammear/unitymcubes
/// <summary>
/// class that represent a function generated mesh, using parametric or implicit functions
/// </summary>
public class FunctionGeneratedMesh : MonoBehaviour
{
    //Parametric or implicit mode
    public int mode = 0;

    public bool MeshGenerated = false;

    //User input code, to be by the class(main.cs) who creates this component
    public string inputCode = "";

    //public parametric properties, to be changed by the class(main.cs) who created this component
    public int sampleresolution_U = 1, sampleresolution_V = 1, sampleresolution_W = 1;

    public float uMinDomain = 0, uMaxDomain = 1;
    public float vMinDomain = 0, vMaxDomain = 1;
    public float wMinDomain = 0, wMaxDomain = 1;

    //public implicit properties, this can be changed by the class(main.cs) who created the object
    public Vector3 MarchingBoundingBoxSize = new Vector3(100, 100, 100);
    public Vector3 MarchingBoundingBoxCenter = new Vector3(0, 0, 0);
    public Vector3 BoundingBoxResolution = new Vector3(100, 100, 100);

    public float Timecycle = 1;
    public float TimeMinRange = 0;
    public float TimeMaxRange = 1;

    public Material mat;
    public ComputeShader computeshader;

    public void StartDrawing()
    {
        //0 - parametric , 1 - implicit
        if (mode == 0)
        {
            CompileParametricCodeAndAttachToGO(inputCode);
            MeshGenerated = true;
        }
        else if (mode == 1)
        {
            CompileImplicitCodeAndAttachToGO(inputCode);
            //CompileGPUImplicitCodeAndAttachToGO(inputCode);
            MeshGenerated = true;
        }
    }

    /// <summary>
    /// Function that takes user typed code and process it to work with unity
    /// This adds support for several Math functions,custom user variables, "^" power sign
    /// </summary>
    string DoStringFormat(string inputcode)
    {
        string previnputcode;
        //declare a set of special words to be replaced with the unity math version
        Dictionary<string, string> replacestrings =
            new Dictionary<string, string>
            {
                { "cos", "Math.Cos" },
                { "sin", "Math.Sin" },
                { "tan", "Math.Tan" },

                { "acos", "Math.Acos" },
                { "asin", "Math.Asin" },
                { "atan", "Math.Atan" },

                { "cosh", "Math.Cosh" },
                { "sinh", "Math.Sinh" },
                { "tanh", "Math.Tanh" },

                { "atan2", "Math.Atan2" },

                { "abs", "Math.Abs" },
                { "sqrt", "Math.Sqrt" },
                { "pow", "Math.Pow" },

                { "floor", "Math.Floor" },
                { "ceil", "Math.Ceil" },
                { "log", "Math.Log" },
                { "exp", "Math.Exp" },

                { "pi", "Math.PI" },
                //{ "E", "Math.E" },

                //using another class of min,max to allow for unlimited arguments in min() max(),
                { "min", "Maths.Min" },
                { "max", "Maths.Max" },
            };

        //replace
        foreach (KeyValuePair<string, string> entry in replacestrings)
        {
            //This finds the particular key define in the dictonary in inputcode
            //which LHS has a space and any charater not alphanumberic(means symbol character) 
            //and same for RHS,
            //This replaces the particular key with the value
            do
            {
                previnputcode = inputcode;
                inputcode = Regex.Replace(inputcode, "([^a-zA-Z0-9][\\s]*)" + entry.Key + "([\\s]*[^a-zA-Z0-9])", "$1" + entry.Value + "$2");

            } while (!previnputcode.Equals(inputcode));
        }

        //Special Replace to include user created variables
        //This check the starting charaters before the = sign to see if valid varaible
        //valid varaible is accepted if its a single charater but not uvwxyzt or a number 0-9 charaters 
        //valid varaible is accepted if it starts with an alphabet only or underscore (cannot start with digit) following by
        //[\\s]* denotes any amount of space between the user decalred varaible and equal sign is accpeted
        //one more alphanumberic charaters
        //'u=' will not be accepted
        //'1=' will not be accepted
        //'1u=' will not be accepted
        //'uu=' will be accepted
        //'u1=' will be accepted
        //'u1u=' will be accepted
        inputcode = Regex.Replace(inputcode, @"\n*(([^uvwxyzt0-9 ]|([a-zA-Z_][a-zA-Z0-9_]+))([\s])*)\=(.+)", "double $1=$5");

        //Special Replace to include the '^' symbol without brackets
        //(0 |[1 - 9][0 - 9] *)(.(0 |[0 - 9]*[1 - 9]))? - searches for valid numbers
        // ([a-zA-Z_][a-zA-Z0-9_]*) - searches for a variable
        // ((Math\\..*)?\\((?:[^()]|(?<counter>\\()|(?<-counter>\\)))+(?(counter)(?!))\\)) - searches for the outter most bracket that is balanced with an optional Math.Pow or other functions
        // [\\s]* - searches for leading or trailing spaces, as sometimes uers might spacing their code between varaibles
        // so the '^' symbol LHS have to be a valid number or a valid varaible, then it do replacement
        // otherwise it will not do replacement, and runtime compile will come out error.
        //Note: becareful of spaces when using regex, spaces is also taken into account
        //Note: $2 means refrence to the content matching the second group
        //Note: $9 means refrence to the content matching the ninth group
        //For this to work, we have to use .net 4.0 runtime version, .net 3.5 runtime somehow doesnt allow work with "(?(counter)(?!)"
        do
        {
            previnputcode = inputcode;
            inputcode = Regex.Replace(inputcode,

            "(((0|[1-9][0-9]*)(\\.(0|[0-9]*[1-9]))?|([a-zA-Z_][a-zA-Z0-9_]*)|((Maths?\\.[a-zA-Z0-9]*)?\\((?:[^()]|(?<counter>\\()|(?<-counter>\\)))+(?(counter)(?!))\\)))[\\s]*)"

            + "\\^" +

            "([\\s]*((0|[1-9][0-9]*)(\\.(0|[0-9]*[1-9]))?|([a-zA-Z_][a-zA-Z0-9_]*)|((Maths?\\.[a-zA-Z0-9]*)?\\((?:[^()]|(?<counter>\\()|(?<-counter>\\)))+(?(counter)(?!))\\))))",

            "Math.Pow($2,$9)");
        }
        while (!previnputcode.Equals(inputcode));

        return inputcode;
    }

    //Huge function that compiles the input code,attach the new compiled code to the gameobject and the code draws and updates the mesh
    void CompileParametricCodeAndAttachToGO(string inputcode)
    {
        //Note we need to check for u,v,w,t at the beginning to see what object to draw
        //Curves,surfaces or solids.
        //This checking can be done within the CompliedMesh runtime compiled script but
        //it is better to delegate all preprocessing at here 
        //and CompiledMesh just runs the algorithm

        bool isusingU = false;
        bool isusingV = false;
        bool isusingW = false;
        bool isusingT = false;

        // check the string for parameter "u"
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9]|[\\s])u([^a-zA-Z0-9]|[\\s])"))
        {
            isusingU = true;
        }
        // check the string for parameter "v"
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9]|[\\s])v([^a-zA-Z0-9]|[\\s])"))
        {
            isusingV = true;
        }
        // check the string for parameter "w"
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9]|[\\s])w([^a-zA-Z0-9]|[\\s])"))
        {
            isusingW = true;
        }
        // check the string for time "t" usage
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9_]|[\\s])t([^a-zA-Z0-9_]|[\\s])"))
        {
            isusingT = true;
        }

        int counter = 0;
        if (isusingU) counter++;
        if (isusingV) counter++;
        if (isusingW) counter++;
        //Special settings for curves
        if (counter == 1)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.material.SetColor("_EmissionColor", renderer.material.GetColor("_Color"));
        }


        inputcode =DoStringFormat(inputcode);

        var assembly = Compile(@"
            using System;
            using System.Collections.Generic;
            using UnityEngine;

            public class CompliedMesh : MonoBehaviour
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

                public float Timecycle;
                public float TimeMinRange;
                public float TimeMaxRange;

                public MeshFilter meshfilter;

                void Start()
                {
                    meshfilter = GetComponent<MeshFilter>();

                    CreateParametricObject(meshfilter.mesh,isusingU,isusingV,isusingW, 
                                            t,
                                            uMinDomain, uMaxDomain,
                                            vMinDomain, vMaxDomain,
                                            wMinDomain, wMaxDomain,
                                            sampleresolution_U,  sampleresolution_V,  sampleresolution_W);

                    Main.Instance.viewcamera.SetExamineTarget(gameObject);
                    Main.Instance.coordinateAxes.ScaleToObject(gameObject);
                }
                
                void Update()
                {
                    if(hastime)
                    {
                        t += Time.deltaTime/Mathf.Abs(Timecycle);
                        if (t > 1)  t = 0;
                        CreateParametricObject(meshfilter.mesh,isusingU,isusingV,isusingW, 
                                                t * (TimeMaxRange - TimeMinRange) + TimeMinRange,
                                                uMinDomain, uMaxDomain,
                                                vMinDomain, vMaxDomain,
                                                wMinDomain, wMaxDomain,
                                                sampleresolution_U,  sampleresolution_V,  sampleresolution_W);
                    }
                }
                public static void Setup(GameObject go,
                    bool isusingU, bool isusingV, bool isusingW, 
                    bool hastime,float Timecycle,float TimeMinRange,float TimeMaxRange,
                    float uMinDomain,float uMaxDomain,
                    float vMinDomain,float vMaxDomain,
                    float wMinDomain,float wMaxDomain,
                    int sampleresolution_U, int sampleresolution_V, int sampleresolution_W)
                {
                    CompliedMesh a =  go.AddComponent<CompliedMesh>();
                    a.isusingU = isusingU;
                    a.isusingV = isusingV;
                    a.isusingW = isusingW;
                    a.hastime = hastime;
                    a.Timecycle = Timecycle;
                    a.TimeMinRange = TimeMinRange;
                    a.TimeMaxRange = TimeMaxRange;
                    a.uMinDomain = uMinDomain;
                    a.uMaxDomain = uMaxDomain;
                    a.vMinDomain = vMinDomain;
                    a.vMaxDomain = vMaxDomain;
                    a.wMinDomain = wMinDomain;
                    a.wMaxDomain = wMaxDomain;
                    a.sampleresolution_U = sampleresolution_U;
                    a.sampleresolution_V = sampleresolution_V;
                    a.sampleresolution_W = sampleresolution_W;
                }

                public Mesh CreateParametricObject
                    (Mesh inmesh,
                    bool isusingU, bool isusingV, bool isusingW,
                    float t,
                    float uMinDomain,float uMaxDomain,
                    float vMinDomain,float vMaxDomain,
                    float wMinDomain,float wMaxDomain,
                    int sampleresolution_U, int sampleresolution_V, int sampleresolution_W)
                {
    
                    //Some checks before creating mesh
                    int varUsed = 0;
                    if(isusingU) varUsed++;
                    else sampleresolution_U = 0;
                    if(isusingV) varUsed++;
                    else sampleresolution_V = 0;
                    if(isusingW) varUsed++;
                    else sampleresolution_W = 0;
                    
                    Mesh mesh = inmesh;
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

                                double x = 0;
                                double y = 0;
                                double z = 0;

                                //User input code goes here
                                " + inputcode + @"

                                //this little code reverses the Z this changes left hand coordinates to right hand coordinates
                                z*=-1;

                                vertices.Add(new Vector3((float)x, (float)y, (float)z));

                                //Drawing Clockwise
                                if (varUsed == 3)
                                {
                                    if (k == 0 && i > 0 && j > 0)//front
                                    {
                                        //topright botright botleft topleft
                                        indices.Add(vertices.Count - 1);
                                        indices.Add(vertices.Count - 1 - sampleresolution_U);
                                        indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                                        indices.Add(vertices.Count - 1 - 1);
                                    }
                                    if (k == sampleresolution_W - 1 && i > 0 && j > 0)//back
                                    {
                                        //topright botright botleft topleft
                                        indices.Add(vertices.Count - 1 - 1);
                                        indices.Add(vertices.Count - 1 - 1 - sampleresolution_U);
                                        indices.Add(vertices.Count - 1 - sampleresolution_U);
                                        indices.Add(vertices.Count - 1);
                                    }
                                    if (k > 0 && i == 0 && j > 0) //left
                                    {
                                        //topleft topright botright botleft
                                        indices.Add(vertices.Count - 1);
                                        indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_V);
                                        indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_V);
                                        indices.Add(vertices.Count - 1 - sampleresolution_U);
                                    }
                                    if (k > 0 && i == sampleresolution_U - 1 && j > 0) //right
                                    {
                                        indices.Add(vertices.Count - 1 - sampleresolution_U);
                                        indices.Add(vertices.Count - 1 - sampleresolution_U - sampleresolution_U * sampleresolution_V);
                                        indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_V);
                                        indices.Add(vertices.Count - 1);
                                    }
                                    if (k > 0 && j == 0 && i > 0) //bot
                                    {
                                        indices.Add(vertices.Count - 1);
                                        indices.Add(vertices.Count - 1 - 1);
                                        indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_V);
                                        indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_V);
                                    }
                                    if (k > 0 && j == sampleresolution_V - 1 && i > 0) //top
                                    {
                                        indices.Add(vertices.Count - 1 - sampleresolution_U * sampleresolution_V);
                                        indices.Add(vertices.Count - 1 - 1 - sampleresolution_U * sampleresolution_V);
                                        indices.Add(vertices.Count - 1 - 1);
                                        indices.Add(vertices.Count - 1);
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
                                    if(isusingU)
                                    {
                                        loopindex1 = i;
                                        sampleres = sampleresolution_U;
                                        if(isusingV)
                                        {
                                            loopindex2 = j;
                                        }
                                        else if(isusingW)
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
                                    if(loopindex1 > 0 && loopindex2 > 0)
                                    {
                                        indices.Add(vertices.Count - 1 - 1);
                                        indices.Add(vertices.Count - 1);
                                        indices.Add(vertices.Count - 1 - sampleres);
                                        indices.Add(vertices.Count - 1 - 1 - sampleres);
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
                    else if (varUsed == 2 || varUsed == 3)
                    {
                        mesh.SetIndices(indices.ToArray(), MeshTopology.Quads, 0);
                        mesh.RecalculateNormals();
                    }
                    mesh.RecalculateBounds();
                    return mesh;
                }
                void OnDestroy()
                {
                    GetComponent<MeshFilter>().mesh = null;
                }
            }"
        );
        //get the assembly at runtime
        var runtimeType = assembly.GetType("CompliedMesh");//this name have to match the name of the compiling code above
        //get the method ("Setup") and create a delegate so that we can execute it to tell the script to start
        var method = runtimeType.GetMethod("Setup");

        var del = (Func<GameObject, bool, bool, bool, bool, float, float, float, float, float, float, float, float, float, int, int, int>)
                      Delegate.CreateDelegate(
                          typeof(Func<GameObject, bool, bool, bool, bool, float, float, float, float, float, float, float, float, float, int, int, int>),
                          method);

        del.Invoke(gameObject, isusingU, isusingV, isusingW, isusingT, Timecycle, TimeMinRange, TimeMaxRange, uMinDomain, uMaxDomain, vMinDomain, vMaxDomain, wMinDomain, wMaxDomain, sampleresolution_U, sampleresolution_V, sampleresolution_W);
    }
    delegate void Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8, T9 p9, T10 p10, T11 p11, T12 p12, T13 p13, T14 p14, T15 p15, T16 p16, T17 p17);

    void CompileImplicitCodeAndAttachToGO(string inputcode)
    {
        

        bool isusingT = false;
        // check the string for time "t" usage
        if (Regex.IsMatch(inputcode, "([^a-zA-Z0-9_]|[\\s])t([^a-zA-Z0-9_]|[\\s])"))
        {
            isusingT = true;
        }

        inputcode = DoStringFormat(inputcode);

        var assembly = Compile(@"
            using System;
            using System.Collections.Generic;
            using UnityEngine;

            public class CompliedMesh : MonoBehaviour
            {
                public bool hastime = false;
                float timer = 0f;

                //original marching cube https://github.com/ttammear/unitymcubes
                MarchingCubes marchingCube = new MarchingCubes();

                Vector3 MarchingBoundingBoxSize = new Vector3(100, 100, 100);
                Vector3 MarchingBoundingBoxCenter = new Vector3(0, 0, 0);
                Vector3 BoundingBoxResolution = new Vector3(100, 100, 100);

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

                    " + inputcode + @"
                    //Example of user input code is
                    //return ((4.9f*t) * (4.9f*t) - x * x - y * y - z * z);
                }

                public static void Setup(GameObject go,
                    bool hastime,float Timecycle,float TimeMinRange,float TimeMaxRange,
                    Vector3 MarchingBoundingBoxSize,
                    Vector3 MarchingBoundingBoxCenter,
                    Vector3 BoundingBoxResolution,
                    int vertsPerGO)
                {
                    CompliedMesh a = go.AddComponent<CompliedMesh>();
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

                    Main.Instance.viewcamera.SetExamineTarget(gameObject);
                    Main.Instance.coordinateAxes.ScaleToObject(gameObject);
                }

                void Update()
                {
                    if(hastime)
                    {
                        timer += Time.deltaTime/Mathf.Abs(Timecycle);
                        if (timer > 1)  timer = 0;

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
            }
        ");

        //get the assembly at runtime
        var runtimeType = assembly.GetType("CompliedMesh");
        //get the method ("Setup") and create a delegate so that we can execute it to tell the script to start
        var method = runtimeType.GetMethod("Setup");

        var del = (Func<GameObject, bool, float, float, float, Vector3, Vector3, Vector3, int>)
                      Delegate.CreateDelegate(
                          typeof(Func<GameObject, bool, float, float, float, Vector3, Vector3, Vector3, int>),
                          method);

        del.Invoke(gameObject, isusingT, Timecycle, TimeMinRange, TimeMaxRange, MarchingBoundingBoxSize, MarchingBoundingBoxCenter, BoundingBoxResolution, 64998); //must be divisible by 3, ie 3 verts == 1 triangle
    }
    delegate void Func<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8, T9 p9);

    void CompileGPUImplicitCodeAndAttachToGO(string inputcode)
    {
        inputcode = DoStringFormat(inputcode);

        var assembly = Compile(@"
            using System;
            using System.Collections.Generic;
            using UnityEngine;

            public class CompliedMesh : MonoBehaviour
            {
                Material mat;
                ComputeShader MarchingCubesCS;
                Vector3 MarchingBoundingBoxSize = new Vector3(100, 100, 100);
                Vector3 MarchingBoundingBoxCenter = new Vector3(0, 0, 0);
                Vector3 BoundingBoxResolution = new Vector3(100, 100, 100);

                DensityFieldGenerator dfg;
                GPUMarchingCubes marchingCubes;

                void AttachDensityField()
                {
                    dfg = gameObject.AddComponent<DensityFieldGenerator>();
                    dfg.BBoxResolution = (int)BoundingBoxResolution.x;
                    dfg.BBoxSize = (int)MarchingBoundingBoxSize.x;
                    dfg.samplingfunction = samplingfunction;
                }
                void AttachGPUMarchingCube()
                {
                    marchingCubes = gameObject.AddComponent<GPUMarchingCubes>();
                    marchingCubes.BBoxResolution = (int)BoundingBoxResolution.x;
                    marchingCubes.BBoxSize = (int)MarchingBoundingBoxSize.x;
                    marchingCubes.mat = mat;
                    marchingCubes.MarchingCubesCS = MarchingCubesCS;
                }
                static double samplingfunction(double x,double y,double z,double t)
                {
                    " + inputcode + @"
                }

                public static void Setup(GameObject go,
                    Material mat,
                    ComputeShader MarchingCubesCS,
                    Vector3 MarchingBoundingBoxSize,
                    Vector3 MarchingBoundingBoxCenter,
                    Vector3 BoundingBoxResolution)
                {
                    CompliedMesh a = go.AddComponent<CompliedMesh>();
                    a.mat = mat;
                    a.MarchingCubesCS = MarchingCubesCS;
                    a.MarchingBoundingBoxSize = MarchingBoundingBoxSize;
                    a.MarchingBoundingBoxCenter = MarchingBoundingBoxCenter;
                    a.BoundingBoxResolution = BoundingBoxResolution;
                }
                void Start()
                {
                    AttachDensityField();

                    AttachGPUMarchingCube();

                    Main.Instance.viewcamera.SetExamineTarget(gameObject);
                    Main.Instance.coordinateAxes.ScaleToObject(gameObject);
                }
                void OnDestroy()
                {
                    Destroy(dfg);
                    Destroy(marchingCubes);
                }
            }
        ");

        //get the assembly at runtime
        var runtimeType = assembly.GetType("CompliedMesh");
        //get the method ("Setup") and create a delegate so that we can execute it to tell the script to start
        var method = runtimeType.GetMethod("Setup");

        var del = (Func<GameObject, Material, ComputeShader, Vector3, Vector3, Vector3>)
                      Delegate.CreateDelegate(
                          typeof(Func<GameObject, Material, ComputeShader, Vector3, Vector3, Vector3>),
                          method);

        del.Invoke(gameObject, mat, computeshader, MarchingBoundingBoxSize, MarchingBoundingBoxCenter, BoundingBoxResolution); //must be divisible by 3, ie 3 verts == 1 triangle
    }
    delegate void Func<T1, T2, T3, T4, T5, T6>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6);

    //This function just takes the source string and compiles it and returns an Assembly 
    //From the Assembly, you can 
    //Get your compiled class within the assembly at by doing:
    //var runtimeType = assembly.GetType("ClassName");
    //After that you can get the Method using
    //var method = runtimeType.GetMethod("MethodName");
    static Assembly Compile(string source)
    {
        //get a list of assemblyRefrences
        string[] assemblyReferences = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !(a is System.Reflection.Emit.AssemblyBuilder) && !string.IsNullOrEmpty(a.Location))
            .Select(a => a.Location)
            .ToArray();

        CompilerParameters param = new CompilerParameters();
        param.GenerateExecutable = false;
        param.GenerateInMemory = true;
        param.ReferencedAssemblies.AddRange(assemblyReferences);

        // Compile the sourcefile
        //var result = new CSharpCodeProvider().CompileAssemblyFromSource(param, source); // uncomment this to use the .net version 
        var result = new CodeCompiler().CompileAssemblyFromSource(param, source); // using aeroson version

        StringBuilder errormessages = new StringBuilder();
        foreach (CompilerError error in result.Errors)
        {
            //CS0219 = decalred varaible but is unused. 
            //suppress this warning
            if (error.ErrorNumber == "CS0219")
            {
                continue;
            }
            errormessages.AppendFormat("Error ({0}): {1}\n", error.ErrorNumber, error.ErrorText);
        }
        if (errormessages.Length != 0)
        {
            //throw execption if theres any error
            throw new Exception(errormessages.ToString());
        }

        // Return the compiled code (assembly)
        return result.CompiledAssembly;
    }
}