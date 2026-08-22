using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView
    {
        private float GetIntensity(int index, float normalizedSpeed)
        {
            return index == 0 ? normalizedSpeed : _points[index - 1].Intensity;
        }

        private float GetAlpha(int index, float tailFactor, float intensity)
        {
            var age = index == 0 ? 0f : _points[index - 1].Age;
            var lifeFactor = 1f - Mathf.Clamp01(age / _lifetime);

            return lifeFactor * Mathf.Lerp(0.08f, 1f, 1f - tailFactor) * Mathf.Lerp(0.45f, 1f, intensity);
        }

        private static void GetFrame(Vector2[] positions, int index, out Vector2 normal)
        {
            var previous = index == 0 ? positions[index] : positions[index - 1];
            var next = index == positions.Length - 1 ? positions[index] : positions[index + 1];
            var tangent = (next - previous).normalized;
            normal = new Vector2(-tangent.y, tangent.x);
        }

        private static void SetStrip(Vector3[] vertices, Vector2[] uv, Color[] colors, int vertexIndex,
            Vector2 center, Vector2 normal, float halfWidth, float accumulatedLength, float alpha)
        {
            vertices[vertexIndex] = center - normal * halfWidth;
            vertices[vertexIndex + 1] = center + normal * halfWidth;
            uv[vertexIndex] = new Vector2(accumulatedLength * 0.3f, 0f);
            uv[vertexIndex + 1] = new Vector2(accumulatedLength * 0.3f, 1f);
            colors[vertexIndex] = new Color(1f, 1f, 1f, alpha);
            colors[vertexIndex + 1] = new Color(1f, 1f, 1f, alpha);
        }

        private static void SetQuadTriangles(int[] triangles, int pointIndex, int vertexIndex)
        {
            if (pointIndex * 6 >= triangles.Length)
            {
                return;
            }

            var triangleIndex = pointIndex * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 3;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
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

        private static void SetDecalTriangles(int[] triangles, int triangleIndex, int vertexIndex)
        {
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 3;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
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
