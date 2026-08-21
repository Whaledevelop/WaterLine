using System.Threading;
using UnityEngine;
using VContainer;
using Whaledevelop;
using Whaledevelop.VContainer;

namespace Game.Prototype
{
    public sealed class CanopusPrototypeEntryPoint : EntryPointBase
    {
        private readonly CanopusPrototypeRuntime _runtime;
        private readonly IUpdatesDispatcher _updatesDispatcher;

        [Inject]
        public CanopusPrototypeEntryPoint(CanopusPrototypeRuntime runtime, IUpdatesDispatcher updatesDispatcher)
        {
            _runtime = runtime;
            _updatesDispatcher = updatesDispatcher;
        }

        public override async Awaitable StartAsync(CancellationToken cancellation = new())
        {
            await Awaitable.NextFrameAsync(cancellation);
            _runtime.Initialize();
            _updatesDispatcher.TryRegister(_runtime);
        }
    }
}
