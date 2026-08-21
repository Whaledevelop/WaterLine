using Game.Ships;
using Game.Water;
using UnityEngine;
using VContainer;
using Whaledevelop.VContainer;

namespace Game.Prototype
{
    public sealed class CanopusPrototypeScope : GameLifetimeScopeBase<CanopusPrototypeEntryPoint>
    {
        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private ShipVisualProfile _visualProfile;

        [SerializeField]
        private ShipMovementSettings _movementSettings = new();

        [SerializeField]
        private ShipView _shipView;

        [SerializeField]
        private ShipFoamView _foamView;

        [SerializeField]
        private ShipWakeView _wakeView;

        [SerializeField]
        private WaterView _waterView;

        public void Setup(Camera camera, ShipVisualProfile visualProfile, ShipView shipView,
            ShipFoamView foamView, ShipWakeView wakeView, WaterView waterView)
        {
            _camera = camera;
            _visualProfile = visualProfile;
            _shipView = shipView;
            _foamView = foamView;
            _wakeView = wakeView;
            _waterView = waterView;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterInstance(_camera);
            builder.RegisterInstance(_visualProfile);
            builder.RegisterInstance(_movementSettings);
            builder.RegisterInstance(_shipView);
            builder.RegisterInstance(_foamView);
            builder.RegisterInstance(_wakeView);
            builder.RegisterInstance(_waterView);
            builder.Register<ShipModel>(Lifetime.Singleton);
            builder.Register<ShipMovement>(Lifetime.Singleton);
            builder.Register<CanopusPrototypeRuntime>(Lifetime.Singleton);
        }
    }
}
