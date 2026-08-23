using Fusion;
using Fusion.Addons.Physics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class NetworkGrabItem : NetworkBehaviour
{
    private enum GrabHand { None, Left, Right }

    [Header("Velocity Sampling (Throw)")]
    [SerializeField] private int velocitySampleCount = 5;

    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rb;

    // 🌟 다른 클라이언트 복제용
    [Networked, OnChangedRender(nameof(OnIsGrabbedChanged))]
    private NetworkBool IsGrabbed { get; set; }

    private void OnIsGrabbedChanged()
    {
        if (!IsGrabbed && _isPendingDetach)
        {
            // 🌟 Host의 확정 값이 도착했으니 얼려두던 로컬 오버라이드 해제
            //    이 시점부터는 NetworkRigidbody3D의 정상 보간(Teleport로 확정된 finalPos 기준)에 맡김
            _isPendingDetach = false;
        }
    }
    [Networked] private Vector3 GrabOffsetPosition { get; set; }
    [Networked] private Quaternion GrabOffsetRotation { get; set; }
    [Networked] private PlayerRef GrabbingPlayer { get; set; }
    [Networked] private GrabHand GrabbingHandSide { get; set; }

    [Networked] private Vector3 GrabStartWorldPosition { get; set; }
    [Networked] private Quaternion GrabStartWorldRotation { get; set; }
    [Networked] private float GrabStartTime { get; set; }
    [Networked] private NetworkBool IsFarAttach { get; set; }
    [Networked] private float GrabEaseInTime { get; set; }

    // 로컬 전용
    private Transform _localGrabbingHand;
    private bool _isLocalGrabbing = false;
    private Vector3 _localOffsetPosition;
    private Quaternion _localOffsetRotation;
    private Vector3 _localGrabStartPosition;
    private Quaternion _localGrabStartRotation;
    private float _localGrabStartTime;
    private bool _localIsFarAttach;
    private float _localEaseInTime;

    private Player _cachedGrabberPlayer;

    // 🌟 던지기 속도 계산용 샘플 버퍼
    private readonly Queue<(Vector3 pos, Quaternion rot, float time)> _velocitySamples = new Queue<(Vector3, Quaternion, float)>();

    [SerializeField] private NetworkRigidbody3D _networkRb; // 인스펙터에서 연결 또는 GetComponent

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
        if (_networkRb == null) _networkRb = GetComponent<NetworkRigidbody3D>();

        _grabInteractable.selectEntered.AddListener(OnGrabEntered);
        _grabInteractable.selectExited.AddListener(OnGrabExited);
    }

    public override void FixedUpdateNetwork()
    {
        if (_rb != null)
        {
            _rb.isKinematic = IsGrabbed ? true : !HasStateAuthority;
        }

        // 🌟 .enabled 토글 대신, 잡혀 있는 동안 Host가 매 틱 정확한 위치를 강제 주입
        if (IsGrabbed && HasStateAuthority)
        {
            Transform grabberHand = ResolveGrabberHand();
            if (grabberHand != null)
            {
                Vector3 targetPos = grabberHand.position + grabberHand.rotation * GrabOffsetPosition;
                Quaternion targetRot = grabberHand.rotation * GrabOffsetRotation;

                if (_networkRb != null)
                {
                    _networkRb.Teleport(targetPos, targetRot); // 네트워크 권위 위치를 강제 갱신
                }
                else
                {
                    transform.position = targetPos;
                    transform.rotation = targetRot;


                }

                _networkRb.Teleport(targetPos, targetRot);
                Debug.Log($"[Grab 중] transform.position={transform.position}, _rb.position={_rb.position}");
            }
        }
    }
    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabEntered);
            _grabInteractable.selectExited.RemoveListener(OnGrabExited);
        }
    }
    private void OnGrabEntered(SelectEnterEventArgs args)
    {
        if (Object == null || !Object.IsValid) return;

        var interactorTransform = args.interactorObject.transform;
        var playerRoot = interactorTransform.root;
        var networkObject = playerRoot.GetComponent<NetworkObject>();
        var player = playerRoot.GetComponent<Player>();

        if (networkObject == null || !networkObject.HasInputAuthority || player == null) return;

        GrabHand handSide = GrabHand.None;
        if (interactorTransform == player.NetLeftHand || interactorTransform.IsChildOf(player.NetLeftHand))
        {
            handSide = GrabHand.Left;
        }
        else if (interactorTransform == player.NetRightHand || interactorTransform.IsChildOf(player.NetRightHand))
        {
            handSide = GrabHand.Right;
        }

        _localGrabbingHand = interactorTransform;
        _isLocalGrabbing = true;

        var (offsetPos, offsetRot) = CalculateAttachOffset(_localGrabbingHand);
        _localOffsetPosition = offsetPos;
        _localOffsetRotation = offsetRot;

        _localGrabStartPosition = transform.position;
        _localGrabStartRotation = transform.rotation;
        _localGrabStartTime = (float)Runner.SimulationTime;

        float easeInTime = _grabInteractable.attachEaseInTime;
        _localIsFarAttach = easeInTime > 0f;
        _localEaseInTime = easeInTime;

        _velocitySamples.Clear();

        if (_rb != null) _rb.isKinematic = true;

        SetGrabState(true, _localOffsetPosition, _localOffsetRotation, Runner.LocalPlayer, handSide,
                     _localGrabStartPosition, _localGrabStartRotation, _localGrabStartTime, _localIsFarAttach, easeInTime);
    }

    // 필드 추가
    private bool _isPendingDetach = false;
    private Vector3 _pendingFinalPosition;
    private Quaternion _pendingFinalRotation;

    private void OnGrabExited(SelectExitEventArgs args)
    {
        if (!_isLocalGrabbing) return;

        Vector3 throwVelocity = Vector3.zero;
        Vector3 throwAngularVelocity = Vector3.zero;

        if (_velocitySamples.Count >= 2)
        {
            var samples = _velocitySamples.ToArray();
            var oldest = samples[0];
            var newest = samples[samples.Length - 1];

            float dt = newest.time - oldest.time;
            if (dt > 0.0001f)
            {
                throwVelocity = (newest.pos - oldest.pos) / dt * _grabInteractable.throwVelocityScale;

                Quaternion deltaRot = newest.rot * Quaternion.Inverse(oldest.rot);
                deltaRot.ToAngleAxis(out float angleDeg, out Vector3 axis);
                if (angleDeg > 180f) angleDeg -= 360f;
                if (float.IsNaN(axis.x) || axis.sqrMagnitude < 0.0001f) axis = Vector3.zero;

                throwAngularVelocity = axis * (angleDeg * Mathf.Deg2Rad / dt) * _grabInteractable.throwAngularVelocityScale;
            }
        }

        Vector3 finalPos = transform.position;
        Quaternion finalRot = transform.rotation;

        _isLocalGrabbing = false;
        _localGrabbingHand = null;
        _velocitySamples.Clear();

        // 🌟 네트워크 확인 전까지 "그 자리에 얼려서" 유지
        _isPendingDetach = true;
        _pendingFinalPosition = finalPos;
        _pendingFinalRotation = finalRot;

        RPC_RequestDetach(finalPos, finalRot, throwVelocity, throwAngularVelocity);
        Debug.Log("IsGrabbed : " + IsGrabbed);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDetach(Vector3 finalPos, Quaternion finalRot, Vector3 velocity, Vector3 angularVelocity)
    {
        ApplyDetach(finalPos, finalRot, velocity, angularVelocity);
    }

    // 🌟 Host에서만 실행: 위치 확정 → Kinematic 해제 → 속도 적용 → 상태 초기화, 이 순서를 하나의 메서드 안에서 원자적으로 보장
    private void ApplyDetach(Vector3 finalPos, Quaternion finalRot, Vector3 velocity, Vector3 angularVelocity)
    {
        try
        {
            if (_networkRb != null)
            {
                _networkRb.Teleport(finalPos, finalRot);
            }
            else
            {
                transform.position = finalPos;
                transform.rotation = finalRot;
            }

            if (_rb != null)
            {
                // 🌟 Kinematic → Non-Kinematic 전환 전, Rigidbody 내부 위치를 명시적으로 강제 동기화
                _rb.position = finalPos;
                _rb.rotation = finalRot;
                Physics.SyncTransforms(); // 물리 엔진에 즉시 반영 강제

                _rb.isKinematic = false;
                _rb.linearVelocity = velocity;
                _rb.angularVelocity = angularVelocity;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkGrabItem] ApplyDetach 중 예외 발생: {e}");
        }
        finally
        {
            IsGrabbed = false;
            GrabOffsetPosition = Vector3.zero;
            GrabOffsetRotation = Quaternion.identity;
            GrabbingPlayer = PlayerRef.None;
            GrabbingHandSide = GrabHand.None;
            GrabStartWorldPosition = Vector3.zero;
            GrabStartWorldRotation = Quaternion.identity;
            GrabStartTime = 0f;
            IsFarAttach = false;
            GrabEaseInTime = 0f;
        }
    }
    private (Vector3 offsetPos, Quaternion offsetRot) CalculateAttachOffset(Transform hand)
    {
        Transform attachPoint = _grabInteractable.attachTransform != null
            ? _grabInteractable.attachTransform
            : transform;

        Vector3 localPosToAttach = attachPoint == transform ? Vector3.zero : attachPoint.localPosition;
        Quaternion localRotToAttach = attachPoint == transform ? Quaternion.identity : attachPoint.localRotation;

        Quaternion itemRotAtHand = hand.rotation * Quaternion.Inverse(localRotToAttach);
        Vector3 itemPosAtHand = hand.position - itemRotAtHand * localPosToAttach;

        Vector3 offsetPos = Quaternion.Inverse(hand.rotation) * (itemPosAtHand - hand.position);
        Quaternion offsetRot = Quaternion.Inverse(hand.rotation) * itemRotAtHand;

        return (offsetPos, offsetRot);
    }

    private void LateUpdate()
    {
        if (_isLocalGrabbing && _localGrabbingHand != null)
        {
            Vector3 idealPos = _localGrabbingHand.position + _localGrabbingHand.rotation * _localOffsetPosition;
            Quaternion idealRot = _localGrabbingHand.rotation * _localOffsetRotation;

            if (_localIsFarAttach)
            {
                float elapsed = (float)Runner.SimulationTime - _localGrabStartTime;
                float t = Mathf.Clamp01(elapsed / _localEaseInTime);
                float smoothT = t * t * (3f - 2f * t);

                transform.position = Vector3.Lerp(_localGrabStartPosition, idealPos, smoothT);
                transform.rotation = Quaternion.Slerp(_localGrabStartRotation, idealRot, smoothT);
            }
            else
            {
                transform.position = idealPos;
                transform.rotation = idealRot;
            }

            RecordVelocitySample();
        }
        else if (_isPendingDetach)  // 🌟 바깥으로 꺼냄 - 이제 정상적으로 도달 가능
        {
            transform.position = _pendingFinalPosition;
            transform.rotation = _pendingFinalRotation;
        }
        else if (IsGrabbed)
        {
            Transform grabberHand = ResolveGrabberHand();
            if (grabberHand == null) return;

            Vector3 idealPos = grabberHand.position + grabberHand.rotation * GrabOffsetPosition;
            Quaternion idealRot = grabberHand.rotation * GrabOffsetRotation;

            if (IsFarAttach)
            {
                float elapsed = (float)Runner.SimulationTime - GrabStartTime;
                float t = Mathf.Clamp01(elapsed / GrabEaseInTime);
                float smoothT = t * t * (3f - 2f * t);

                transform.position = Vector3.Lerp(GrabStartWorldPosition, idealPos, smoothT);
                transform.rotation = Quaternion.Slerp(GrabStartWorldRotation, idealRot, smoothT);
            }
            else
            {
                transform.position = idealPos;
                transform.rotation = idealRot;
            }
        }

        if (Input.GetKeyDown(KeyCode.F1)) // 아무 키나 지정
        {
            Debug.Log($"[GrabItem Debug] transform.position={transform.position}, " +
                       $"_rb.position={_rb.position}, IsGrabbed={IsGrabbed}, " +
                       $"_rb.isKinematic={_rb.isKinematic}");
        }
    }

    private void RecordVelocitySample()
    {
        _velocitySamples.Enqueue((transform.position, transform.rotation, Time.time));
        while (_velocitySamples.Count > velocitySampleCount)
        {
            _velocitySamples.Dequeue();
        }
    }

    private Transform ResolveGrabberHand()
    {
        if (_cachedGrabberPlayer == null || _cachedGrabberPlayer.Object.InputAuthority != GrabbingPlayer)
        {
            var grabberObject = Runner.GetPlayerObject(GrabbingPlayer);
            if (grabberObject == null) return null;

            _cachedGrabberPlayer = grabberObject.GetComponent<Player>();
            if (_cachedGrabberPlayer == null) return null;
        }

        return GrabbingHandSide switch
        {
            GrabHand.Left => _cachedGrabberPlayer.NetLeftHand,
            GrabHand.Right => _cachedGrabberPlayer.NetRightHand,
            _ => null
        };
    }

    private void SetGrabState(bool grabbed, Vector3 offsetPos, Quaternion offsetRot, PlayerRef player, GrabHand handSide,
                               Vector3 startPos, Quaternion startRot, float startTime, bool isFarAttach, float easeInTime)
    {
        if (Object.HasStateAuthority)
        {
            ApplyGrabState(grabbed, offsetPos, offsetRot, player, handSide, startPos, startRot, startTime, isFarAttach, easeInTime);
        }
        else
        {
            RPC_RequestSetGrabState(grabbed, offsetPos, offsetRot, player, handSide, startPos, startRot, startTime, isFarAttach, easeInTime);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSetGrabState(bool grabbed, Vector3 offsetPos, Quaternion offsetRot, PlayerRef player, GrabHand handSide,
                                          Vector3 startPos, Quaternion startRot, float startTime, bool isFarAttach, float easeInTime)
    {
        ApplyGrabState(grabbed, offsetPos, offsetRot, player, handSide, startPos, startRot, startTime, isFarAttach, easeInTime);
    }

    private void ApplyGrabState(bool grabbed, Vector3 offsetPos, Quaternion offsetRot, PlayerRef player, GrabHand handSide,
                                 Vector3 startPos, Quaternion startRot, float startTime, bool isFarAttach, float easeInTime)
    {
        IsGrabbed = grabbed;
        GrabOffsetPosition = offsetPos;
        GrabOffsetRotation = offsetRot;
        GrabbingPlayer = player;
        GrabbingHandSide = handSide;
        GrabStartWorldPosition = startPos;
        GrabStartWorldRotation = startRot;
        GrabStartTime = startTime;
        IsFarAttach = isFarAttach;
        GrabEaseInTime = easeInTime;
    }
}