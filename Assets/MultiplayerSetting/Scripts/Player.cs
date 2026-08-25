using Fusion;
using System.Threading.Tasks; // �ʼ� �߰�
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Player : NetworkBehaviour
{
    [Header("Network Avatar Parts (Must have NetworkTransform)")]
    [SerializeField] private Transform _netHead;
    [SerializeField] private Transform _netLeftHand;
    [SerializeField] private Transform _netRightHand;
    [SerializeField] private Transform _netBody;

    // 추가: 외부에서 손 Transform을 조회할 수 있는 프로퍼티
    public Transform NetLeftHand => _netLeftHand;
    public Transform NetRightHand => _netRightHand;

    [SerializeField] private Animator leftHandAnimator;
    [SerializeField] private Animator rightHandAnimator;

    [SerializeField] private float _followAngle = 45f; // ������ ������� �����ϴ� ����
    [SerializeField] private float _rotateSpeed = 8f; // ȸ�� �ӵ�

    private CustomNetworkCharacterController _cc;
    [SerializeField] private float _jumpForce = 5f; // ���� ��
    [SerializeField] private float _snapTurnAngle = 45f; // �� ���� ȸ���� ����

    [Header("Walk Animaton")]
    [SerializeField] private Animator animator;
    [Networked] private NetworkBool IsWalking { get; set; }

    [Networked] public NetworkBool IsEscaped { get; set; }

    [Networked] public float LeftGrab { get; set; }
    [Networked] public float LeftTrigger { get; set; }
    [Networked] public float RightGrab { get; set; }
    [Networked] public float RightTrigger { get; set; }

    // 수면 또는 컷신 중 플레이어 조작 차단 플래그
    private bool _isInputBlocked = false;

    // 🌟 XR 컨트롤러 및 인터랙터 전체 캐싱
    private ActionBasedController[] _localControllers;
    private XRBaseInteractor[] _localInteractors;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual[] _localLineVisuals;


    // Fusion 2 ����ȭ�� ���� ChangeDetector �� Animator Hash
    private ChangeDetector _changeDetector;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private NetworkButtons _previousButtons;

    private bool _controlLocked = false;

    [Networked, OnChangedRender(nameof(OnRescuedChanged))]
    public NetworkBool IsRescued { get; set; }
    [SerializeField] private VRFadeController vfc; // 인스펙터에서 연결
    [SerializeField] private Canvas successCanvas;

    [Networked, OnChangedRender(nameof(OnApproachNotifiedChanged))]
    private int ApproachNotifyToken { get; set; } // 값 변화 자체가 트리거 (bool 토글보다 중복 알림에 안전)
    [SerializeField] private NetworkUIManager uiManager; // 실제 UI 컴포넌트로 교체

    [Networked, OnChangedRender(nameof(OnEscapeFailedChanged))]
    public NetworkBool IsEscapeFailed { get; set; }

    private void OnRescuedChanged()
    {
        if (IsRescued && HasInputAuthority)
        {
            vfc.StartFadeToWhite();

            var uiManager = GetComponent<NetworkUIManager>(); // 실제 클래스명으로 교체 필요
            if (uiManager != null)
            {
                uiManager.ShowSuccessUI(); // 실제 함수명으로 교체 필요
            }
        }
    }

    private void OnEscapeFailedChanged()
    {
        if (IsEscapeFailed && HasInputAuthority)
        {
            vfc.StartFadeToWhite();

            var uiManager = GetComponent<NetworkUIManager>();
            if (uiManager != null)
            {
                uiManager.ShowFailUI(); // 실제 함수명으로 교체 필요
            }
        }
    }

    private void OnApproachNotifiedChanged()
    {
        if (HasInputAuthority && uiManager != null)
        {
            uiManager.ShowApproachNotice(); // 각 프리팹에 붙은 실제 UI 표시 함수 호출
        }
    }

    // 호스트가 호출할 공개 메서드
    public void NotifyHelicopterApproaching()
    {
        if (Object.HasStateAuthority)
        {
            ApproachNotifyToken++;
        }
    }

    private void Awake()
    {
        _cc = GetComponent<CustomNetworkCharacterController>();
    }

    public void SetInputEnabled(bool isEnabled)
    {
        _isInputBlocked = !isEnabled;

        if (_isInputBlocked)
        {
            IsWalking = false;
        }

        // 1. 레이저 및 잡기 인터랙터 켜기/끄기
        if (_localInteractors != null)
        {
            foreach (var interactor in _localInteractors)
            {
                if (interactor != null) interactor.enabled = isEnabled;
            }
        }

        // 2. 레이저 시각선(Line Visual) 켜기/끄기
        if (_localLineVisuals != null)
        {
            foreach (var line in _localLineVisuals)
            {
                if (line != null) line.enabled = isEnabled;
            }
        }

        Debug.Log($"🎮 [Player] 입력 활성화 상태 변경: {isEnabled}");
    }

    public override void FixedUpdateNetwork()
    {
        // �� ƽ���� �Է��� �����ϴ��� Ȯ��
        if (GetInput(out NetworkInputData data))
        {
            // 0. ���޹��� ���� Ʈ��ŷ ��ǥ�� ��Ʈ��ũ �ƹ�Ÿ �ڽĵ鿡�� �״�� �����մϴ�.
            _netHead.localPosition = data.headPos;
            _netHead.localRotation = data.headRot;

            _netLeftHand.localPosition = data.leftHandPos;
            _netLeftHand.localRotation = data.leftHandRot;

            _netRightHand.localPosition = data.rightHandPos;
            _netRightHand.localRotation = data.rightHandRot;

            LeftGrab = data.leftGrip;
            LeftTrigger = data.leftTrigger;

            RightGrab = data.rightGrip;
            RightTrigger = data.rightTrigger;

            if (_isInputBlocked)
            {
                IsWalking = false;
                _previousButtons = data.buttons;
                return;
            }

            // 1. �Է� ��ȭ��(Pressed) ���
            // ���� ƽ�� �Է��� ������ �̹� ƽ�� '���� ����' ��ư�� �Ǻ��մϴ�.
            if (IsEscaped)
            {
                // 🌟 사다리에 매달린 동안은 CC 로직(이동/입력 처리) 전체를 건너뜀
                //    위치는 PlayerHelicopterRider가 전담
                IsWalking = false;
                return;
            }

            // 🌟 조작 잠금 중이면 이동/회전/점프 입력은 여기서 차단
            if (_controlLocked)
            {
                IsWalking = false;
                return;
            }

            var pressed = data.buttons.GetPressed(_previousButtons);
            _previousButtons = data.buttons;

            Vector3 pivot = _netHead.position;
            // 2. ���� �� ó��
            // ���̽�ƽ�� �����̳� ���������� �� �о��� �� �� �� ���� ȸ���մϴ�.
            if (pressed.IsSet(MyButtons.TurnLeft))
            {
                _cc.Rotate(-_snapTurnAngle);
            }
            else if (pressed.IsSet(MyButtons.TurnRight))
            {
                _cc.Rotate(_snapTurnAngle);
            }
            Vector3 offset = pivot - _netHead.position;
            _cc.Move(offset);

            // data.direction���� ���̽�ƽ�� ���� �� (X=�¿�, Z=�յ�)�� ����ֽ��ϴ�.
            Vector3 rawInput = new Vector3(data.direction.x, 0, data.direction.y);
            Vector3 moveDir = Vector3.zero;

            if (rawInput.sqrMagnitude > 0.001f)
            {
                // ���� �Ӹ� ���� ���� ���� ����
                Vector3 forward = _netHead.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 right = _netHead.right;
                right.y = 0;
                right.Normalize();

                // ���� ���̽�ƽ X(�¿�)�� Z(�յ�)�� �Ӹ� ������ ���� ���� ���� �̵� ���� �ϼ�!
                moveDir = (forward * rawInput.z) + (right * rawInput.x);

                // �Է��� �������Ƿ� Walking ���� true
                IsWalking = true;
            }
            else
            {
                IsWalking = false;
            }

            // ������ �̵� ������ NCC�� ����
            _cc.Move(moveDir);

            // 3. ���� (Jump) ó��
            // �̹� ƽ�� "Jump" ��ư�� ���� ���Ȱ�, ĳ���Ͱ� ���� �ִٸ�
            if (pressed.IsSet(MyButtons.Jump) && _cc.Grounded)
            {
                // NCC���� ���� ������ �����ϴ�.
                _cc.Jump(ignoreGrounded: false, overrideImpulse: _jumpForce);
            }
        }
        else
        {
            IsWalking = false;
        }
    }

    private void Update()
    {
        leftHandAnimator.SetFloat("Grip", LeftGrab);
        leftHandAnimator.SetFloat("Trigger", LeftTrigger);

        rightHandAnimator.SetFloat("Grip", RightGrab);
        rightHandAnimator.SetFloat("Trigger", RightTrigger);
    }

    public override async void Spawned()
    {
        // Debug.Log($"[Player Debug] HasInputAuthority={HasInputAuthority}, HasStateAuthority={HasStateAuthority}, InputAuthority={Object.InputAuthority}, Runner.LocalPlayer={Runner.LocalPlayer}");

        // 1. ���� ���(��: 100ms = 0.1��) ����Ͽ� Fusion�� ����(Authority)�� ������ ������ �ð��� �ݴϴ�.
        await Task.Delay(100);
        // �� �ƹ�Ÿ ������Ʈ�� �� ��ǻ�� ��ǲ ������ ���� '���� ��¥ �ƹ�Ÿ'���?
        if (HasInputAuthority)
        {
            // ��(���� �÷��̾�)�� �����ϴ� ĳ������ ���� ����
            if (HasInputAuthority)
            {
                // 1. ���� �ִ� XR Origin ������Ʈ�� ã���ϴ�.
                // (XR Origin ������Ʈ�� Unity�� ���� XR Origin (XR Interaction Toolkit) ������Ʈ ����)
                GameObject xrOrigin = GameObject.FindWithTag("XROrigin");

                if (xrOrigin != null)
                {
                    // 2. XR Origin�� �θ� �� Player Prefab(�ڽ�)���� �����մϴ�.
                    xrOrigin.transform.SetParent(transform);

                    // 3. ���� ��ǥ�� (0, 0, 0)���� ���߾� �� ĳ���� ��ġ�� ��Ȯ�� ��ġ��ŵ�ϴ
                    Transform mouth = _netHead.Find("MouthZone");
                    xrOrigin.transform.localPosition = new Vector3(0, mouth.transform.localPosition.y, 0);
                    xrOrigin.transform.localRotation = Quaternion.identity;

                    InputHandler.Instance._localHead = xrOrigin.transform.Find("Camera Offset/Main Camera");
                    InputHandler.Instance._localLeftHand = xrOrigin.transform.Find("Camera Offset/Left Hand");
                    InputHandler.Instance._localRightHand = xrOrigin.transform.Find("Camera Offset/Right Hand");

                    Debug.Log("Player : " + transform.position);
                    Debug.Log("XR Origin : " + xrOrigin.transform.position);
                    Debug.Log("Main Camera : " + InputHandler.Instance._localHead.position);
                    // Debug.Log("XR Origin�� ���������� Player Prefab ������ ���յǾ����ϴ�.");

                    _localControllers = xrOrigin.GetComponentsInChildren<ActionBasedController>(true);
                    _localInteractors = xrOrigin.GetComponentsInChildren<XRBaseInteractor>(true);
                    _localLineVisuals = xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>(true);
                }
                else
                {
                    // Debug.LogError("������ XR Origin�� ã�� �� �����ϴ�!");
                }

                //InitPlayerStats();
            }
        }
    }

/*
    private void InitPlayerStats()
    {
        NetworkPlayerStats stats = GetComponent<NetworkPlayerStats>();
        if (stats != null)
        {
            // PlayerStats 스크립트에 만든 StartSurvival() 함수 호출!
            stats.StartSurvival();
            Debug.Log("✅ [Player] PlayerStats 생존 스탯 감소 시작 함수 호출 성공!");
        }
        else
        {
            Debug.LogWarning("⚠️ [Player] 이 프리팹에 PlayerStats 컴포넌트가 없습니다.");
        }
    }*/ 
    
    public override void Render()
    {
        UpdateBodyRotation();

        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, IsWalking);
        }
    }

    private void UpdateBodyRotation()
    {
        if (_netBody == null || _netHead == null)
            return;

        // �Ӹ��� ���� ���⸸ ���
        Vector3 headForward = _netHead.forward;
        headForward.y = 0f;

        if (headForward.sqrMagnitude < 0.0001f)
            return;

        headForward.Normalize();

        // ������ ���� ���� ����
        Vector3 bodyForward = _netBody.forward;
        bodyForward.y = 0f;
        bodyForward.Normalize();

        // ����� �Ӹ��� Y�� ���� ����
        float angle = Vector3.SignedAngle(bodyForward, headForward, Vector3.up);

        // ���� ���� �̻� ���̳��� ���� ȸ��
        if (Mathf.Abs(angle) > _followAngle)
        {
            Quaternion targetRotation = Quaternion.LookRotation(headForward, Vector3.up);

            _netBody.rotation = Quaternion.Slerp(
                _netBody.rotation,
                targetRotation,
                _rotateSpeed * Time.deltaTime);
        }

        _netBody.position = _netHead.position;
    }

    // 추가 - NetHead의 NetworkTransform on/off
    public void SetNetworkTransformHead(bool on)
    {
        _netHead.GetComponent<NetworkTransform>().enabled = on;
    }

    public void SyncControllerPositionToTransform()
    {
        if (_cc != null)
        {
            _cc.Teleport(transform.position); // Fusion NetworkCharacterController 기반이면 보통 제공됨
        }
    }
    public void SetControlLocked(bool locked)
    {
        _controlLocked = locked;
    }

    public void TeleportTo(Vector3 targetPos, Quaternion targetRot)
    {
        // 1. CustomNetworkCharacterController 텔레포트 호출 (핵심!)
        if (_cc != null)
        {
            _cc.Teleport(targetPos);
        }
        else
        {
            transform.position = targetPos;
        }

        transform.rotation = targetRot;

        // 2. 자식 XR Origin의 로컬 위치를 (0,0,0)으로 재정렬
        GameObject xrOrigin = transform.Find("XR Origin")?.gameObject;
        if (xrOrigin != null)
        {
            xrOrigin.transform.localPosition = Vector3.zero;
            xrOrigin.transform.localRotation = Quaternion.identity;
        }

        Debug.Log($"🚀 [Player {Object.InputAuthority.PlayerId}] CustomNetworkCharacterController 텔레포트 완료 -> {targetPos}");
    }

    /*
    private void OnRescuedChanged()
    {
        if (IsRescued && HasInputAuthority)
        {
            vfc.StartFadeToWhite(); // 탈출 성공 연출 (필요에 따라 다른 함수로 교체)
            successCanvas.enabled = true;
        }
    }
    */
}