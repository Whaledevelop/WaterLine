using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipWaterInteractionResolver
    {
        private const float SectorAngle = 45f;
        private readonly ShipVisualProfile _profile;

        public ShipWaterInteractionResolver(ShipVisualProfile profile)
        {
            _profile = profile;
        }

        public ShipWaterInteractionPose Resolve(Vector2 position, float heading)
        {
            var sector = Mathf.Repeat(heading, 360f) / SectorAngle;
            var fromIndex = Mathf.FloorToInt(sector) % 8;
            var toIndex = (fromIndex + 1) % 8;
            var factor = Mathf.SmoothStep(0f, 1f, sector - Mathf.Floor(sector));
            var from = _profile.GetVisual(fromIndex);
            var to = _profile.GetVisual(toIndex);

            return new ShipWaterInteractionPose(
                position,
                position + Vector2.Lerp(from.BowAnchor, to.BowAnchor, factor),
                position + Vector2.Lerp(from.SternAnchor, to.SternAnchor, factor),
                position + Vector2.Lerp(from.PortAnchor, to.PortAnchor, factor),
                position + Vector2.Lerp(from.StarboardAnchor, to.StarboardAnchor, factor),
                Mathf.Lerp(from.FoamWidth, to.FoamWidth, factor));
        }
    }
}
