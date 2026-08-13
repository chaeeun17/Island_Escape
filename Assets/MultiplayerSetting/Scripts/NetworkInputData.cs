using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 direction; // 키보드 WASD나 패드 스틱 값
    // 여러 버튼 상태를 효율적으로 담을 수 있는 Fusion 전용 구조체입니다.
    public NetworkButtons buttons;

    // VR 트래킹 데이터 (XR Origin 기준의 localPosition / localRotation)
    public Vector3 headPos;
    public Quaternion headRot;

    public Vector3 leftHandPos;
    public Quaternion leftHandRot;

    public Vector3 rightHandPos;
    public Quaternion rightHandRot;

    public float leftGrab;
    public float leftTrigger;

    public float rightGrab;
    public float rightTrigger;
}

// 어떤 버튼인지 구분하기 위한 비트 플래그용 열거형
public enum MyButtons
{
    Jump = 0,
    TurnLeft = 1,
    TurnRight = 2,
}