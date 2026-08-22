using UnityEngine;

namespace Game.Ships
{
    public readonly struct ShipWaterInteractionPose
    {
        public ShipWaterInteractionPose(Vector2 center, Vector2 bow, Vector2 stern, Vector2 port,
            Vector2 starboard, float foamWidth)
        {
            Center = center;
            Bow = bow;
            Stern = stern;
            Port = port;
            Starboard = starboard;
            FoamWidth = foamWidth;
        }

        public Vector2 Center { get; }

        public Vector2 Bow { get; }

        public Vector2 Stern { get; }

        public Vector2 Port { get; }

        public Vector2 Starboard { get; }

        public float FoamWidth { get; }
    }
}
