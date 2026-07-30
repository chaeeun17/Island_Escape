using Fusion;
using System.Threading.Tasks; // �ʼ� �߰�
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("Network Avatar Parts (Must have NetworkTransform)")]
    [SerializeField] private Transform _netHead;
    [SerializeField] private Transform _netLeftHand;
    [SerializeField] private Transform _netRightHand;
    [SerializeField] private Transform _netBody;

    [SerializeField] private HandController _leftHandController;
    [SerializeField] private HandController _rightHandController;

    [SerializeField] private float _followAngle = 45f; // ������ ������� �����ϴ� ����
    [SerializeField] private float _rotateSpeed = 8f; // ȸ�� �ӵ�

    private CustomNetworkCharacterController _cc;
    [SerializeField] private float _jumpForce = 5f; // ���� ��
    [SerializeField] private float _snapTurnAngle = 45f; // �� ���� ȸ���� ����

    [Header("Walk Animaton")]
    [SerializeField] private Animator animator;
    [Networked] private NetworkBool IsWalking { get; set; }
    // Fusion 2 ����ȭ�� ���� ChangeDetector �� Animator Hash
    private ChangeDetector _changeDetector;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private NetworkButtons _previousButtons;

    private void Awake()
    {
        _cc = GetComponent<CustomNetworkCharacterController>();
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

            _leftHandController.Grip = data.leftGrip;
            _leftHandController.Trigger = data.leftTrigger;

            _rightHandController.Grip = data.rightGrip;
            _rightHandController.Trigger = data.rightTrigger;

            // 1. �Է� ��ȭ��(Pressed) ���
            // ���� ƽ�� �Է��� ������ �̹� ƽ�� '���� ����' ��ư�� �Ǻ��մϴ�.
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

    public override async void Spawned()
    {
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

                    // 3. ���� ��ǥ�� (0, 0, 0)���� ���߾� �� ĳ���� ��ġ�� ��Ȯ�� ��ġ��ŵ�ϴ�.
                    xrOrigin.transform.localPosition = Vector3.zero;
                    xrOrigin.transform.localRotation = Quaternion.identity;

                    InputHandler.Instance._localHead = xrOrigin.transform.Find("Camera Offset/Main Camera");
                    InputHandler.Instance._localLeftHand = xrOrigin.transform.Find("Camera Offset/Left Hand");
                    InputHandler.Instance._localRightHand = xrOrigin.transform.Find("Camera Offset/Right Hand");

                    Debug.Log("Player : " + transform.position);
                    Debug.Log("XR Origin : " + xrOrigin.transform.position);
                    Debug.Log("Main Camera : " + InputHandler.Instance._localHead.position);

                    // Debug.Log("XR Origin�� ���������� Player Prefab ������ ���յǾ����ϴ�.");
                }
                else
                {
                    // Debug.LogError("������ XR Origin�� ã�� �� �����ϴ�!");
                }

                //InitPlayerStats();
            }
        }
    }

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
    }
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
}