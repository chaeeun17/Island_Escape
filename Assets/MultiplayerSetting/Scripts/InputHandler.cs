using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System 네임스페이스

public class InputHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static InputHandler Instance;

    [Header("Local XR Origin Transforms")]
    public Transform _localHead;
    public Transform _localLeftHand;
    public Transform _localRightHand;

    // 매 프레임 캐싱할 변수들
    private Vector3 _hPos; private Quaternion _hRot;
    private Vector3 _lPos; private Quaternion _lRot;
    private Vector3 _rPos; private Quaternion _rRot;

    // Input Action Asset
    [SerializeField] private InputActionReference _leftHandTurnAction;
    [SerializeField] private InputActionReference _rightHandMoveAction;
    [SerializeField] private InputActionReference _jumpAction;

    // 매 프레임 수집된 로컬 입력 값을 임시로 보관할 변수
    private Vector2 _moveDirection;
    private Vector2 _turnDirection;
    private bool _jumpPressed;

    // 중요: 사용하기 전에 Action을 반드시 활성화(Enable)해줘야 합니다.
    private void OnEnable()
    {
        if (_leftHandTurnAction != null) _leftHandTurnAction.action.Enable();
        if (_rightHandMoveAction != null) _rightHandMoveAction.action.Enable();
        if (_jumpAction != null) _jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_leftHandTurnAction != null) _leftHandTurnAction.action.Disable();
        if (_rightHandMoveAction != null) _rightHandMoveAction.action.Disable();
        if (_jumpAction != null) _jumpAction.action.Disable();
    }

    // 싱글톤
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    private void Update()
    {
        if (_localHead == null) return;

        // XR Origin 기준의 '상대적'인 로컬 좌표를 가져와야 합니다.
        _hPos = _localHead.localPosition; _hRot = _localHead.localRotation;
        _lPos = _localLeftHand.localPosition; _lRot = _localLeftHand.localRotation;
        _rPos = _localRightHand.localPosition; _rRot = _localRightHand.localRotation;

        // 1. 유니티 Update() 프레임마다 New Input System의 값을 읽어와 변수에 저장합니다.
        Vector2 move = new Vector2();
        if (_rightHandMoveAction != null)
            move = _rightHandMoveAction.action.ReadValue<Vector2>();

        Vector2 turn = new Vector2();
        if (_leftHandTurnAction != null)
            turn = _leftHandTurnAction.action.ReadValue<Vector2>();

        // 최종 월드 기준 이동 방향 벡터 완성
        _moveDirection = move;
        _turnDirection = turn;

        // 이번 프레임에 점프 버튼이 눌렸다면 true 유지
        if (_jumpAction.action.WasPressedThisFrame())
        {
            _jumpPressed = true;
        }
    }

    // 2. Fusion 엔진이 "네트워크로 보낼 입력 데이터 줘!" 하고 틱마다 호출하는 메서드
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // 수집해둔 New Input System의 데이터를 Fusion 구조체에 담기
        data.direction = _moveDirection;

        // Snap Turn 전용 : 데드존(Deadzone) 임계값 설정 (조이스틱을 확실히 밀었을 때만 인식)
        float threshold = 0.7f;

        // 3. Vector2 값을 MyButtons 비트 플래그로 매핑
        if (_turnDirection.x < -threshold)
        {
            // 조이스틱을 왼쪽으로 밀었으면 TurnLeft 비트를 켬
            data.buttons.Set(MyButtons.TurnLeft, true);
        }
        else if (_turnDirection.x > threshold)
        {
            // 조이스틱을 오른쪽으로 밀었으면 TurnRight 비트를 켬
            data.buttons.Set(MyButtons.TurnRight, true);
        }

        if (_jumpPressed)
        {
            data.buttons.Set(MyButtons.Jump, true);
            _jumpPressed = false; // 수집 완료 후 초기화
        }

        // 트래킹 데이터를 Input 구조체에 세팅
        data.headPos = _hPos; data.headRot = _hRot;
        data.leftHandPos = _lPos; data.leftHandRot = _lRot;
        data.rightHandPos = _rPos; data.rightHandRot = _rRot;

        // Fusion 엔진에 입력 데이터 전달
        input.Set(data);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}