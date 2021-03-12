//Requires Clipboard Helper.cs

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The MeshToCodeWindow allows converting a selected prefab with a mesh into C# code.
/// </summary>
public class MeshToCode : EditorWindow
{
    Vector2 scrollPosition; // Position of scroll view.

    GameObject prefab; // A reference to the selected prefab.

    string code; // String that will hold the generated C# code.

    bool optimize; // Holds the value of the Optimize check box.

    bool roundVertexes; // Holds the checked state of the Round Vertexes check box.

    int vertexDecimals; // Holds the number of decimal places to round off the vertex values.

    bool roundNorms; // Holds the checked state of the round normals check box.

    int normalDecimals; // Holds the number of decimal places to round off the normal values.

    // Provides a menu in the Unity menu system for showing this window.
    [MenuItem("Utilities/Mesh/Mesh To Code")]
    public static void ShowWindow()
    {
        //create a unity window, show it, and give focus
        MeshToCode window = GetWindow<MeshToCode>(false, "Mesh To Code");
        window.Show();
        window.Focus();
        window.Repaint();
    }

    /// <summary>
    /// Draws the window controls.
    /// </summary>
    void OnGUI()
    {
        //creates the area for user to place in the prefab object
        prefab = EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), true) as GameObject;

        GUILayout.Space(4);

        // creates the check box to determine whether to use optimization.
        optimize = GUILayout.Toggle(optimize, "Optimize");

        GUILayout.Space(4);
        
        GUILayout.BeginHorizontal();
        //creates a toggle box to round vertices 
        roundVertexes = GUILayout.Toggle(roundVertexes, "Round vertices");
        if (roundVertexes)
        {
            //if checkbox is ticked,create a int field for users to keyin the decimal place they want 
            vertexDecimals = EditorGUILayout.IntField("Decimal places", vertexDecimals);
            vertexDecimals = vertexDecimals < 0 ? 0 : vertexDecimals;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        //creates a toggle box to round normals 
        roundNorms = GUILayout.Toggle(roundNorms, "Round normals");
        if (roundNorms)
        {
            //if checkbox is ticked,create a int field for users to keyin the decimal place they want 
            normalDecimals = EditorGUILayout.IntField("Decimal places", normalDecimals);
            normalDecimals = normalDecimals < 0 ? 0 : normalDecimals;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        //Create the button to start generating the code from mesh
        if (GUILayout.Button("Generate"))
        {
            GenerateCode();
        }

        GUILayout.Space(4);

        // add a copy to clipboard button
        if (GUILayout.Button("Copy to Sytstem clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = code;
        }
        GUILayout.EndHorizontal();

        // write out the  generated code
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        code = GUILayout.TextArea(string.IsNullOrEmpty(code) ? string.Empty : code, int.MaxValue);
        EditorGUILayout.EndScrollView();
    }

    // The function to generates C# code from the specified prefab.
    void GenerateCode()
    {
        if (prefab == null)
        {
            code = "No prefab"; // if no prefab just return
            return;
        }

        // check if a mesh filter is present
        MeshFilter filter = prefab.GetComponent<MeshFilter>();
        if (filter == null)
        {
            code = "No mesh filter";
            return;
        }

        Mesh tempMesh = new Mesh();
        tempMesh.vertices = filter.sharedMesh.vertices;
        tempMesh.normals = filter.sharedMesh.normals;
        tempMesh.uv = filter.sharedMesh.uv;
        tempMesh.triangles = filter.sharedMesh.triangles;

        // check if user wants to optimize first
        if (optimize)
        {
            MeshUtility.Optimize(tempMesh);
        }

        code = "Mesh Generate()\r\n";
        code += "{\r\n";
        code += "    var mesh = new Mesh();\r\n";
        code += "\r\n";

        // vertices
        code += "    var vertices = new Vector3[]\r\n";
        code += "    {\r\n";

        code += string.Join(string.Empty,
            tempMesh.vertices.Select(vector => GetVector3String(vector, roundVertexes, vertexDecimals)).ToArray());

        code += "    };\r\n";
        code += "\r\n";

        // normals
        code += "    var normals = new Vector3[]\r\n";
        code += "    {\r\n";

        code += string.Join(string.Empty,
            tempMesh.normals.Select(vector => GetVector3String(vector, roundNorms, normalDecimals)).ToArray());

        code += "    };\r\n";
        code += "\r\n";

        // uv cords
        code += "    var uv = new Vector2[]\r\n";
        code += "    {\r\n";

        code += string.Join(string.Empty,
            tempMesh.uv.Select(vector => string.Format("        new Vector2({0}f, {1}f),\r\n", vector.x, vector.y))
                .ToArray());

        code += "    };\r\n";
        code += "\r\n";

        // triangles        
        code += "    var triangles = new int[]\r\n";
        code += "    {\r\n";

        int[] triangles = tempMesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            code += string.Format("        {0}, {1}, {2},\r\n", triangles[i], triangles[i + 1], triangles[i + 2]);
        }

        code += "    };\r\n";
        code += "\r\n";

        code += "    mesh.vertices = vertices;\r\n";
        code += "    mesh.normals = normals;\r\n";
        code += "    mesh.uv = uv;\r\n";
        code += "    mesh.triangles = triangles;\r\n";
        code += "    return mesh;\r\n";
        code += "}\r\n";
    }

    /// <summary>
    /// Gets the rounded values of a <see cref="Vector3"/> type as a C# encoded string.
    /// </summary>
    /// <param name="vector">The source vector to be converted.</param>
    /// <param name="round">If true the values from <see cref="vector"/> will be rounded.</param>
    /// <param name="decimals">The number of decimal places to round.</param>
    /// <returns>Returns the rounded values of the <see cref="vector"/> parameter as a C# encoded string.</returns>
    string GetVector3String(Vector3 vector, bool round, int decimals)
    {
        if (round)
        {
            return string.Format(
                "        new Vector3({0}f, {1}f, {2}f),\r\n",
                Math.Round(vector.x, decimals),
                Math.Round(vector.y, decimals),
                Math.Round(vector.z, decimals));
        }

        return string.Format("        new Vector3({0}f, {1}f, {2}f),\r\n", vector.x, vector.y, vector.z);
    }
}