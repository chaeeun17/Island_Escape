using UnityEngine;
using TMPro;
using Fusion;

public class NetworkPlayerStats : NetworkBehaviour
{
    // [멀티 싱글톤] 내 조종 권한(HasInputAuthority)이 있는 내 캐릭터만 LocalInstance로 등록
    public static NetworkPlayerStats LocalInstance { get; private set; }

    [Header("Max Stats")]
    public int maxHunger = 100;
    public int maxThirst = 100;
    public int maxStamina = 100;

    // 포톤 네트워크를 통해 모든 플레이어에게 실시간 동기화
    [Networked] public int CurrentHunger { get; set; }
    [Networked] public int CurrentThirst { get; set; }
    [Networked] public int CurrentStamina { get; set; }

    [Networked] private NetworkBool isFainted { get; set; }
    [Networked] private NetworkBool isSurvivalStarted { get; set; }
    [Networked] private NetworkBool isRevived { get; set; }

    [Header("Walk Animaton")]
    [SerializeField] private Animator animator;

    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int RevivedHash = Animator.StringToHash("Revived");

    [Header("Decrease Rates (몇 초당 1씩 줄어들 것인가)")]
    public float hungerDecreaseInterval = 2.0f;
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

    // 각 스탯별 타이머 작동용 변수 (로컬 타이머)
    private float hungerTimer;
    private float thirstTimer;
    private float staminaTimer;


    private WarningVignetteController wvc;

    public override void Spawned()
    {
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

        // 수치 감소 플래그 ON
        isSurvivalStarted = true;
        isFainted = false;

        // 수치 초기화
        CurrentHunger = maxHunger;
        CurrentThirst = maxThirst;
        CurrentStamina = maxStamina;

        Debug.Log("🔥 [PlayerStats] 생존 시작! 스탯 감소가 정상 작동합니다.");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnTutorialCompleted -= StartSurvivalTimer;
        }
    }

    private void StartSurvivalTimer()
    {
        // 소유권을 가진 주체가 생존 타이머를 켭니다.
        if (HasStateAuthority)
        {
            isSurvivalStarted = true;
            Debug.Log("[PlayerStats] 튜토리얼 완료 이벤트 수신! 생존 스탯 감소를 시작합니다.");
        }
    }

    // 퓨전 전용 네트워크 업데이트 루프
    public override void FixedUpdateNetwork()
    {
        // 스탯 감소 및 상태 연산은 소유권(StateAuthority)을 가진 쪽에서만 진행합니다.
        if (!HasStateAuthority) return;

        // 이미 기절했다면 수치 감소나 기절 체크를 중단
        if (isFainted) return;

        if (isSurvivalStarted)
        {
            // 시간에 따른 감소 처리
            HandleTimedDecrease();

            // 수치가 0이 되었는지 체크
            CheckFaintConditions();
        }
    }

    // 렌더링 프레임(UI 업데이트용)
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
    }

    private void HandleTimedDecrease()
    {
        // 퓨전 델타 타임 사용
        float dt = Runner.DeltaTime;

        // 배고픔 타이머 계산
        hungerTimer += dt;
        if (hungerTimer >= hungerDecreaseInterval)
        {
            if (CurrentHunger > 0) CurrentHunger--;
            hungerTimer = 0f; // 타이머 초기화
        }

        // 갈증 타이머 계산
        thirstTimer += dt;
        if (thirstTimer >= thirstDecreaseInterval)
        {
            if (CurrentThirst > 0) CurrentThirst--;
            thirstTimer = 0f;
        }

        // 체력(스테미나) 타이머 계산
        staminaTimer += dt;
        if (staminaTimer >= staminaDecreaseInterval)
        {
            if (CurrentStamina > 0) CurrentStamina--;
            staminaTimer = 0f;
        }
    }

    // 기절 조건 체크
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

        string reason = "";
        if (CurrentHunger <= 0) reason = "배고픔 0";
        else if (CurrentThirst <= 0) reason = "갈증 0";
        else if (CurrentStamina <= 0) reason = "체력 0";

        Debug.LogError($"🚨 [기절] 사유: {reason}");
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
    // 🍎 스탯 회복 함수들
    // ==========================================
    public void RestoreHunger(int amount)
    {
        if (isFainted || !HasStateAuthority) return;
        CurrentHunger = Mathf.Min(CurrentHunger + amount, maxHunger);
    }

    public void RestoreThirst(int amount)
    {
        if (isFainted || !HasStateAuthority) return;
        CurrentThirst = Mathf.Min(CurrentThirst + amount, maxThirst);
    }

    public void RestoreStamina(int amount)
    {
        if (isFainted || !HasStateAuthority) return;
        CurrentStamina = Mathf.Min(CurrentStamina + amount, maxStamina);
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
