using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipVisualPoseResolver
    {
        private const float SectorAngle = 45f;
        private const float Hysteresis = 4f;
        private readonly ShipVisualProfile _profile;
        private int _directionIndex;
        private bool _isInitialized;

        public ShipVisualPoseResolver(ShipVisualProfile profile)
        {
            _profile = profile;
        }

        public ShipVisualPose Resolve(Vector2 position, float heading)
        {
            if (!_isInitialized)
            {
                _directionIndex = GetNearestIndex(heading);
                _isInitialized = true;
            }

            var centerHeading = _directionIndex * SectorAngle;
            var delta = Mathf.DeltaAngle(centerHeading, heading);
            if (Mathf.Abs(delta) > SectorAngle * 0.5f + Hysteresis)
            {
                _directionIndex = GetNearestIndex(heading);
            }

            var visual = _profile.GetVisual(_directionIndex);

            return new ShipVisualPose(_directionIndex, heading, position,
                position + visual.BowAnchor, position + visual.SternAnchor,
                position + visual.PortAnchor, position + visual.StarboardAnchor, visual.FoamWidth,
                visual.BowWaveMask, visual.BowWaveOffset, visual.BowWaveSize, visual.BowWaveAngleOffset);
        }

        private static int GetNearestIndex(float heading)
        {
            return Mathf.RoundToInt(Mathf.Repeat(heading, 360f) / SectorAngle) % 8;
        }
    }
}
