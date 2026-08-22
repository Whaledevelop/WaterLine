using System.Collections.Generic;
using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView
    {
        private void BuildCenterDecals(List<WakePathPoint> path, float hullHalfWidth, float normalizedSpeed,
            float alphaMultiplier, Mesh mesh)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var variants = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var accumulatedLength = 0f;
            var nextDecalDistance = 0f;
            var decalIndex = 0;
            for (var i = 0; i < path.Count; i++)
            {
                if (i > 0)
                {
                    accumulatedLength += Vector2.Distance(path[i - 1].Position, path[i].Position);
                }

                if (accumulatedLength < nextDecalDistance)
                {
                    continue;
                }

                GetFrame(path, i, out var tangent, out var normal);
                var tailFactor = (float)i / (path.Count - 1);
                var intensity = i == 0 ? normalizedSpeed : path[i].Intensity;
                var alpha = GetAlpha(path[i], tailFactor, alphaMultiplier);
                alpha *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.28f, accumulatedLength));
                var halfLength = Mathf.Lerp(0.78f, 1.05f, intensity);
                var halfWidth = Mathf.Lerp(hullHalfWidth * 0.28f, _centerWakeWidth * 0.5f,
                    Mathf.Clamp01(accumulatedLength / 0.75f));
                AddDecal(vertices, uv, variants, colors, triangles, path[i].Position, tangent, normal, halfLength,
                    halfWidth, alpha, decalIndex % 2);
                nextDecalDistance = accumulatedLength + halfLength * 0.24f;
                decalIndex++;
            }

            ApplyDecalMesh(mesh, vertices, uv, variants, colors, triangles);
        }

        private void BuildSideRibbon(List<WakePathPoint> path, float hullHalfWidth, float normalizedSpeed,
            float alphaMultiplier, Mesh mesh)
        {
            var pointCount = path.Count;
            var leftPositions = new Vector2[pointCount];
            var rightPositions = new Vector2[pointCount];
            var accumulatedLengths = new float[pointCount];
            var accumulatedLength = 0f;
            for (var i = 0; i < pointCount; i++)
            {
                if (i > 0)
                {
                    accumulatedLength += Vector2.Distance(path[i - 1].Position, path[i].Position);
                }

                GetFrame(path, i, out _, out var normal);
                var tailFactor = (float)i / (pointCount - 1);
                var spread = Mathf.Lerp(hullHalfWidth * 0.82f, _baseWidth * 2.7f,
                    Mathf.Pow(tailFactor, 0.7f));
                leftPositions[i] = path[i].Position - normal * spread;
                rightPositions[i] = path[i].Position + normal * spread;
                accumulatedLengths[i] = accumulatedLength;
            }

            var vertices = new Vector3[pointCount * 4];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[(pointCount - 1) * 12];
            for (var i = 0; i < pointCount; i++)
            {
                var tailFactor = (float)i / (pointCount - 1);
                var intensity = i == 0 ? normalizedSpeed : path[i].Intensity;
                var alpha = GetAlpha(path[i], tailFactor, alphaMultiplier) * 0.82f;
                alpha *= Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(0.1f, _headBlendDistance, accumulatedLengths[i]));
                var halfWidth = Mathf.Lerp(0.16f, 0.27f, intensity);
                GetRibbonJoin(leftPositions, i, out var leftNormal, out var leftScale);
                GetRibbonJoin(rightPositions, i, out var rightNormal, out var rightScale);
                SetStrip(vertices, uv, colors, i * 2, leftPositions[i], leftNormal, halfWidth * leftScale,
                    accumulatedLengths[i], alpha);
                var rightVertexIndex = pointCount * 2 + i * 2;
                SetStrip(vertices, uv, colors, rightVertexIndex, rightPositions[i], rightNormal,
                    halfWidth * rightScale, accumulatedLengths[i], alpha);
                if (i >= pointCount - 1)
                {
                    continue;
                }

                SetStripTriangles(triangles, i * 6, i * 2, (i + 1) * 2);
                SetStripTriangles(triangles, (pointCount - 1) * 6 + i * 6, rightVertexIndex,
                    rightVertexIndex + 2);
            }

            ApplyMesh(mesh, vertices, uv, colors, triangles);
        }

        private void BuildResidualDecals(List<WakePathPoint> path, float alphaMultiplier, Mesh mesh)
        {
            var decalCount = Mathf.Min((path.Count - 1) / 21, 14);
            var vertices = new List<Vector3>(decalCount * 4);
            var uv = new List<Vector2>(decalCount * 4);
            var variants = new List<Vector2>(decalCount * 4);
            var colors = new List<Color>(decalCount * 4);
            var triangles = new List<int>(decalCount * 6);
            for (var decalIndex = 0; decalIndex < decalCount; decalIndex++)
            {
                var pointIndex = Mathf.Min(12 + decalIndex * 21, path.Count - 2);
                GetFrame(path, pointIndex, out var tangent, out var normal);
                var random = Mathf.Repeat(decalIndex * 0.618f, 1f);
                var center = path[pointIndex].Position + normal * Mathf.Lerp(-0.55f, 0.55f, random);
                var alpha = GetAlpha(path[pointIndex], (float)pointIndex / path.Count, alphaMultiplier) * 0.3f;
                AddDecal(vertices, uv, variants, colors, triangles, center, tangent, normal,
                    Mathf.Lerp(0.35f, 0.7f, 1f - random), Mathf.Lerp(0.16f, 0.34f, random), alpha, 0);
            }

            ApplyDecalMesh(mesh, vertices, uv, variants, colors, triangles);
        }

        private static void AddDecal(List<Vector3> vertices, List<Vector2> uv, List<Vector2> variants,
            List<Color> colors, List<int> triangles, Vector2 center, Vector2 tangent, Vector2 normal,
            float halfLength, float halfWidth, float alpha, int variant)
        {
            var tail = center + tangent * halfLength;
            var head = center - tangent * halfLength;
            var vertexIndex = vertices.Count;
            vertices.Add(tail - normal * halfWidth);
            vertices.Add(tail + normal * halfWidth);
            vertices.Add(head - normal * halfWidth);
            vertices.Add(head + normal * halfWidth);
            uv.Add(Vector2.zero);
            uv.Add(Vector2.up);
            uv.Add(Vector2.right);
            uv.Add(Vector2.one);
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
