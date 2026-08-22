using UnityEngine;

namespace Game.Ships
{
    public struct WakeSample
    {
        public WakeSample(Vector2 position, Vector2 tangent, float heading, float intensity, float hullHalfWidth)
        {
            Position = position;
            Tangent = tangent;
            Heading = heading;
            Intensity = intensity;
            HullHalfWidth = hullHalfWidth;
            Age = 0f;
        }

        public Vector2 Position;
        public Vector2 Tangent;
        public float Heading;
        public float Intensity;
        public float HullHalfWidth;
        public float Age;
    }
}
