/*
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

    [Header("Velocity Tracking Settings")]
    [SerializeField] private float maxLinearVelocity = 10f;
    [SerializeField] private float maxAngularVelocity = 20f; // rad/s

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return; // 물리는 오직 Host에서만 계산

        if (IsGrabbed)
        {
            _rb.isKinematic = false; // 🌟 물리 충돌이 가능하도록 유지

            Transform grabberHand = ResolveGrabberHand();
            if (grabberHand != null)
            {
                Vector3 targetPos = grabberHand.position + grabberHand.rotation * GrabOffsetPosition;
                Quaternion targetRot = grabberHand.rotation * GrabOffsetRotation;

                // 🌟 PD 컨트롤러: 목표 위치와의 차이만큼 속도 부여 (물리적으로 밀려서 이동)
                Vector3 posError = targetPos - _rb.position;
                Vector3 desiredVelocity = posError / Runner.DeltaTime;
                _rb.linearVelocity = Vector3.ClampMagnitude(desiredVelocity, maxLinearVelocity);

                Quaternion rotError = targetRot * Quaternion.Inverse(_rb.rotation);
                rotError.ToAngleAxis(out float angleDeg, out Vector3 axis);
                if (angleDeg > 180f) angleDeg -= 360f;

                if (!float.IsNaN(axis.x) && axis.sqrMagnitude > 0.0001f)
                {
                    Vector3 desiredAngularVelocity = axis * (angleDeg * Mathf.Deg2Rad / Runner.DeltaTime);
                    _rb.angularVelocity = Vector3.ClampMagnitude(desiredAngularVelocity, maxAngularVelocity);
                }
            }
        }
        else
        {
            _rb.isKinematic = !HasStateAuthority; // 안 잡혔을 때는 기존 로직 유지
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

        // if (_rb != null) _rb.isKinematic = true;

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
            if (_rb != null)
            {
                // 🌟 Teleport/위치 강제 설정 불필요 - 이미 물리적으로 정확한 위치에 있음
                _rb.isKinematic = false;

                // 손 추적 속도에 던지기 속도를 더함 (또는 원하시면 덮어쓰기만 해도 됨)
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
        // 🌟 더 이상 transform을 직접 강제 조작하지 않음
        //    NetworkRigidbody3D가 Host의 물리 시뮬레이션 결과를 모든 클라이언트에 보간해서 반영

        if (_isLocalGrabbing && _localGrabbingHand != null)
        {
            RecordVelocitySample(); // 던지기 속도 계산용 샘플링은 계속 필요
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

        Debug.Log($"[GrabDebug] attachTransform={_grabInteractable.attachTransform?.name ?? "NULL"}");
    }
}
*/

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
        Debug.Log($"🔥 OnIsGrabbedChanged 발동! IsGrabbed={IsGrabbed}, _isPendingDetach={_isPendingDetach}");
        if (!IsGrabbed && _isPendingDetach)
        {
            // 🌟 Host의 확정 값이 도착했으니 얼려두던 로컬 오버라이드 해제
            //    이 시점부터는 NetworkRigidbody3D의 정상 보간(Teleport로 확정된 finalPos 기준)에 맡김
            _isPendingDetach = false;

            // 🌟 그랩 중 켜뒀던 로컬 시뮬레이션(예측)을 여기서 해제.
            //    Host의 detach 확정이 도착한 뒤에 꺼야 위치가 안정된 상태에서 넘어감.
            if (!Object.HasStateAuthority)
            {
                Runner.SetIsSimulated(Object, false);
            }
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

    public bool IsCurrentlyGrabbed => IsGrabbed;
    public PlayerRef CurrentGrabber => GrabbingPlayer;
    public bool IsLocalGrabbing => _isLocalGrabbing; // 로컬 전용, 네트워크 지연 없음
                                                     // 기존 IsLocalGrabbing 대신, 혹은 추가로
    public bool IsLocallyOwnedAction => _isLocalGrabbing || _isPendingDetach;

    // 🌟 던지기 속도 계산용 샘플 버퍼
    private readonly Queue<(Vector3 pos, Quaternion rot, float time)> _velocitySamples = new Queue<(Vector3, Quaternion, float)>();

    [SerializeField] private NetworkRigidbody3D _networkRb; // 인스펙터에서 연결 또는 GetComponent

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
        if (_networkRb == null) _networkRb = GetComponent<NetworkRigidbody3D>();

        // 🌟 XRI 자체 Throw On Detach 기능을 끈다. 이 스크립트가 놓는 순간 속도를 직접 계산해서
        //    RPC_RequestDetach -> ApplyDetach(Host)로 Rigidbody에 적용하는 커스텀 시스템을 이미 가지고 있다.
        //    XRI의 내장 throw 로직은 이걸 모르고 별도로 자기 스스로 Rigidbody.velocity를 건드리려 하는데,
        //    클라이언트는 ApplyDetach가 로컬에서 실행되지 않아(Host에서만 RPC로 실행됨) 그랩 중 켜둔
        //    isKinematic=true가 놓은 뒤에도 그대로 남아있어 "Cannot throw a kinematic Rigidbody..." 예외가 난다.
        _grabInteractable.throwOnDetach = false;

        _grabInteractable.selectEntered.AddListener(OnGrabEntered);
        _grabInteractable.selectExited.AddListener(OnGrabExited);
    }

    public override void FixedUpdateNetwork()
    {
        if (_rb != null)
        {
            _rb.isKinematic = IsGrabbed ? true : !HasStateAuthority;
        }

        if (IsGrabbed && HasStateAuthority)
        {
            Transform grabberHand = ResolveGrabberHand();

            // 🌟 진단용 로그 (틱마다 찍히면 너무 많으니 필요시 주석 처리)
            Debug.Log($"[GrabItem:{name}] Hold 중. GrabbingPlayer={GrabbingPlayer}, grabberHand={(grabberHand == null ? "NULL" : grabberHand.name)}, HandPos={(grabberHand != null ? grabberHand.position.ToString() : "N/A")}");

            if (grabberHand != null)
            {
                Vector3 targetPos = grabberHand.position + grabberHand.rotation * GrabOffsetPosition;
                Quaternion targetRot = grabberHand.rotation * GrabOffsetRotation;

                if (_networkRb != null)
                    _networkRb.Teleport(targetPos, targetRot);
                else
                {
                    transform.position = targetPos;
                    transform.rotation = targetRot;
                }
            }
        }
    }

    private void ApplyDetach(Vector3 finalPos, Quaternion finalRot, Vector3 velocity, Vector3 angularVelocity)
    {
        try
        {
            if (_networkRb != null)
            {
                _networkRb.Teleport(finalPos, finalRot); // 이것만으로 위치+회전 반영 충분
            }
            else
            {
                transform.position = finalPos;
                transform.rotation = finalRot;
            }

            if (_rb != null)
            {
                // 🌟 _rb.position / _rb.rotation 직접 대입 제거
                // 🌟 Physics.SyncTransforms() 제거 (Teleport가 이미 처리)
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
            IsGrabbed = false; // 이 값이 바뀌면 다음 FixedUpdateNetwork에서 isKinematic이 자연스럽게 false로 전환됨
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

        // 🌟 네트워크 아바타 손(player.NetLeftHand/NetRightHand)이 아니라
        //    로컬 XR Origin의 실제 트래킹 손(InputHandler)과 비교해야 함
        GrabHand handSide = GrabHand.None;
        if (InputHandler.Instance._localLeftHand != null &&
            (interactorTransform == InputHandler.Instance._localLeftHand || interactorTransform.IsChildOf(InputHandler.Instance._localLeftHand)))
        {
            handSide = GrabHand.Left;
        }
        else if (InputHandler.Instance._localRightHand != null &&
                 (interactorTransform == InputHandler.Instance._localRightHand || interactorTransform.IsChildOf(InputHandler.Instance._localRightHand)))
        {
            handSide = GrabHand.Right;
        }

        _localGrabbingHand = interactorTransform;
        _isLocalGrabbing = true;

        // 🌟 StateAuthority(Host)가 아닌 클라이언트가 잡았을 경우, 이 아이템을 로컬 시뮬레이션에
        //    잠깐 포함시켜야 NetworkRigidbody3D.Render()가 매 프레임 위치를 되돌리지 않는다.
        //    (InterpolationTarget이 없는 상태에서 IsInSimulation이 false면 Fusion이 계속
        //     transform을 네트워크 스냅샷 위치로 강제로 스냅시켜, 손을 따라가도 곧바로 되돌아감)
        if (!Object.HasStateAuthority)
        {
            Runner.SetIsSimulated(Object, true);
        }

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

    // 🌟 이 아이템을 실제로 쥐고 있던 로컬 유저가 놓는 순간 발생.
    //    Zone 등이 selectExited에 별도 리스너를 등록해 감지하는 대신 이걸 구독하면,
    //    Client에서 Physics.Simulate()가 아예 돌지 않아(ClientPhysicsSimulation.Disabled) 트리거
    //    콜백(OnTriggerEnter/Exit) 자체가 불안정한 문제를 피하고, 확실히 발생하는 이 콜백에 의존할 수 있다.
    public static event System.Action<NetworkGrabItem, Vector3> LocallyReleased;

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

        LocallyReleased?.Invoke(this, finalPos);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDetach(Vector3 finalPos, Quaternion finalRot, Vector3 velocity, Vector3 angularVelocity)
    {
        ApplyDetach(finalPos, finalRot, velocity, angularVelocity);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDespawn()
    {
        if (Object != null && Object.IsValid && Runner != null)
            Runner.Despawn(Object);
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

            Physics.SyncTransforms(); // 🌟 콜라이더 위치 즉시 갱신 - 잡고 있는 동안도 물리적으로 정확한 위치 유지

            RecordVelocitySample();
        }
        else if (_isPendingDetach)
        {
            transform.position = _pendingFinalPosition;
            transform.rotation = _pendingFinalRotation;
            Physics.SyncTransforms(); // 🌟 놓은 직후 콜라이더 위치 즉시 갱신 - 재상호작용 가능하도록
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

            Physics.SyncTransforms(); // 🌟 다른 사람이 잡고 있는 걸 보는 화면에서도 동일하게 동기화
        }
        else
        {
            // 🌟 아무도 안 잡고 있는 상태(가만히 있거나 낙하 중인 proxy)에서는 Fusion의
            //    NetworkRigidbody3D.Render()가 InterpolationTarget 없이 이 transform을 직접 움직이는데,
            //    Render()는 Physics.SyncTransforms()를 호출하지 않는다.
            //    이 프로젝트는 Physics 자동 동기화를 쓰지 않으므로, 여기서 동기화를 안 해주면
            //    다음 물리 스텝 전까지 콜라이더(raycast/트리거 판정용)가 화면에 보이는 위치와
            //    어긋난 채로 남아있게 된다 (재그랩 시 raycast 위치 불일치, InventoryZone 트리거 미감지의 원인).
            Physics.SyncTransforms();
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
