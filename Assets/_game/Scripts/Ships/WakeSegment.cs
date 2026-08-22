using System.Collections.Generic;
namespace Game.Ships
{
    public sealed class WakeSegment
    {
        public WakeSegment(float referenceHeading)
        {
            ReferenceHeading = referenceHeading;
        }

        public List<WakeSample> Samples { get; } = new();
        public float ReferenceHeading { get; }
        public float Length { get; private set; }

        public void Add(WakeSample sample, float distance)
        {
            Samples.Insert(0, sample);
            Length += distance;
        }
    }
}
