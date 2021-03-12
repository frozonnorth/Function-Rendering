    using UnityEngine;

    [ExecuteInEditMode]
    public class MeshNormalsVisualizer : MonoBehaviour
    {
        public float scaleNormals = 1;

        public Color color = Color.white;

        private void OnRenderObject()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return;
            }

            Mesh mesh = meshFilter.sharedMesh != null ? meshFilter.sharedMesh : meshFilter.mesh;
            if (mesh == null)
            {
                return;
            }

            Vector3[] vertexes = mesh.vertices;
            Vector3[] normals = mesh.normals;

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one));
            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < vertexes.Length; i++)
            {
                Vector3 vertex = vertexes[i];
                Vector3 normal = vertex + (normals[i] * scaleNormals);
           
                GL.Vertex3(vertex.x, vertex.y, vertex.z);
                GL.Vertex3(normal.x, normal.y, normal.z);
                
            }

            GL.End();
            GL.PopMatrix();
        }
    }
