using Fusion;
using UnityEngine;

public class PlayerHelicopterRider : NetworkBehaviour
{
    [Header("VR Components to Disable")]
    [SerializeField] private CharacterController characterController;

    [Header("Player Reference")]
    [SerializeField] private Player playerScript;

    [Header("Climb Settings")]
    [SerializeField] private float climbSpeedMultiplier = 1.0f; // 클라이밍 감도
    [SerializeField] private float maxClimbY = 10.0f;           // 사다리 최상단 제한
    [SerializeField] private float minClimbY = -2.0f;           // 사다리 최하단 제한

    private Rigidbody _rigidbody;
    private Transform _targetRung;
    private Vector3 _offsetPosition;
    private Quaternion _offsetRotation;
    private bool _isAttached = false;

    // 클라이밍 계산용 변수
    private Transform _grabbingHand;      // 잡고 있는 손 Transform
    private Vector3 _lastHandLocalPos;    // 이전 프레임의 손 위치

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (playerScript == null) playerScript = GetComponent<Player>();
    }

    // 사다리를 잡았을 때 호출 (RopeRung에서 handTransform을 전달받음)
    public void AttachToLadder(Transform rungTransform, Transform handTransform)
    {
        _targetRung = rungTransform;
        _grabbingHand = handTransform;

        if (_grabbingHand != null)
        {
            // 잡은 손의 사다리 기준 상대 위치 기록
            _lastHandLocalPos = _targetRung.InverseTransformPoint(_grabbingHand.position);
        }

        if (playerScript != null)
        {
            playerScript.IsEscaped = true;
        }

        AttachToLadderInternal();
    }

    public void DetachFromLadder()
    {
        if (playerScript != null)
        {
            playerScript.IsEscaped = false;
        }

        _grabbingHand = null;
        DetachFromLadderInternal();
    }

    private void AttachToLadderInternal()
    {
        if (_isAttached || _targetRung == null) return;

        _isAttached = true;
        SetLocomotionEnabled(false);

        // 첫 부착 시 오프셋 계산
        _offsetPosition = _targetRung.InverseTransformPoint(transform.position);
        _offsetRotation = Quaternion.Inverse(_targetRung.rotation) * transform.rotation;

        transform.SetParent(_targetRung);
    }

    private void DetachFromLadderInternal()
    {
        if (!_isAttached) return;

        _isAttached = false;
        _targetRung = null;

        transform.SetParent(null);
        SetLocomotionEnabled(true);
    }

    private void LateUpdate()
    {
        if (!_isAttached || _targetRung == null) return;

        // 1. 손 이동에 따른 Y 오프셋 계산을 LateUpdate 내에서 직접 수행
        if (_grabbingHand != null)
        {
            Vector3 currentHandLocalPos = _targetRung.InverseTransformPoint(_grabbingHand.position);

            // 프레임 간 손 이동량 계산
            float deltaY = currentHandLocalPos.y - _lastHandLocalPos.y;

            // 떨림 방지: 미세한 노이즈(0.1mm 이하 변화) 무시
            if (Mathf.Abs(deltaY) > 0.0001f)
            {
                _offsetPosition.y -= deltaY * climbSpeedMultiplier;
                _offsetPosition.y = Mathf.Clamp(_offsetPosition.y, minClimbY, maxClimbY);
            }

            // 이번 프레임 기준 위치로 갱신
            _lastHandLocalPos = currentHandLocalPos;
        }

        // 2. 최종 월드 Transform 적용
        transform.position = _targetRung.TransformPoint(_offsetPosition);
        transform.rotation = _targetRung.rotation * _offsetRotation;
    }

    private void SetLocomotionEnabled(bool isEnabled)
    {
        if (characterController != null) characterController.enabled = isEnabled;
        if (_rigidbody != null) _rigidbody.isKinematic = !isEnabled;
    }
}