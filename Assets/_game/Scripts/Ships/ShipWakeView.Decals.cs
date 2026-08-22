using System.Collections.Generic;
using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView
    {
        private void BuildCenterDecals(Vector2[] positions, float hullHalfWidth, float normalizedSpeed)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var variants = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var accumulatedLength = 0f;
            var nextDecalDistance = 0f;
            var decalIndex = 0;
            for (var i = 0; i < positions.Length; i++)
            {
                if (i > 0)
                {
                    accumulatedLength += Vector2.Distance(positions[i - 1], positions[i]);
                }

                if (accumulatedLength < nextDecalDistance)
                {
                    continue;
                }

                GetDecalFrame(positions, i, out var tangent, out var normal);
                var tailFactor = (float)i / (positions.Length - 1);
                var intensity = GetIntensity(i, positions.Length, normalizedSpeed);
                var alpha = GetAlpha(i, positions.Length, tailFactor, intensity);
                alpha *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.28f, accumulatedLength));
                var halfLength = Mathf.Lerp(0.78f, 1.05f, intensity);
                var halfWidth = Mathf.Lerp(hullHalfWidth * 0.28f, _baseWidth * 0.56f,
                    Mathf.Clamp01(accumulatedLength / 0.75f));
                AddDecal(vertices, uv, variants, colors, triangles, positions[i], tangent, normal, halfLength,
                    halfWidth, alpha, decalIndex % 2, false);
                nextDecalDistance = accumulatedLength + halfLength * 0.24f;
                decalIndex++;
            }

            ApplyDecalMesh(_centerMesh, vertices, uv, variants, colors, triangles);
        }

        private void BuildSideDecals(Vector2[] positions, float hullHalfWidth, float normalizedSpeed)
        {
            var pathPointCount = positions.Length;
            var leftPositions = new Vector2[pathPointCount];
            var rightPositions = new Vector2[pathPointCount];
            var accumulatedLengths = new float[pathPointCount];
            var accumulatedLength = 0f;
            for (var i = 0; i < pathPointCount; i++)
            {
                if (i > 0)
                {
                    accumulatedLength += Vector2.Distance(positions[i - 1], positions[i]);
                }

                GetDecalFrame(positions, i, out var tangent, out var normal);
                var tailFactor = (float)i / (pathPointCount - 1);
                var spread = Mathf.Lerp(hullHalfWidth * 0.82f, _baseWidth * 2.7f,
                    Mathf.Pow(tailFactor, 0.7f));
                leftPositions[i] = positions[i] - normal * spread;
                rightPositions[i] = positions[i] + normal * spread;
                accumulatedLengths[i] = accumulatedLength;
            }

            BuildSideRibbonMesh(leftPositions, rightPositions, accumulatedLengths, normalizedSpeed);
        }

        private void BuildSideRibbonMesh(Vector2[] leftPositions, Vector2[] rightPositions,
            float[] accumulatedLengths, float normalizedSpeed)
        {
            var pathPointCount = leftPositions.Length;
            var vertices = new Vector3[pathPointCount * 4];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[(pathPointCount - 1) * 12];
            for (var i = 0; i < pathPointCount; i++)
            {
                var tailFactor = (float)i / (pathPointCount - 1);
                var intensity = GetIntensity(i, pathPointCount, normalizedSpeed);
                var alpha = GetAlpha(i, pathPointCount, tailFactor, intensity) * 0.82f;
                alpha *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.5f, accumulatedLengths[i]));
                var halfWidth = Mathf.Lerp(0.16f, 0.27f, intensity);
                GetRibbonJoin(leftPositions, i, out var leftNormal, out var leftScale);
                GetRibbonJoin(rightPositions, i, out var rightNormal, out var rightScale);
                SetStrip(vertices, uv, colors, i * 2, leftPositions[i], leftNormal, halfWidth * leftScale,
                    accumulatedLengths[i], alpha);
                var rightVertexIndex = pathPointCount * 2 + i * 2;
                SetStrip(vertices, uv, colors, rightVertexIndex, rightPositions[i], rightNormal,
                    halfWidth * rightScale, accumulatedLengths[i], alpha);
                if (i >= pathPointCount - 1)
                {
                    continue;
                }

                SetStripTriangles(triangles, i * 6, i * 2, (i + 1) * 2);
                SetStripTriangles(triangles, (pathPointCount - 1) * 6 + i * 6, rightVertexIndex,
                    rightVertexIndex + 2);
            }

            ApplyMesh(_sideMesh, vertices, uv, colors, triangles);
        }

        private static void GetDecalFrame(Vector2[] positions, int index, out Vector2 tangent, out Vector2 normal)
        {
            var previous = positions[Mathf.Max(0, index - 1)];
            var next = positions[Mathf.Min(positions.Length - 1, index + 1)];
            tangent = (next - previous).normalized;
            normal = new Vector2(-tangent.y, tangent.x);
        }

        private static void AddDecal(List<Vector3> vertices, List<Vector2> uv, List<Vector2> variants,
            List<Color> colors, List<int> triangles, Vector2 center, Vector2 tangent, Vector2 normal,
            float halfLength, float halfWidth, float alpha, int variant, bool mirror)
        {
            var tail = center + tangent * halfLength;
            var head = center - tangent * halfLength;
            var vertexIndex = vertices.Count;
            vertices.Add(tail - normal * halfWidth);
            vertices.Add(tail + normal * halfWidth);
            vertices.Add(head - normal * halfWidth);
            vertices.Add(head + normal * halfWidth);
            uv.Add(new Vector2(0f, mirror ? 1f : 0f));
            uv.Add(new Vector2(0f, mirror ? 0f : 1f));
            uv.Add(new Vector2(1f, mirror ? 1f : 0f));
            uv.Add(new Vector2(1f, mirror ? 0f : 1f));
            var variantUv = new Vector2(variant, 0f);
            var color = new Color(1f, 1f, 1f, alpha);
            for (var i = 0; i < 4; i++)
            {
                variants.Add(variantUv);
                colors.Add(color);
            }

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 3);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
        }

        private static void ApplyDecalMesh(Mesh mesh, List<Vector3> vertices, List<Vector2> uv,
            List<Vector2> variants, List<Color> colors, List<int> triangles)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, variants);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
