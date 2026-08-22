using UnityEngine;

namespace Game.Ships
{
    public readonly struct ShipVisualPose
    {
        public ShipVisualPose(int directionIndex, float heading, Vector2 center,
            Vector2 bow, Vector2 stern, Vector2 port, Vector2 starboard, float foamWidth,
            Texture2D bowWaveMask, Vector2 bowWaveOffset, Vector2 bowWaveSize, float bowWaveAngleOffset)
        {
            DirectionIndex = directionIndex;
            Heading = heading;
            Center = center;
            Bow = bow;
            Stern = stern;
            Port = port;
            Starboard = starboard;
            FoamWidth = foamWidth;
            BowWaveMask = bowWaveMask;
            BowWaveOffset = bowWaveOffset;
            BowWaveSize = bowWaveSize;
            BowWaveAngleOffset = bowWaveAngleOffset;
        }

        public int DirectionIndex { get; }
        public float Heading { get; }
        public Vector2 Center { get; }
        public Vector2 Bow { get; }
        public Vector2 Stern { get; }
        public Vector2 Port { get; }
        public Vector2 Starboard { get; }
        public float FoamWidth { get; }
        public Texture2D BowWaveMask { get; }
        public Vector2 BowWaveOffset { get; }
        public Vector2 BowWaveSize { get; }
        public float BowWaveAngleOffset { get; }
        public float HullHalfWidth => Vector2.Distance(Port, Starboard) * 0.5f;
        public Vector2 Forward => new(Mathf.Cos(Heading * Mathf.Deg2Rad), Mathf.Sin(Heading * Mathf.Deg2Rad));
    }
}
