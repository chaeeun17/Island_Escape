using Fusion;
using System.Threading.Tasks; // 필수 추가
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("Network Avatar Parts (Must have NetworkTransform)")]
    [SerializeField] private Transform _netHead;
    [SerializeField] private Transform _netLeftHand;
    [SerializeField] private Transform _netRightHand;
    [SerializeField] private Transform _netBody;

    [SerializeField] private Animator leftHandAnimator;
    [SerializeField] private Animator rightHandAnimator;

    [SerializeField] private float _followAngle = 45f; // 몸통이 따라오기 시작하는 각도
    [SerializeField] private float _rotateSpeed = 8f; // 회전 속도

    private CustomNetworkCharacterController _cc;
    [SerializeField] private float _jumpForce = 5f; // 점프 힘
    [SerializeField] private float _snapTurnAngle = 45f; // 한 번에 회전할 각도

    [Header("Walk Animaton")]
    [SerializeField] private Animator animator;
    [Networked] private NetworkBool IsWalking { get; set; }
    [Networked] public float LeftGrab { get; set; }
    [Networked] public float LeftTrigger { get; set; }
    [Networked] public float RightGrab { get; set; }
    [Networked] public float RightTrigger { get; set; }

    // Fusion 2 최적화를 위한 ChangeDetector 및 Animator Hash
    private ChangeDetector _changeDetector;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private NetworkButtons _previousButtons;

    private void Awake()
    {
        _cc = GetComponent<CustomNetworkCharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        // 각 틱마다 입력이 존재하는지 확인
        if (GetInput(out NetworkInputData data))
        {
            // 0. 전달받은 로컬 트래킹 좌표를 네트워크 아바타 자식들에게 그대로 대입합니다.
            _netHead.localPosition = data.headPos;
            _netHead.localRotation = data.headRot;

            _netLeftHand.localPosition = data.leftHandPos;
            _netLeftHand.localRotation = data.leftHandRot;

            _netRightHand.localPosition = data.rightHandPos;
            _netRightHand.localRotation = data.rightHandRot;

            LeftGrab = data.leftGrab;
            LeftTrigger = data.leftTrigger;

            RightGrab = data.rightGrab;
            RightTrigger = data.rightTrigger;

            // 1. 입력 변화량(Pressed) 계산
            // 이전 틱의 입력을 가져와 이번 틱에 '새로 눌린' 버튼을 판별합니다.
            var pressed = data.buttons.GetPressed(_previousButtons);
            _previousButtons = data.buttons;

            Vector3 pivot = _netHead.position;
            // 2. 스냅 턴 처리
            // 조이스틱을 왼쪽이나 오른쪽으로 툭 밀었을 때 딱 한 번만 회전합니다.
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

            // data.direction에는 조이스틱의 순수 값 (X=좌우, Z=앞뒤)이 들어있습니다.
            Vector3 rawInput = new Vector3(data.direction.x, 0, data.direction.y);
            Vector3 moveDir = Vector3.zero;

            if (rawInput.sqrMagnitude > 0.001f)
            {
                // 현재 머리 방향 기준 벡터 추출
                Vector3 forward = _netHead.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 right = _netHead.right;
                right.y = 0;
                right.Normalize();

                // 순수 조이스틱 X(좌우)와 Z(앞뒤)에 머리 방향을 입혀 최종 월드 이동 방향 완성!
                moveDir = (forward * rawInput.z) + (right * rawInput.x);

                // 입력이 들어왔으므로 Walking 상태 true
                IsWalking = true;
            }
            else
            {
                IsWalking = false;
            }

            // 수정한 이동 방향을 NCC에 전달
            _cc.Move(moveDir);

            // 3. 점프 (Jump) 처리
            // 이번 틱에 "Jump" 버튼이 새로 눌렸고, 캐릭터가 땅에 있다면
            if (pressed.IsSet(MyButtons.Jump) && _cc.Grounded)
            {
                // NCC에게 점프 명령을 내립니다.
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
        // 1. 아주 잠깐(예: 100ms = 0.1초) 대기하여 Fusion이 권한(Authority)을 완전히 세팅할 시간을 줍니다.
        await Task.Delay(100);
        // 이 아바타 오브젝트가 내 컴퓨터 인풋 권한을 가진 '나의 진짜 아바타'라면?
        if (HasInputAuthority)
        {
            // 내(로컬 플레이어)가 조종하는 캐릭터일 때만 실행
            if (HasInputAuthority)
            {
                // 1. 씬에 있는 XR Origin 오브젝트를 찾습니다.
                // (XR Origin 컴포넌트나 Unity의 신형 XR Origin (XR Interaction Toolkit) 컴포넌트 기준)
                GameObject xrOrigin = GameObject.FindWithTag("XROrigin");

                if (xrOrigin != null)
                {
                    // 2. XR Origin의 부모를 이 Player Prefab(자신)으로 설정합니다.
                    xrOrigin.transform.SetParent(transform);

                    // 3. 로컬 좌표를 (0, 0, 0)으로 맞추어 내 캐릭터 위치와 정확히 일치시킵니다.
                    xrOrigin.transform.localPosition = Vector3.zero;
                    xrOrigin.transform.localRotation = Quaternion.identity;

                    InputHandler.Instance._localHead = xrOrigin.transform.Find("Camera Offset/Main Camera");
                    InputHandler.Instance._localLeftHand = xrOrigin.transform.Find("Camera Offset/Left Hand");
                    InputHandler.Instance._localRightHand = xrOrigin.transform.Find("Camera Offset/Right Hand");

                    Debug.Log("Player : " + transform.position);
                    Debug.Log("XR Origin : " + xrOrigin.transform.position);
                    Debug.Log("Main Camera : " + InputHandler.Instance._localHead.position);

                    // Debug.Log("XR Origin이 성공적으로 Player Prefab 하위로 결합되었습니다.");
                }
                else
                {
                    // Debug.LogError("씬에서 XR Origin을 찾을 수 없습니다!");
                }
            }
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

        // 머리의 수평 방향만 사용
        Vector3 headForward = _netHead.forward;
        headForward.y = 0f;

        if (headForward.sqrMagnitude < 0.0001f)
            return;

        headForward.Normalize();

        // 몸통의 현재 수평 방향
        Vector3 bodyForward = _netBody.forward;
        bodyForward.y = 0f;
        bodyForward.Normalize();

        // 몸통과 머리의 Y축 각도 차이
        float angle = Vector3.SignedAngle(bodyForward, headForward, Vector3.up);

        // 일정 각도 이상 차이나면 몸통 회전
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