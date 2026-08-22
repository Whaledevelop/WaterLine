using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView
    {
        private static readonly int BowWaveMaskId = Shader.PropertyToID("_BowWaveMask");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private void BuildBowMesh(ShipVisualPose pose, float normalizedSpeed)
        {
            if (normalizedSpeed < _minimumSpeed)
            {
                _bowMesh.Clear();

                return;
            }

            var localRotation = Quaternion.Euler(0f, 0f, pose.Heading + pose.BowWaveAngleOffset);
            var maskCorrection = Quaternion.Euler(0f, 0f, pose.BowWaveAngleOffset);
            var center = pose.Center + (Vector2)(localRotation * pose.BowWaveOffset);
            var halfSize = pose.BowWaveSize * 0.5f;
            var lowerLeft = maskCorrection * new Vector3(-halfSize.x, -halfSize.y);
            var upperLeft = maskCorrection * new Vector3(-halfSize.x, halfSize.y);
            var lowerRight = maskCorrection * new Vector3(halfSize.x, -halfSize.y);
            var upperRight = maskCorrection * new Vector3(halfSize.x, halfSize.y);
            var vertices = new[]
            {
                (Vector3)center + lowerLeft,
                (Vector3)center + upperLeft,
                (Vector3)center + lowerRight,
                (Vector3)center + upperRight
            };
            var uv = new[] { Vector2.zero, Vector2.up, Vector2.right, Vector2.one };
            var colors = new[] { Color.white, Color.white, Color.white, Color.white };
            var triangles = new[] { 0, 1, 3, 0, 3, 2 };
            ApplyMesh(_bowMesh, vertices, uv, colors, triangles);
            _bowRenderer.GetPropertyBlock(_bowProperties);
            _bowProperties.SetTexture(BowWaveMaskId, pose.BowWaveMask);
            _bowProperties.SetFloat(IntensityId, normalizedSpeed);
            _bowRenderer.SetPropertyBlock(_bowProperties);
        }
    }
}
