using UnityEngine;
using System.Collections;

public class SOSManager : MonoBehaviour
{
    public static SOSManager Instance { get; private set; }

    [Header("SOS를 구성하는 총 소켓 개수")]
    [SerializeField] private int totalRequiredSlots = 9;
    private int currentFilledSlots = 0;

    [Header("성공 UI")]
    [SerializeField] private GameObject successCutsceneObject;

    void Awake()
    {
        if (Instance == null) Instance = Instance = this;
        else Destroy(gameObject);
    }

    // 소켓에 돌이 들어왔을 때 호출될 함수
    public void SlotFilled()
    {
        currentFilledSlots++;
        Debug.Log($"[SOS] 돌 배치됨! ({currentFilledSlots} / {totalRequiredSlots})");

        if (currentFilledSlots >= totalRequiredSlots)
        {
            TriggerSOSSuccess();
        }
    }

    // 소켓에서 돌이 빠졌을 때 호출될 함수
    public void SlotEmptied()
    {
        currentFilledSlots = Mathf.Max(0, currentFilledSlots - 1);
    }

    private void TriggerSOSSuccess()
    {
        Debug.Log("🎉 SOS 신호 완성! 구조 시나리오를 시작합니다.");
        
        // ui 
        /*if (successCutsceneObject != null)
        {
            successCutsceneObject.SetActive(true);
        }*/
    }
}
