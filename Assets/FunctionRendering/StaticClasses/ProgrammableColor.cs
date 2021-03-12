using UnityEngine;
using UnityEditor;
using System.IO;

public static class ProgrammableColor
{
    public static Material CreateMaterial(string red, string green, string blue)
    {
#if UNITY_EDITOR
        var resDir = new DirectoryInfo(Path.Combine(Application.dataPath, "Resources"));
        if (!resDir.Exists) resDir.Create();

        string shaderText =
            "Shader \"Programmable_Color\"\n"+
            "{\n" +
                "Properties\n" +
                "{\n" +
                    "_URange(\"URange\", Vector) = (0, 1, 1, 1)\n" +
                    "_VRange(\"VRange\", Vector) = (0, 1, 1, 1)\n" +
                    "_WRange(\"WRange\", Vector) = (0, 1, 1, 1)\n" +
                    "_TRange(\"TRange\", Vector) = (0, 1, 1, 1)\n" +
                    "_Tcycle(\"Timecycle\", float) = 1\n" +
                "}\n" +
                 "SubShader\n" +
                 "{\n" +
                     "Pass\n" +
                     "{\n" +
                        "Tags{\"LightMode\" = \"ForwardBase\" }\n" +
                        "Cull Off CGPROGRAM\n" +
                        "#pragma vertex vert\n" +
                        "#pragma fragment frag\n" +
                        "uniform float4 _URange;\n" +
                        "uniform float4 _VRange;\n" +
                        "uniform float4 _WRange;\n" +
                        "uniform float4 _TRange;\n" +
                        "uniform float _Tcycle;\n" +

                        "struct vertexInput { float4 vertex : POSITION; float3 normal : NORMAL; };\n" +
                        "struct vertexOutput { float4 pos : SV_POSITION; float4 posWorld : TEXCOORD0; float3 normalDir : TEXCOORD1; };\n" +
                        "vertexOutput vert(vertexInput input)\n" +
                        "{\n" +
                            "vertexOutput output;\n" +
                            "float4x4 modelMatrix = unity_ObjectToWorld;\n" +
                            "float4x4 modelMatrixInverse = unity_WorldToObject;\n" +
                            "output.posWorld = mul(modelMatrix, input.vertex);\n" +
                            "output.normalDir = normalize(mul(float4(input.normal, 0.0), modelMatrixInverse).xyz);\n" +
                            "output.pos = UnityObjectToClipPos(input.vertex);\n" +
                            "return output;\n" +
                        "}\n" +

                        "static const float pi = 3.14159265358;\n" +

                        "float4 frag(vertexOutput input) : COLOR \n" +
                        "{\n" +
                            "float u = (input.posWorld.x - _URange.x) / (_URange.y - _URange.x);\n" +
                            "float v = (input.posWorld.y - _URange.x) / (_URange.y - _URange.x);\n" +
                            "float w = (-input.posWorld.z - _URange.x) / (_URange.y - _URange.x);\n" +
                            "float t = cos((_Time.y * 6) / _Tcycle) * (_TRange.y - _TRange.x) + _TRange.x;\n" +

                            //User code goes here
                            "float r = " + red + ";\n" +
                            "float g = " + green + ";\n" +
                            "float b = " + blue + ";\n" +

                            "return float4(r, g, b, 1);\n" +
                        "}\n" +
                        "ENDCG\n" +
                    "}\n" +
                "}\n" +
            "}";

        //create a file and write it as a .shader file,
        //this is basically outputing a file,
        string path = Path.Combine(resDir.FullName, "Programmable_Color.shader");
        File.WriteAllText(path, shaderText);

        //Force unity to update the assets
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        //load the asset manually by code, (usually this is assign/loading via inspector)
        AssetDatabase.LoadAssetAtPath<Shader>("Resources/Programmable_Color.shader");
        Shader shader = Shader.Find("Programmable_Color");
        //return back the material
        return new Material(shader);
#else
        //do nothing if not in editor mode.
        return null;
#endif
    }
}
