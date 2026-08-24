using UnityEngine;
using TMPro;

public class RoomInfoDisplay : MonoBehaviour
{
    [Header("방 정보를 띄울 TextMeshPro UI")]
    [SerializeField] private TMP_Text roomNameText;     // 방 이름/번호 텍스트
    [SerializeField] private TMP_Text roomPasswordText; // 비밀번호 텍스트 (선택)

    private void Start()
    {
        UpdateRoomInfoUI();
    }

    public void UpdateRoomInfoUI()
    {
        string roomName = PlayerSpawner.CurrentRoomName;
        string roomPw = PlayerSpawner.CurrentRoomPassword;

        // 1. 방 이름 텍스트가 연결된 경우
        if (roomNameText != null)
        {
            roomNameText.text = $"ROOM ID: {roomName}";
        }

        // 2. 비밀번호 텍스트가 따로 연결된 경우
        if (roomPasswordText != null)
        {
            roomPasswordText.text = $"PASSWORD: {roomPw}";
        }

        Debug.Log($"📋 [RoomInfoDisplay] UI 갱신 완료 -> Room: {roomName}, PW: {roomPw}");
    }
}