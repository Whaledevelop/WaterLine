using Game.Ships;
using Game.Water;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Whaledevelop;

namespace Game.Prototype
{
    public sealed class CanopusPrototypeRuntime : IUpdate
    {
        private readonly Camera _camera;
        private readonly ShipModel _model;
        private readonly ShipMovement _movement;
        private readonly ShipMovementSettings _movementSettings;
        private readonly ShipVisualProfile _visualProfile;
        private readonly ShipView _shipView;
        private readonly ShipFoamView _foamView;
        private readonly ShipWakeView _wakeView;
        private readonly WaterView _waterView;
        private readonly ShipVisualPoseResolver _visualPoseResolver;

        [Inject]
        public CanopusPrototypeRuntime(Camera camera, ShipModel model, ShipMovement movement,
            ShipMovementSettings movementSettings, ShipVisualProfile visualProfile, ShipView shipView,
            ShipFoamView foamView, ShipWakeView wakeView, WaterView waterView)
        {
            _camera = camera;
            _model = model;
            _movement = movement;
            _movementSettings = movementSettings;
            _visualProfile = visualProfile;
            _shipView = shipView;
            _foamView = foamView;
            _wakeView = wakeView;
            _waterView = waterView;
            _visualPoseResolver = new ShipVisualPoseResolver(visualProfile);
        }

        public void Initialize()
        {
            _model.Position = Vector2.zero;
            _model.Heading = 0f;
            var pose = _visualPoseResolver.Resolve(_model.Position, _model.Heading);
            _shipView.Initialize(_visualProfile, pose);
            _foamView.Initialize(_visualProfile, pose);
            UpdateViews(0f);
        }

        public void OnUpdate()
        {
            ProcessInput();
            _movement.Tick(Time.deltaTime);
            UpdateViews(Time.deltaTime);
        }

        private void ProcessInput()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var screenPosition = Mouse.current.position.ReadValue();
            var worldPosition = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
            _model.SetTarget(worldPosition);
        }

        private void UpdateViews(float deltaTime)
        {
            var normalizedSpeed = _model.Speed / _movementSettings.MaximumSpeed;
            var pose = _visualPoseResolver.Resolve(_model.Position, _model.Heading);
            _shipView.Tick(pose);
            _foamView.Tick(pose, normalizedSpeed, deltaTime);
            _wakeView.Tick(pose, normalizedSpeed, deltaTime);
            _waterView.Tick(Time.time);
        }
    }
}
