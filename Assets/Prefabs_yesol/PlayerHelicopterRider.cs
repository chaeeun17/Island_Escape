using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerHelicopterRider : NetworkBehaviour
{
    [Header("VR Components to Disable")]
    [SerializeField] private CharacterController characterController;

    [Header("Player Reference")]
    [SerializeField] private Player playerScript;

    [Header("Climb Settings")]
    [SerializeField] private float climbSpeedMultiplier = 1.0f;
    [SerializeField] private float maxClimbY = 2.0f;
    [SerializeField] private float minClimbY = -2.0f;

    private Rigidbody _rigidbody;

    // 🌟 다른 클라이언트에게 보여주기 위한 복제 전용 (본인 렌더링에는 사용 안 함)
    [Networked] private NetworkBool IsAttached { get; set; }
    [Networked] private Vector3 OffsetPosition { get; set; }
    [Networked] private Quaternion OffsetRotation { get; set; }
    [Networked] private NetworkId HelicopterId { get; set; }
    [Networked] private int RungIndex { get; set; } = -1;

    // 🌟 본인(소유자) 전용 로컬 상태 - RPC 왕복 없이 즉시 반영됨
    private bool _localIsAttached = false;
    private Transform _localTargetRung;
    private Vector3 _localOffsetPosition;
    private Quaternion _localOffsetRotation;
    private HelicopterController _cachedHelicopterLocal;

    // 손 -> 잡고 있는 발판 매핑 (양손 클라이밍 추적용)
    private readonly Dictionary<Transform, Transform> _grabbingHandsToRung = new Dictionary<Transform, Transform>();

    private Transform _grabbingHand;      // 클라이밍 계산에 쓰이는 "주" 손
    private Vector3 _lastHandLocalPos;

    // 다른 클라이언트에게도 노출: 지금 사다리에 붙어있는 상태인지
    public bool IsCurrentlyAttached => HasInputAuthority ? _localIsAttached : IsAttached;

    [SerializeField] private bool preventReleaseWhileClimbing = false;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (playerScript == null) playerScript = GetComponent<Player>();
    }

    // 손 하나가 발판을 잡았을 때 호출
    public void AttachToLadder(Transform rungTransform, Transform handTransform, HelicopterController helicopter)
    {
        if (!HasInputAuthority) return;

        int index = helicopter.GetRungIndex(rungTransform);
        if (index < 0)
        {
            Debug.LogWarning("[PlayerHelicopterRider] 등록되지 않은 Rung입니다.");
            return;
        }

        bool wasAttached = _localIsAttached;

        _grabbingHandsToRung[handTransform] = rungTransform;
        _cachedHelicopterLocal = helicopter;

        // 🌟 rungTransform이 아니라 helicopter.transform 기준으로 오프셋 계산
        Transform heliRoot = helicopter.transform;
        Vector3 initialOffsetPos = heliRoot.InverseTransformPoint(transform.position);
        Quaternion initialOffsetRot = Quaternion.Inverse(heliRoot.rotation) * transform.rotation;

        _localTargetRung = rungTransform;       // 클라이밍 축 계산에는 여전히 사용
        _localOffsetPosition = initialOffsetPos; // 🌟 헬기 기준
        _localOffsetRotation = initialOffsetRot;
        _localIsAttached = true;

        _grabbingHand = handTransform;
        _lastHandLocalPos = heliRoot.InverseTransformPoint(handTransform.position); // 🌟 헬기 기준으로 통일

        if (!wasAttached)
        {
            if (playerScript != null) playerScript.IsEscaped = true;
            SetLocomotionEnabled(false);
        }

        var heliNetObj = helicopter.Object;
        NetworkId heliId = heliNetObj != null ? heliNetObj.Id : default;
        SetAttachState(true, initialOffsetPos, initialOffsetRot, heliId, index);
    }

    // 손 하나가 발판을 놓았을 때 호출 (완전 탈착은 여기서 내부적으로 판단)
    public void DetachHand(Transform handTransform)
    {
        if (!HasInputAuthority) return;
        if (preventReleaseWhileClimbing) return; // 🌟 아예 탈착 로직을 막음

        _grabbingHandsToRung.Remove(handTransform);

        if (_grabbingHandsToRung.Count == 0)
        {
            if (playerScript != null) playerScript.IsEscaped = false;

            _localIsAttached = false;
            _localTargetRung = null;
            _grabbingHand = null;

            SetAttachState(false, Vector3.zero, Quaternion.identity, default, -1);

            // 🌟 로코모션을 켜기 전에, CC의 내부 위치를 현재 위치로 먼저 동기화
            if (playerScript != null)
            {
                playerScript.SyncControllerPositionToTransform();
            }

            SetLocomotionEnabled(true);
        }
        else
        {
            Transform remainingHand = null;
            Transform remainingRung = null;
            foreach (var kv in _grabbingHandsToRung)
            {
                remainingHand = kv.Key;
                remainingRung = kv.Value;
            }

            // 🌟 remainingRung이 아니라 헬기 루트 기준으로 통일
            Transform heliRoot = _cachedHelicopterLocal != null ? _cachedHelicopterLocal.transform : null;
            if (heliRoot == null) return;

            Vector3 newOffsetPos = heliRoot.InverseTransformPoint(transform.position);
            Quaternion newOffsetRot = Quaternion.Inverse(heliRoot.rotation) * transform.rotation;

            _localTargetRung = remainingRung;
            _localOffsetPosition = newOffsetPos;
            _localOffsetRotation = newOffsetRot;
            _grabbingHand = remainingHand;
            _lastHandLocalPos = heliRoot.InverseTransformPoint(remainingHand.position); // 🌟 여기도 heliRoot 기준

            int index = _cachedHelicopterLocal.GetRungIndex(remainingRung);
            NetworkId heliId = _cachedHelicopterLocal.Object != null ? _cachedHelicopterLocal.Object.Id : default;

            SetAttachState(true, newOffsetPos, newOffsetRot, heliId, index);
        }
    }

    // ==========================================
    // [Networked] 상태 쓰기 헬퍼 (다른 클라이언트 복제 전용)
    // ==========================================
    private void SetAttachState(bool attached, Vector3 offsetPos, Quaternion offsetRot, NetworkId heliId, int rungIndex)
    {
        if (Object.HasStateAuthority)
        {
            ApplyAttachState(attached, offsetPos, offsetRot, heliId, rungIndex);
        }
        else
        {
            RPC_RequestSetAttachState(attached, offsetPos, offsetRot, heliId, rungIndex);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSetAttachState(bool attached, Vector3 offsetPos, Quaternion offsetRot, NetworkId heliId, int rungIndex)
    {
        ApplyAttachState(attached, offsetPos, offsetRot, heliId, rungIndex);
    }

    private void ApplyAttachState(bool attached, Vector3 offsetPos, Quaternion offsetRot, NetworkId heliId, int rungIndex)
    {
        IsAttached = attached;
        OffsetPosition = offsetPos;
        OffsetRotation = offsetRot;
        HelicopterId = heliId;
        RungIndex = rungIndex;
    }

    private void LateUpdate()
    {
        if (HasInputAuthority)
        {
            if (!_localIsAttached || _cachedHelicopterLocal == null) return;

            Transform heliRoot = _cachedHelicopterLocal.transform;

            if (_grabbingHand != null)
            {
                // 🌟 클라이밍(손 이동)도 헬기 기준 좌표계로 계산
                Vector3 currentHandLocalPos = heliRoot.InverseTransformPoint(_grabbingHand.position);
                float deltaY = currentHandLocalPos.y - _lastHandLocalPos.y;

                if (Mathf.Abs(deltaY) > 0.0001f)
                {
                    _localOffsetPosition.y -= deltaY * climbSpeedMultiplier;
                    _localOffsetPosition.y = Mathf.Clamp(_localOffsetPosition.y, minClimbY, maxClimbY);

                    SetAttachState(true, _localOffsetPosition, _localOffsetRotation, HelicopterId, RungIndex);
                }

                _lastHandLocalPos = currentHandLocalPos;
            }

            // 🌟 헬기 기준으로 월드 위치 계산 → 헬기와의 거리가 항상 고정 유지
            transform.position = heliRoot.TransformPoint(_localOffsetPosition);
            transform.rotation = heliRoot.rotation * _localOffsetRotation;
        }
        else
        {
            if (!IsAttached) return;

            Transform heliTransform = ResolveHelicopterTransform();
            if (heliTransform == null) return;

            transform.position = heliTransform.TransformPoint(OffsetPosition);
            transform.rotation = heliTransform.rotation * OffsetRotation;
        }
    }

    private Transform ResolveHelicopterTransform()
    {
        if (!HelicopterId.IsValid || Runner == null) return null;

        var heliObj = Runner.FindObject(HelicopterId);
        return heliObj != null ? heliObj.transform : null;
    }

    /*
    private Transform ResolveRungTransform()
    {
        if (RungIndex < 0 || !HelicopterId.IsValid || Runner == null) return null;

        var heliObj = Runner.FindObject(HelicopterId);
        if (heliObj == null) return null;

        var helicopter = heliObj.GetComponent<HelicopterController>();
        if (helicopter == null) return null;

        return helicopter.GetRungByIndex(RungIndex);
    }
    */

    private void SetLocomotionEnabled(bool isEnabled)
    {
        if (characterController != null) characterController.enabled = isEnabled;
        if (_rigidbody != null) _rigidbody.isKinematic = !isEnabled;
    }
}