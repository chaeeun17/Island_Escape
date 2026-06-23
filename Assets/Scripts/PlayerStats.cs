using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Max Stats")]
    public float maxHunger = 100f;
    public float maxThirst = 100f;
    public float maxStamina = 100f;

    [Header("Current Stats (인스펙터에서 실시간 수정 가능)")]
    [Range(0f, 100f)] public float currentHunger = 100f;
    [Range(0f, 100f)] public float currentThirst = 100f;
    [Range(0f, 100f)] public float currentStamina = 100f;

    [Header("Decrease Rates (초당 감소량)")]
    public float hungerDecreaseRate = 0.5f; 
    public float thirstDecreaseRate = 0.8f; 
    public float staminaDecreaseRate = 0.2f; 

    private bool isFainted = false; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 처음 시작할 때만 최대치로 초기화 
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        currentStamina = maxStamina;
    }

    void Update()
    {
        // 이미 기절했다면 수치 감소나 강제 기절 체크를 중단합니다.
        if (isFainted) return;

        // 인스펙터에서 유저가 임의로 값을 바꾼 상태를 반영하기 위해 
        // 실시간으로 최대/최소 범위(0 ~ Max) 밖으로 나가지 않게 클램핑만 해줍니다.
        ClampInspectorValues();

        // 시간에 따른 감소 처리
        DecreaseStatsOverTime();

        // 수치가 0이 되었는지 체크
        CheckFaintConditions();
    }

    private void ClampInspectorValues()
    {
        // 인스펙터에서 마우스로 과도하게 수치를 올려도 최대치를 넘지 않도록 제한합니다.
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
        currentThirst = Mathf.Clamp(currentThirst, 0f, maxThirst);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    private void DecreaseStatsOverTime()
    {
        // 매 프레임 인스펙터에 있는 '현재 값'에서 감소율을 뺍니다.
        currentHunger -= hungerDecreaseRate * Time.deltaTime;
        currentThirst -= thirstDecreaseRate * Time.deltaTime;
        currentStamina -= staminaDecreaseRate * Time.deltaTime;
    }

    private void CheckFaintConditions()
    {
        if (currentHunger <= 0f || currentThirst <= 0f || currentStamina <= 0f)
        {
            Faint();
        }
    }

    private void Faint()
    {
        isFainted = true;
        
        string reason = "";
        if (currentHunger <= 0f) reason = "아사(배고픔 0)";
        else if (currentThirst <= 0f) reason = "탈수(갈증 0)";
        else if (currentStamina <= 0f) reason = "과로(체력 0)";

        Debug.LogError($"🚨 [기절 시스템 작동] 사유: {reason}");
    }

    // 기절 상태를 풀고 다시 테스트하고 싶을 때 인스펙터에서 우클릭으로 실행
    [ContextMenu("Reset Faint State (기절 해제 및 스탯 부활)")]
    public void ResetStats()
    {
        isFainted = false;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        currentStamina = maxStamina;
        Debug.Log("☀️ 플레이어가 부활했습니다! 스탯이 다시 감소합니다.");
    }

    // 회복 함수
    public void RestoreHunger(float amount)
    {
        if (isFainted) return;
        currentHunger = Mathf.Min(currentHunger + amount, maxHunger);
    }

    public void RestoreThirst(float amount)
    {
        if (isFainted) return;
        currentThirst = Mathf.Min(currentThirst + amount, maxThirst);
    }

    public void RestoreStamina(float amount)
    {
        if (isFainted) return;
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
    }
}
