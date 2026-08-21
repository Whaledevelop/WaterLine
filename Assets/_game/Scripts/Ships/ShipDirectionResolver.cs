using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipDirectionResolver
    {
        private const float SectorAngle = 45f;
        private readonly float _hysteresis;
        private int _currentIndex;

        public ShipDirectionResolver(float hysteresis = 4f)
        {
            _hysteresis = hysteresis;
        }

        public int CurrentIndex => _currentIndex;

        public void Initialize(float heading)
        {
            _currentIndex = GetNearestIndex(heading);
        }

        public bool Resolve(float heading)
        {
            var center = _currentIndex * SectorAngle;
            var delta = Mathf.DeltaAngle(center, heading);
            if (Mathf.Abs(delta) <= SectorAngle * 0.5f + _hysteresis)
            {
                return false;
            }

            _currentIndex = GetNearestIndex(heading);

            return true;
        }

        public void GetInterpolation(float heading, out int fromIndex, out int toIndex, out float factor)
        {
            var sector = Mathf.Repeat(heading, 360f) / SectorAngle;
            fromIndex = Mathf.FloorToInt(sector) % 8;
            toIndex = (fromIndex + 1) % 8;
            factor = sector - Mathf.Floor(sector);
        }

        private static int GetNearestIndex(float heading)
        {
            return Mathf.RoundToInt(Mathf.Repeat(heading, 360f) / SectorAngle) % 8;
        }
    }
}
