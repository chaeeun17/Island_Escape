using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

/// 기존 NetworkCharacterController에 VR 이동과 맞지 않는 부분이 존재했음 (Move() 메서드)
/// 이동과 회전을 분리시키도록 로직을 변경.

namespace Fusion
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [NetworkBehaviourWeaved(NetworkCCData.WORDS)]
    // ReSharper disable once CheckNamespace
    public sealed unsafe class CustomNetworkCharacterController : NetworkTRSP, INetworkTRSPTeleport, IBeforeAllTicks, IAfterAllTicks, IBeforeCopyPreviousState
    {
        new ref NetworkCCData Data => ref ReinterpretState<NetworkCCData>();

        [Header("Character Controller Settings")]
        public float gravity = -20.0f;
        public float jumpImpulse = 8.0f;
        public float acceleration = 10.0f;
        public float braking = 10.0f;
        public float maxSpeed = 2.0f;
        public float rotationSpeed = 15.0f;

        Tick _initial;
        CharacterController _controller;

        public Vector3 Velocity
        {
            get => Data.Velocity;
            set => Data.Velocity = value;
        }

        public bool Grounded
        {
            get => Data.Grounded;
            set => Data.Grounded = value;
        }

        public void Teleport(Vector3? position = null, Quaternion? rotation = null)
        {
            _controller.enabled = false;
            NetworkTRSP.Teleport(this, transform, position, rotation);
            _controller.enabled = true;
        }


        public void Jump(bool ignoreGrounded = false, float? overrideImpulse = null)
        {
            if (Data.Grounded || ignoreGrounded)
            {
                var newVel = Data.Velocity;
                newVel.y += overrideImpulse ?? jumpImpulse;
                Data.Velocity = newVel;
            }
        }
        public void Move(Vector3 direction)
        {
            var deltaTime = Runner.DeltaTime;
            var previousPos = transform.position;
            var moveVelocity = Data.Velocity;

            direction = direction.normalized;

            if (Data.Grounded && moveVelocity.y < 0)
            {
                moveVelocity.y = 0f;
            }

            moveVelocity.y += gravity * Runner.DeltaTime;

            var horizontalVel = default(Vector3);
            horizontalVel.x = moveVelocity.x;
            horizontalVel.z = moveVelocity.z;

            if (direction == default)
            {
                horizontalVel = Vector3.Lerp(horizontalVel, default, braking * deltaTime);
            }
            else
            {
                horizontalVel = Vector3.ClampMagnitude(horizontalVel + direction * acceleration * deltaTime, maxSpeed);
                // transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Runner.DeltaTime);
            }

            moveVelocity.x = horizontalVel.x;
            moveVelocity.z = horizontalVel.z;

            _controller.Move(moveVelocity * deltaTime);

            Data.Velocity = (transform.position - previousPos) * Runner.TickRate;
            Data.Grounded = _controller.isGrounded;
        }

        public void Rotate(float angle)
        {
            Quaternion currentRotation = Data.TRSPData.Rotation;
            Quaternion targetRotation = currentRotation * Quaternion.Euler(0, angle, 0);

            Data.TRSPData.Rotation = targetRotation;
            transform.rotation = targetRotation;
        }

        public override void Spawned()
        {
            _initial = default;
            TryGetComponent(out _controller);
            // Without disabling and re-enabling the CharacterController here, the first Move call will reset the position to 0,0,0 instead of
            // keeping the position it was spawned at. Presumably disabling it clears some kind of internally cached "previous position" value
            _controller.enabled = false;
            _controller.enabled = true;
            CopyToBuffer();
        }

        public override void Render()
        {
            NetworkTRSP.Render(this, transform, false, false, false, ref _initial);
        }

        void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
        {
            CopyToEngine();
        }

        void IAfterAllTicks.AfterAllTicks(bool resimulation, int tickCount)
        {
            CopyToBuffer();
        }

        void IBeforeCopyPreviousState.BeforeCopyPreviousState()
        {
            CopyToBuffer();
        }

        void Awake()
        {
            TryGetComponent(out _controller);
        }

        void CopyToBuffer()
        {
            Data.TRSPData.Position = transform.position;
            Data.TRSPData.Rotation = transform.rotation;
        }

        void CopyToEngine()
        {
            // CC must be disabled before resetting the transform state
            _controller.enabled = false;

            // set position and rotation
            transform.SetPositionAndRotation(Data.TRSPData.Position, Data.TRSPData.Rotation);

            // Re-enable CC
            _controller.enabled = true;
        }
    }
}