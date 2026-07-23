using UnityEngine;

public class WaterPurifier : MonoBehaviour, IInteractable
{
    private bool isWaterReady = true; // 대기 시간 후 true가 된다고 가정

    public string GetInteractText()
    {
        return isWaterReady ? "깨끗한 물 마시기" : "물이 정수되는 중...";
    }

    public void Interact()
    {
        if (isWaterReady)
        {
            Debug.Log("깨끗한 물을 마셨습니다! 갈증이 해소됩니다.");
            // 플레이어 갈증 스탯 상승 로직 추가
            isWaterReady = false; // 다시 정수 시작
        }
    }
}