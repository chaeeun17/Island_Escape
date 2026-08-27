using UnityEngine;
using System.Collections;
using TMPro;
using Fusion;

public class NetworkPlayerStats : NetworkBehaviour
{
    public static NetworkPlayerStats LocalInstance { get; private set; }

    [Header("Max Stats")]
    public int maxHunger = 100;
    public int maxThirst = 100;
    public int maxStamina = 100;

    public int CurrentHunger { get; private set; }
    public int CurrentThirst { get; private set; }
    public int CurrentStamina { get; private set; }

    [Networked, OnChangedRender(nameof(OnFaintedChanged))]
    private NetworkBool isFainted { get; set; }
    [Networked, OnChangedRender(nameof(OnRevivedChanged))]
    private NetworkBool isRevived { get; set; }
    [Networked] private NetworkBool isSurvivalStarted { get; set; }

    [Header("Faint/Revive Animaton")]
    [SerializeField] private Animator animator;
    [SerializeField] private float reviveAnimationDuration = 2.0f;
    private Coroutine controlLockCoroutine;

    [Networked, OnChangedRender(nameof(OnVisibilityChanged))]
    public NetworkBool IsVisible { get; set; } = true;
    [SerializeField] private Renderer animatedHead;
    [SerializeField] private Renderer[] networkedRightHand;
    [SerializeField] private Renderer[] networkedLeftHand;
    [SerializeField] private Canvas stateUI;
    [SerializeField] private Renderer networkedHead;
    [SerializeField] private VRFadeController vfc;

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
    public int warningThreshold = 20;
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;

    private float hungerTimer;
    private float thirstTimer;
    private float staminaTimer;

    private bool isVignetteOn = false;
    private WarningVignetteController wvc;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            LocalInstance = this;

            CurrentHunger = maxHunger;
            CurrentThirst = maxThirst;
            CurrentStamina = maxStamina;

            SetFainted(false);
        }

        Debug.Log("✅ [PlayerStats] Spawned 완료!");

        if (SOSManager.Instance != null)
        {
            SOSManager.Instance.OnTutorialCompleted += StartSurvivalTimer;
        }

        GameObject globalVolume = GameObject.Find("Global Volume");
        if (globalVolume != null)
        {
            wvc = globalVolume.GetComponent<WarningVignetteController>();
        }
    }

    public void StartSurvival()
    {
        LocalInstance = this;

        CurrentHunger = maxHunger;
        CurrentThirst = maxThirst;
        CurrentStamina = maxStamina;

        SetFainted(false);
        SetSurvivalStarted(true);

        Debug.Log("✅ [PlayerStats] 로컬 플레이어 스탯 시스템 준비 완료! (100/100/100)");

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
        SetSurvivalStarted(true);
        Debug.Log("[PlayerStats] 튜토리얼 완료! 생존 스탯 감소를 시작합니다.");
    }

    // ==========================================
    // [Networked] 프로퍼티 쓰기 전용 헬퍼
    // ==========================================
    private void SetSurvivalStarted(bool value)
    {
        if (Object.HasStateAuthority)
        {
            isSurvivalStarted = value;
        }
        else
        {
            RPC_RequestSetSurvivalStarted(value);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSetSurvivalStarted(bool value)
    {
        isSurvivalStarted = value;
    }

    private void SetFainted(bool value)
    {
        if (Object.HasStateAuthority)
        {
            isFainted = value;
        }
        else
        {
            RPC_RequestSetFainted(value);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSetFainted(bool value)
    {
        isFainted = value;
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        if (!isSurvivalStarted || isFainted) return;

        UpdateStatTexts();

        if (wvc != null)
        {
            if (!isVignetteOn)
            {
                if (CurrentHunger <= warningThreshold)
                {
                    WarningVignetteOn(Stats.HUNGER);
                    isVignetteOn = true;
                }
                else if (CurrentThirst <= warningThreshold)
                {
                    WarningVignetteOn(Stats.THIRST);
                    isVignetteOn = true;
                }
                else if (CurrentStamina <= warningThreshold)
                {
                    WarningVignetteOn(Stats.STAMINA);
                    isVignetteOn = true;
                }
            }
            else
            {
                if (CurrentHunger > warningThreshold && CurrentThirst > warningThreshold && CurrentStamina > warningThreshold)
                {
                    WarningVignetteOff();
                    isVignetteOn = false;
                }
            }
        }

        HandleTimedDecrease();
        CheckFaintConditions();
        UpdateStatTexts();
    }

    private void HandleTimedDecrease()
    {
        float dt = Time.deltaTime;

        hungerTimer += dt;
        if (hungerTimer >= hungerDecreaseInterval)
        {
            if (CurrentHunger > 0) CurrentHunger--;
            hungerTimer = 0f;
        }

        thirstTimer += dt;
        if (thirstTimer >= thirstDecreaseInterval)
        {
            if (CurrentThirst > 0) CurrentThirst--;
            thirstTimer = 0f;
        }

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
        SetFainted(true);
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

    public void ReduceHunger(int amount)
    {
        if (isFainted) return;
        CurrentHunger = Mathf.Max(CurrentHunger - amount, 0);
        UpdateStatTexts();
    }

    public void ReduceThirst(int amount)
    {
        if (isFainted) return;
        CurrentThirst = Mathf.Max(CurrentThirst - amount, 0);
        UpdateStatTexts();
    }

    public void ReduceStamina(int amount)
    {
        if (isFainted) return;
        CurrentStamina = Mathf.Max(CurrentStamina - amount, 0);
        UpdateStatTexts();
    }

    private void WarningVignetteOn(Stats stats)
    {
        if (wvc != null) wvc.EnableWarningVignette(stats);
    }

    private void WarningVignetteOff()
    {
        if (wvc != null) wvc.DisableWarningVignette();
    }

    public void Revive()
    {
        if (!isFainted) return;
        if (isRevived) return;

        if (Object.HasStateAuthority)
        {
            DoRevive();
        }
        else
        {
            RPC_RequestRevive();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRevive()
    {
        if (!isFainted) return;
        if (isRevived) return;
        DoRevive();
    }

    private void DoRevive()
    {
        isFainted = false;
        isRevived = true;

        // 🌟 CurrentHunger/Thirst/Stamina는 [Networked]가 아니라 각 피어(특히 InputAuthority를
        //    가진 당사자)에 로컬로만 존재하는 값이다. DoRevive()는 항상 Host(StateAuthority)에서
        //    실행되므로, 여기서 그냥 대입하면 Host 자신이 들고 있는 로컬 사본만 바뀌고 정작
        //    매 프레임 CheckFaintConditions()를 도는 당사자 클라이언트의 값은 그대로 0에 남아있어
        //    부활하자마자 다시 기절해버린다. InputAuthority를 가진 당사자에게 직접 리셋을 요청한다.
        RPC_ResetStatsAfterRevive();

        SetHeadVisible(true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ResetStatsAfterRevive()
    {
        CurrentHunger = maxHunger / 2;
        CurrentStamina = maxStamina / 2;
        CurrentThirst = maxThirst / 2;
        UpdateStatTexts();
    }

    public bool IsPlayerFainted()
    {
        return isFainted;
    }

    public void SetHeadVisible(bool visible)
    {
        if (Object.HasStateAuthority)
        {
            IsVisible = visible;
        }
        else
        {
            RPC_RequestSetVisible(visible);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSetVisible(bool visible)
    {
        IsVisible = visible;
    }

    private void OnVisibilityChanged() => ApplyVisibility(IsVisible);

    private void ApplyVisibility(bool visible)
    {
        if (networkedHead != null) networkedHead.enabled = visible;
        if (stateUI != null) stateUI.enabled = visible;
        if (animatedHead != null) animatedHead.enabled = !visible;
    }

    private void OnFaintedChanged()
    {
        if (animator == null) return;

        if (isFainted)
        {
            Player player = GetComponent<Player>();
            if (player != null) player.SetControlLocked(true);

            if (player != null) player.SetNetworkTransformHead(false);
            SetHeadVisible(false);
            animator.SetTrigger(IsDeadHash);

            if (HasInputAuthority && vfc != null)
            {
                vfc.StartFadeToWhite();
            }
        }
    }

    private void OnRevivedChanged()
    {
        if (isRevived && animator != null)
        {
            animator.SetTrigger(RevivedHash);

            if (HasInputAuthority)
            {
                if (vfc != null) vfc.StartFadeToTransparent();

                if (controlLockCoroutine != null) StopCoroutine(controlLockCoroutine);
                controlLockCoroutine = StartCoroutine(LockControlDuringRevive());
            }
        }
    }

    private IEnumerator LockControlDuringRevive()
    {
        Player player = GetComponent<Player>();

        yield return new WaitForSeconds(reviveAnimationDuration);

        SetHeadVisible(true);
        if (player != null)
        {
            player.SetControlLocked(false);
            player.SetNetworkTransformHead(true);
        }
    }

    // =========================================================================
    // 🌟 아이템 섭취 & 제거 처리 (클라이언트/호스트 공용)
    // =========================================================================

    public void RequestEatItem(NetworkObject itemNetObj, ItemType itemType)
    {
        // 1. 내 로컬 스탯 먼저 변경 (본인 캐릭터에만 적용)
        switch (itemType)
        {
            case ItemType.RawFood:
                ReduceStamina(20);
                Debug.Log($"🤢 날음식 섭취: 스태미나 -> {CurrentStamina}");
                break;
            case ItemType.Water:
                RestoreThirst(20);
                Debug.Log($"💧 음료 섭취: 갈증 -> {CurrentThirst}");
                break;
            case ItemType.CookedFood:
                RestoreHunger(20);
                Debug.Log($"🍎 음식 섭취: 배고픔 -> {CurrentHunger}");
                break;
        }

        // 2. 호스트에게 아이템 Despawn 요청
        if (Runner != null && Runner.IsRunning && itemNetObj != null)
        {
            RPC_DespawnEatenItem(itemNetObj);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DespawnEatenItem(NetworkObject itemNetObj)
    {
        if (itemNetObj != null && Runner != null && Runner.IsRunning)
        {
            Debug.Log($"🗑️ [Host] 아이템 제거 완료: {itemNetObj.name}");
            Runner.Despawn(itemNetObj);
        }
    }

    // =========================================================================
    // 🌟 호스트가 충돌 감지 후 해당 플레이어에게만 스탯 적용을 명령하는 RPC
    // =========================================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_ApplyEatEffect(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.RawFood:
                ReduceStamina(20);
                Debug.Log($"🤢 날음식 섭취 -> 내 스태미나: {CurrentStamina}");
                break;
            case ItemType.Water:
                RestoreThirst(20);
                Debug.Log($"💧 음료 섭취 -> 내 갈증: {CurrentThirst}");
                break;
            case ItemType.CookedFood:
                RestoreHunger(20);
                Debug.Log($"🍎 음식 섭취 -> 내 배고픔: {CurrentHunger}");
                break;
        }
    }
}