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

        private void BuildMeshes(Vector2 sternPosition, float normalizedSpeed)
        {
            var pointCount = _points.Count + 1;
            if (pointCount < 2)
            {
                _centerMesh.Clear();
                _sideMesh.Clear();
                _residualMesh.Clear();

                return;
            }

            var positions = BuildSmoothedPositions(sternPosition);
            BuildCenterMesh(positions, normalizedSpeed);
            BuildSideMesh(positions, normalizedSpeed);
            BuildResidualMesh(positions);
        }

        private Vector2[] BuildSmoothedPositions(Vector2 sternPosition)
        {
            var positions = new Vector2[_points.Count + 1];
            positions[0] = sternPosition;
            for (var i = 1; i < positions.Length; i++)
            {
                positions[i] = _points[i - 1].Position;
            }

            for (var pass = 0; pass < 2; pass++)
            {
                var previousPositions = (Vector2[])positions.Clone();
                for (var i = 1; i < positions.Length - 1; i++)
                {
                    positions[i] = previousPositions[i - 1] * 0.2f + previousPositions[i] * 0.6f +
                        previousPositions[i + 1] * 0.2f;
                }
            }

            return positions;
        }

        private void BuildCenterMesh(Vector2[] positions, float normalizedSpeed)
        {
            var vertices = new Vector3[positions.Length * 2];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[(positions.Length - 1) * 6];
            var accumulatedLength = 0f;
            for (var i = 0; i < positions.Length; i++)
            {
                if (i > 0)
                {
                    accumulatedLength += Vector2.Distance(positions[i - 1], positions[i]);
                }

                GetFrame(positions, i, out var normal);
                var tailFactor = (float)i / (positions.Length - 1);
                var intensity = GetIntensity(i, normalizedSpeed);
                var headFactor = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(accumulatedLength / _headBlendDistance));
                var width = Mathf.Lerp(_baseWidth * Mathf.Lerp(0.9f, 2.2f, intensity), _baseWidth * 0.08f,
                    Mathf.Pow(tailFactor, 0.68f)) * Mathf.Lerp(0.06f, 1f, headFactor);
                var alpha = GetAlpha(i, tailFactor, intensity);
                var vertexIndex = i * 2;
                vertices[vertexIndex] = positions[i] - normal * width;
                vertices[vertexIndex + 1] = positions[i] + normal * width;
                uv[vertexIndex] = new Vector2(accumulatedLength * 0.42f, 0f);
                uv[vertexIndex + 1] = new Vector2(accumulatedLength * 0.42f, 1f);
                colors[vertexIndex] = new Color(1f, 1f, 1f, alpha);
                colors[vertexIndex + 1] = new Color(1f, 1f, 1f, alpha);
                SetQuadTriangles(triangles, i, vertexIndex);
            }

            ApplyMesh(_centerMesh, vertices, uv, colors, triangles);
        }

        private void BuildSideMesh(Vector2[] positions, float normalizedSpeed)
        {
            var vertices = new Vector3[positions.Length * 4];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[(positions.Length - 1) * 12];
            var accumulatedLength = 0f;
            for (var i = 0; i < positions.Length; i++)
            {
                if (i > 0)
                {
                    accumulatedLength += Vector2.Distance(positions[i - 1], positions[i]);
                }

                GetFrame(positions, i, out var normal);
                var tailFactor = (float)i / (positions.Length - 1);
                var intensity = GetIntensity(i, normalizedSpeed);
                var headFactor = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(accumulatedLength / _headBlendDistance));
                var offset = _baseWidth * Mathf.Lerp(0.12f,
                    Mathf.Lerp(1.5f, 4.2f, Mathf.Pow(tailFactor, 0.72f)), headFactor);
                var stripWidth = _baseWidth * Mathf.Lerp(0.03f,
                    Mathf.Lerp(0.25f, 0.08f, tailFactor), headFactor);
                var alpha = GetAlpha(i, tailFactor, intensity) * Mathf.Lerp(0.18f,
                    Mathf.Lerp(0.48f, 0.14f, tailFactor), headFactor);
                var vertexIndex = i * 4;
                SetStrip(vertices, uv, colors, vertexIndex, positions[i] - normal * offset, normal,
                    stripWidth, accumulatedLength, alpha);
                SetStrip(vertices, uv, colors, vertexIndex + 2, positions[i] + normal * offset, normal,
                    stripWidth, accumulatedLength, alpha);

                if (i >= positions.Length - 1)
                {
                    continue;
                }

                SetStripTriangles(triangles, i * 12, vertexIndex, vertexIndex + 4);
                SetStripTriangles(triangles, i * 12 + 6, vertexIndex + 2, vertexIndex + 6);
            }

            ApplyMesh(_sideMesh, vertices, uv, colors, triangles);
        }

        private void BuildResidualMesh(Vector2[] positions)
        {
            var decalCount = Mathf.Min((positions.Length - 1) / 7, 14);
            var vertices = new Vector3[decalCount * 4];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[decalCount * 6];
            for (var decalIndex = 0; decalIndex < decalCount; decalIndex++)
            {
                var pointIndex = Mathf.Min(4 + decalIndex * 7, positions.Length - 2);
                GetFrame(positions, pointIndex, out var normal);
                var tangent = new Vector2(normal.y, -normal.x);
                var random = Mathf.Repeat(decalIndex * 0.618f, 1f);
                var center = positions[pointIndex] + normal * Mathf.Lerp(-0.55f, 0.55f, random);
                var halfWidth = Mathf.Lerp(0.16f, 0.34f, random);
                var halfLength = Mathf.Lerp(0.35f, 0.7f, 1f - random);
                var vertexIndex = decalIndex * 4;
                vertices[vertexIndex] = center - normal * halfWidth - tangent * halfLength;
                vertices[vertexIndex + 1] = center + normal * halfWidth - tangent * halfLength;
                vertices[vertexIndex + 2] = center - normal * halfWidth + tangent * halfLength;
                vertices[vertexIndex + 3] = center + normal * halfWidth + tangent * halfLength;
                uv[vertexIndex] = Vector2.zero;
                uv[vertexIndex + 1] = Vector2.right;
                uv[vertexIndex + 2] = Vector2.up;
                uv[vertexIndex + 3] = Vector2.one;
                var alpha = GetAlpha(pointIndex, (float)pointIndex / positions.Length,
                    _points[pointIndex - 1].Intensity) * 0.38f;
                for (var colorIndex = 0; colorIndex < 4; colorIndex++)
                {
                    colors[vertexIndex + colorIndex] = new Color(1f, 1f, 1f, alpha);
                }

                SetDecalTriangles(triangles, decalIndex * 6, vertexIndex);
            }

            ApplyMesh(_residualMesh, vertices, uv, colors, triangles);
        }
    }
}
