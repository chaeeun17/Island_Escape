using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Max Stats")]
    public int maxHunger = 100;
    public int maxThirst = 100;
    public int maxStamina = 100;

    [Header("Current Stats (인스펙터에서 실시간 수정 가능)")]
    [Range(0f, 100f)] public int currentHunger = 100;
    [Range(0f, 100f)] public int currentThirst = 100;
    [Range(0f, 100f)] public int currentStamina = 100;

    [Header("Decrease Rates (몇 초당 1씩 줄어들 것인가)")]
    public float hungerDecreaseInterval=2.0f;
    public float thirstDecreaseInterval = 1.5f;
    public float staminaDecreaseInterval = 3.0f;

    [Header("TextMeshPro UI 컴포넌트 연결")]
    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI thirstText;
    public TextMeshProUGUI staminaText;

    [Header("UI 색상 경고 설정")]
    public int warningThreshold = 20; // 20 이하로 떨어지면 빨간색으로 변경
    public Color normalColor = Color.white; // 평소 색상
    public Color warningColor = Color.red;  // 경고 색상 

    // 각 스탯별 타이머 작동용 변수
    private float hungerTimer;
    private float thirstTimer;
    private float staminaTimer;

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
        // 이미 기절했다면 수치 감소나 기절 체크를 중단
        if (isFainted) return;

        // 인스펙터에서 실시간 수정을 방어하기 위한 제한
        ClampInspectorValues();

        // 시간에 따른 감소 처리
        HandleTimedDecrease();

        // 수치가 0이 되었는지 체크
        CheckFaintConditions();

        // ui 반영
        UpdateStatTexts();
    }

    private void ClampInspectorValues()
    {
        // 인스펙터에서 마우스로 과도하게 수치를 올려도 최대치를 넘지 않도록 제한
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    private void HandleTimedDecrease()
    {
        // 배고픔 타이머 계산
        hungerTimer += Time.deltaTime;
        if (hungerTimer >= hungerDecreaseInterval)
        {
            if (currentHunger > 0) currentHunger--;
            hungerTimer = 0f; // 타이머 초기화
        }

        // 갈증 타이머 계산
        thirstTimer += Time.deltaTime;
        if (thirstTimer >= thirstDecreaseInterval)
        {
            if (currentThirst > 0) currentThirst--;
            thirstTimer = 0f;
        }

        // 체력 타이머 계산
        staminaTimer += Time.deltaTime;
        if (staminaTimer >= staminaDecreaseInterval)
        {
            if (currentStamina > 0) currentStamina--;
            staminaTimer = 0f;
        }
    }

    // 기절 조건 체크
    private void CheckFaintConditions()
    {
        if (currentHunger <= 0 || currentThirst <= 0 || currentStamina <= 0)
        {
            Faint();
        }
    }

    private void Faint()
    {
        isFainted = true;
        
        string reason = "";
        if (currentHunger <= 0) reason = "배고픔 0";
        else if (currentThirst <= 0) reason = "갈증 0";
        else if (currentStamina <= 0) reason = "체력 0";

        Debug.LogError($"🚨 [기절] 사유: {reason}");
    }

    private void UpdateStatTexts()
    {
        if (hungerText != null)
        {
            hungerText.text = $"{currentHunger}";
            hungerText.color = (currentHunger <= warningThreshold) ? warningColor : normalColor;
        }
        if (thirstText != null)
        {
            thirstText.text = $"{currentThirst}";
            thirstText.color = (currentThirst <= warningThreshold) ? warningColor : normalColor;
        }
        if (staminaText != null)
        {
            staminaText.text = $"{currentStamina}";
            staminaText.color = (currentStamina <= warningThreshold) ? warningColor : normalColor;
        }
            
    }


    // 회복 함수
    public void RestoreHunger(int amount)
    {
        if (isFainted) return;
        currentHunger = Mathf.Min(currentHunger + amount, maxHunger);
    }

    public void RestoreThirst(int amount)
    {
        if (isFainted) return;
        currentThirst = Mathf.Min(currentThirst + amount, maxThirst);
    }

    public void RestoreStamina(int amount)
    {
        if (isFainted) return;
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
    }
}
