using Game.Ships;
using NUnit.Framework;
using UnityEngine;

namespace Game.Editor.Tests
{
    public sealed class WakeDirectionHistoryTests
    {
        [Test]
        public void InitializeCreatesCurrentSegment()
        {
            var history = new WakeDirectionHistory();

            history.Initialize(45f);

            Assert.IsNotNull(history.Current);
            Assert.AreEqual(45f, history.Current.ReferenceHeading);
            Assert.IsEmpty(history.Current.Samples);
        }

        [Test]
        public void CurrentSegmentKeepsSamplesUntilReinitialized()
        {
            var history = CreateHistory();
            var current = history.Current;
            AddLength(current, 1f);

            Assert.AreSame(current, history.Current);
            Assert.AreEqual(1, history.Current.Samples.Count);
        }

        [Test]
        public void InitializeDiscardsPreviousDirectionSamples()
        {
            var history = CreateHistory();
            var previous = history.Current;
            AddLength(previous, 1f);

            history.Initialize(90f);

            Assert.AreNotSame(previous, history.Current);
            Assert.AreEqual(90f, history.Current.ReferenceHeading);
            Assert.IsEmpty(history.Current.Samples);
        }

        private static WakeDirectionHistory CreateHistory()
        {
            var history = new WakeDirectionHistory();
            history.Initialize(0f);

            return history;
        }

        private static void AddLength(WakeSegment segment, float length)
        {
            segment.Add(new WakeSample(Vector2.zero, Vector2.right, 0f, 1f, 0.5f), length);
        }
    }
}
