using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView
    {
        private static Mesh CreateMesh(string name, MeshFilter meshFilter)
        {
            var mesh = new Mesh
            {
                name = name
            };
            mesh.MarkDynamic();
            meshFilter.sharedMesh = mesh;

            return mesh;
        }

        private static void SetStripTriangles(int[] triangles, int triangleIndex, int currentVertexIndex,
            int nextVertexIndex)
        {
            triangles[triangleIndex] = currentVertexIndex;
            triangles[triangleIndex + 1] = nextVertexIndex + 1;
            triangles[triangleIndex + 2] = currentVertexIndex + 1;
            triangles[triangleIndex + 3] = currentVertexIndex;
            triangles[triangleIndex + 4] = nextVertexIndex;
            triangles[triangleIndex + 5] = nextVertexIndex + 1;
        }

        private static void ApplyMesh(Mesh mesh, Vector3[] vertices, Vector2[] uv, Color[] colors, int[] triangles)
        {
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }
    }
}
