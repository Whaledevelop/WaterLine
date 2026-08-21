using UnityEngine;

namespace Game.Ships
{
    public readonly struct ShipVisualAnchors
    {
        public ShipVisualAnchors(Vector2 bow, Vector2 stern, Vector2 port, Vector2 starboard, float foamWidth)
        {
            Bow = bow;
            Stern = stern;
            Port = port;
            Starboard = starboard;
            FoamWidth = foamWidth;
        }

        public Vector2 Bow { get; }

        public Vector2 Stern { get; }

        public Vector2 Port { get; }

        public Vector2 Starboard { get; }

        public float FoamWidth { get; }
    }
}
