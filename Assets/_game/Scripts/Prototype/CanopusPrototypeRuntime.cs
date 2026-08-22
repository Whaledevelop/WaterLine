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
        private readonly ShipDirectionResolver _directionResolver = new();
        private readonly ShipWaterInteractionResolver _waterInteractionResolver;

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
            _waterInteractionResolver = new ShipWaterInteractionResolver(visualProfile);
        }

        public void Initialize()
        {
            _model.Position = Vector2.zero;
            _model.Heading = 0f;
            _directionResolver.Initialize(_model.Heading);
            _shipView.Initialize(_visualProfile, _directionResolver.CurrentIndex);
            _foamView.Initialize(_visualProfile, _directionResolver.CurrentIndex);
            UpdateViews(0f);
        }

        public void OnUpdate()
        {
            ProcessInput();
            _movement.Tick(Time.deltaTime);
            if (_directionResolver.Resolve(_model.Heading))
            {
                _shipView.SetDirection(_directionResolver.CurrentIndex);
                _foamView.SetDirection(_directionResolver.CurrentIndex);
            }

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
            var interactionPose = _waterInteractionResolver.Resolve(_model.Position, _model.Heading);
            _shipView.Tick(_model.Position, deltaTime);
            _foamView.Tick(normalizedSpeed, deltaTime);
            _wakeView.Tick(interactionPose.Bow, interactionPose.Stern, interactionPose.Port,
                interactionPose.Starboard, normalizedSpeed, deltaTime);
            _waterView.Tick(Time.time);
        }
    }
}
