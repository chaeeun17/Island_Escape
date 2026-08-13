using UnityEngine;
using TMPro;
using Fusion;

public class NetworkPlayerStats : NetworkBehaviour
{
    // [내 캐릭터 전용 싱글톤] 내 조종 권한(HasInputAuthority)이 있는 캐릭터만 등록
    public static NetworkPlayerStats LocalInstance { get; private set; }

    [Header("Max Stats")]
    public int maxHunger = 100;
    public int maxThirst = 100;
    public int maxStamina = 100;

    // 🌟 [수정] Networked를 떼어내고 순수 C# 변수로 변경! (나만 관리)
    public int CurrentHunger { get; private set; }
    public int CurrentThirst { get; private set; }
    public int CurrentStamina { get; private set; }

    [Networked] private NetworkBool isFainted { get; set; }
    [Networked] private NetworkBool isSurvivalStarted { get; set; }
    [Networked] private NetworkBool isRevived { get; set; }

    [Header("Walk Animaton")]
    [SerializeField] private Animator animator;

    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int RevivedHash = Animator.StringToHash("Revived");

    // private bool isFainted = false;
    // private bool isSurvivalStarted = false;

    [Header("Decrease Rates (몇 초당 1씩 줄어들 것인가)")]
    public float hungerDecreaseInterval = 2.0f;
    public float thirstDecreaseInterval = 1.5f;
    public float staminaDecreaseInterval = 3.0f;

    [Header("TextMeshPro UI 컴포넌트 연결")]
    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI thirstText;
    public TextMeshProUGUI staminaText;

    [Header("UI 색상 경고 설정")]
    public int warningThreshold = 20;
    public Color normalColor = Color.white;
    public Color warningColor = Color.red; 

    private float hungerTimer;
    private float thirstTimer;
    private float staminaTimer;


    private WarningVignetteController wvc;

    public override void Spawned()
    {
        // 🌟 내 조종 권한(HasInputAuthority)이 있는 "나 자신"이 아니라면 이 스크립트의 작동을 끕니다.
        // (상대방 화면에서 내 스탯 타이머가 따로 도는 것을 방지)
        if (!HasInputAuthority)
        {
            enabled = false;
            return;
        }

        // 내 캐릭터일 경우에만 LocalInstance로 지정
        if (HasInputAuthority)
        {
            LocalInstance = this;
        }

        isSurvivalStarted = true;
        isFainted = false;

        // 캐릭터가 소환될 때 소유권(StateAuthority)을 가진 클라이언트가 최초 스탯 초기화
        if (HasStateAuthority)
        {
            CurrentHunger = maxHunger;
            CurrentThirst = maxThirst;
            CurrentStamina = maxStamina;
            //isFainted = false;
            //isSurvivalStarted = false;
            
        }

        Debug.Log("✅ [PlayerStats] Spawned 완료! 초기 스탯 100 설정됨.");

        // 튜토리얼 완료 이벤트 구독
        if (NetworkTutorialManager.Instance != null)
        {
            NetworkTutorialManager.Instance.OnTutorialCompleted += StartSurvivalTimer;
        }

        wvc = GameObject.Find("Global Volume").GetComponent<WarningVignetteController>();
    }

    public void StartSurvival()
    {
        // 내 캐릭터를 LocalInstance로 확실히 지정
        LocalInstance = this;

        // 초기 스탯 100 세팅
        CurrentHunger = maxHunger;
        CurrentThirst = maxThirst;
        CurrentStamina = maxStamina;
        isFainted = false;
        isSurvivalStarted = true; // 시작하자마자 생존 시작 (필요시 튜토리얼 완료 이벤트와 연결)
        //isSurvivalStarted = false;

        Debug.Log("✅ [PlayerStats] 로컬 플레이어 스탯 시스템 준비 완료! (100/100/100)");

        // 튜토리얼 완료 이벤트 구독
        if (SOSManager.Instance != null)
        {
            SOSManager.Instance.OnTutorialCompleted += StartSurvivalTimer;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (SOSManager.Instance != null)
        {
            SOSManager.Instance.OnTutorialCompleted -= StartSurvivalTimer;
        }
    }

    private void StartSurvivalTimer()
    {
        isSurvivalStarted = true;
        Debug.Log("[PlayerStats] 튜토리얼 완료! 생존 스탯 감소를 시작합니다.");
    }

    // 🌟 퓨전 네트워크 업데이트 대신 일반 유니티 Update 사용 (내 화면에서만 독립 실행)
    private void Update()
    {
        // UI 반영은 내 화면(HasInputAuthority)일 때만 처리
        if (HasInputAuthority)
        {
            UpdateStatTexts();


            if (CurrentHunger <= warningThreshold)
            {
                WarningVignetteOn(Stats.HUNGER);
            }
            else if (CurrentThirst <= warningThreshold)
            {
                WarningVignetteOn(Stats.THIRST);
            }
            else if (CurrentStamina <= warningThreshold)
            {
                WarningVignetteOn(Stats.STAMINA);
            }
            else
            {
                WarningVignetteOff();
            }
        }

        if (!isSurvivalStarted || isFainted) return;

        // 시간에 따른 스탯 감소
        HandleTimedDecrease();

        // 기절 조건 체크
        CheckFaintConditions();

        // UI 텍스트 갱신
        UpdateStatTexts();
    }

    private void HandleTimedDecrease()
    {
        float dt = Time.deltaTime;

        // 배고픔
        hungerTimer += dt;
        if (hungerTimer >= hungerDecreaseInterval)
        {
            if (CurrentHunger > 0) CurrentHunger--;
            hungerTimer = 0f;
        }

        // 갈증
        thirstTimer += dt;
        if (thirstTimer >= thirstDecreaseInterval)
        {
            if (CurrentThirst > 0) CurrentThirst--;
            thirstTimer = 0f;
        }

        // 스테미나
        staminaTimer += dt;
        if (staminaTimer >= staminaDecreaseInterval)
        {
            if (CurrentStamina > 0) CurrentStamina--;
            staminaTimer = 0f;
        }
    }

    private void CheckFaintConditions()
    {
        if (CurrentHunger <= 0 || CurrentThirst <= 0 || CurrentStamina <= 0)
        {
            Faint();
        }
    }

    private void Faint()
    {
        isFainted = true;
        Debug.LogError("🚨 [기절] 스탯 중 하나가 0이 되어 기절했습니다!");
    }

    private void UpdateStatTexts()
    {
        if (hungerText != null)
        {
            hungerText.text = $"{CurrentHunger}";
            hungerText.color = (CurrentHunger <= warningThreshold) ? warningColor : normalColor;
        }
        if (thirstText != null)
        {
            thirstText.text = $"{CurrentThirst}";
            thirstText.color = (CurrentThirst <= warningThreshold) ? warningColor : normalColor;
        }
        if (staminaText != null)
        {
            staminaText.text = $"{CurrentStamina}";
            staminaText.color = (CurrentStamina <= warningThreshold) ? warningColor : normalColor;
        }
    }

    // ==========================================
    // 🍎 로컬 스탯 회복 함수들 (권한 체크 없이 즉시 동작!)
    // ==========================================
    public void RestoreHunger(int amount)
    {
        if (isFainted) return;
        CurrentHunger = Mathf.Min(CurrentHunger + amount, maxHunger);
        UpdateStatTexts();
    }

    public void RestoreThirst(int amount)
    {
        if (isFainted) return;
        CurrentThirst = Mathf.Min(CurrentThirst + amount, maxThirst);
        UpdateStatTexts();
    }

    public void RestoreStamina(int amount)
    {
        if (isFainted) return;
        CurrentStamina = Mathf.Min(CurrentStamina + amount, maxStamina);
        UpdateStatTexts();
    }

    // 감소
    public void ReduceHunger(int amount)
    {
        if (isFainted) return;

        CurrentHunger = Mathf.Max(CurrentHunger - amount, 0);
        UpdateStatTexts();
    }

    // 🌟 3. 갈증 감소 (목마름)
    public void ReduceThirst(int amount)
    {
        if (isFainted) return;

        CurrentThirst = Mathf.Max(CurrentThirst - amount, 0);
        UpdateStatTexts();
    }

    // 🌟 4. 스태미나 감소 (달리기/작업 등)
    public void ReduceStamina(int amount)
    {
        if (isFainted) return;

        CurrentStamina = Mathf.Max(CurrentStamina - amount, 0);
        UpdateStatTexts();
    }
    
    //====================================
    // 추가 (Vignette)
    //====================================
    private void WarningVignetteOn(Stats stats)
    {
        wvc.EnableWarningVignette(stats);
    }

    private void WarningVignetteOff()
    {
        wvc.DisableWarningVignette();
    }

    //====================================
    // 추가 (Revive)
    //====================================
    public void Revive()
    {
        if (!isFainted) return;

        isFainted = false;
        isRevived = true;
        CurrentHunger = maxHunger / 2;
        CurrentStamina = maxStamina / 2;
        CurrentThirst = maxThirst / 2;

        if(animator != null)
        {
            animator.SetTrigger(RevivedHash);
        }
    }

    public bool IsPlayerFainted()
    {
        return isFainted;
    }

    //====================================
    // 추가 (Animation)
    //====================================
    public override void Render()
    {
        if (animator != null)
        {
            if (isFainted)
            {
                animator.SetTrigger(IsDeadHash);
            }
            else if (isRevived)
            {
                animator.SetTrigger(RevivedHash);
            }
        }
    }
}