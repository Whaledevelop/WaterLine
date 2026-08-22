namespace Game.Ships
{
    public sealed class WakeDirectionHistory
    {
        public WakeSegment Current { get; private set; }

        public void Initialize(float heading)
        {
            Current = new WakeSegment(heading);
        }
    }
}
