using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView
    {
        private const int BowWavePointCount = 12;

        private void BuildBowMesh(Vector2 bowPosition, Vector2 sternPosition, float normalizedSpeed)
        {
            if (normalizedSpeed < _minimumSpeed)
            {
                _bowMesh.Clear();

                return;
            }

            var forward = (bowPosition - sternPosition).normalized;
            var normal = new Vector2(-forward.y, forward.x);
            var vertices = new Vector3[BowWavePointCount * 4];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[(BowWavePointCount - 1) * 12];
            var waveLength = Mathf.Lerp(0.65f, 1.55f, normalizedSpeed);
            var waveSpread = Mathf.Lerp(0.32f, 0.82f, normalizedSpeed);
            var waveWidth = Mathf.Lerp(0.045f, 0.12f, normalizedSpeed);
            for (var i = 0; i < BowWavePointCount; i++)
            {
                var factor = (float)i / (BowWavePointCount - 1);
                var curvedFactor = Mathf.Pow(factor, 0.72f);
                var center = bowPosition - forward * waveLength * factor;
                var spread = normal * waveSpread * curvedFactor;
                var localNormal = (normal + forward * Mathf.Lerp(0.45f, 0f, factor)).normalized;
                var width = waveWidth * Mathf.Sin(factor * Mathf.PI) + 0.012f;
                var alpha = Mathf.Sin(factor * Mathf.PI) * Mathf.Lerp(0.55f, 1f, normalizedSpeed);
                var vertexIndex = i * 4;
                SetBowStrip(vertices, uv, colors, vertexIndex, center - spread, localNormal, width, factor, alpha);
                SetBowStrip(vertices, uv, colors, vertexIndex + 2, center + spread, localNormal, width, factor, alpha);

                if (i >= BowWavePointCount - 1)
                {
                    continue;
                }

                SetStripTriangles(triangles, i * 12, vertexIndex, vertexIndex + 4);
                SetStripTriangles(triangles, i * 12 + 6, vertexIndex + 2, vertexIndex + 6);
            }

            ApplyMesh(_bowMesh, vertices, uv, colors, triangles);
        }

        private static void SetBowStrip(Vector3[] vertices, Vector2[] uv, Color[] colors, int vertexIndex,
            Vector2 center, Vector2 normal, float halfWidth, float factor, float alpha)
        {
            vertices[vertexIndex] = center - normal * halfWidth;
            vertices[vertexIndex + 1] = center + normal * halfWidth;
            uv[vertexIndex] = new Vector2(factor * 1.8f, 0f);
            uv[vertexIndex + 1] = new Vector2(factor * 1.8f, 1f);
            colors[vertexIndex] = new Color(1f, 1f, 1f, alpha);
            colors[vertexIndex + 1] = new Color(1f, 1f, 1f, alpha);
        }
    }
}
