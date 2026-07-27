using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;

public enum FloatState
{
    Ready,      // 낚싯대에 붙어있음
    Flying,     // 날아가는 중
    Floating,   // 물 위에 떠있음
    Reeling     // 회수 중
}
public enum Fish
{
    NONE,
    RED,
    BLUE,
    END
}

public class FishingCaster : NetworkBehaviour
{
    [Header("Line Renderer (낚싯줄)")]
    public LineRenderer lineRenderer; // Inspector에서 LineRenderer 연결

    [Header("Fish")]
    public GameObject redFish;
    public GameObject blueFish;

    [Header("Fishing State Values")]
    private FishingManager subject;

    [Networked] public float waitingTime { get; set; }
    [Networked] public float catchTime { get; set; }
    [Networked] public float targetWaitTime { get; set; }
    [Networked] public bool isCatch { get; set; }
    [Networked] public Fish fishType { get; set; }

    [HideInInspector] public float hapticTimer;

    [Header("Float Reference")]
    [Tooltip("낚싯대 끝 자식에 붙어있는 단순 Visual용 더미 찌")]
    public GameObject dummyFloat;

    [Tooltip("던졌을 때 생성할 실제 NetworkFloat Prefab")]
    public NetworkObject floatPrefab;

    public Transform rodTip;

    // 현재 생성되어 날아가고 있는 실제 찌의 참조
    [Networked] private NetworkObject activeFloatObj { get; set; }
    private FlyingFloat currentFlyingFloat;

    [Header("Line & Reeling")]
    public float maxLineLength = 15f;
    public float reelSpeed = 12f;
    public float reelResetThreshold = 0.5f;

    [Networked] public FloatState state { get; set; } = FloatState.Ready;

    [Header("진동 설정")]
    [Range(0, 1)][SerializeField] private float intensity = 0.5f;
    [SerializeField] private float duration = 0.2f;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    // 낚싯줄 위치 갱신은 시각적 요소이므로 Update에서 렌더링 프레임마다 처리
    private void Update()
    {
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || rodTip == null) return;

        // 찌가 날아가거나 물에 떠있거나 회수 중일 때 낚싯줄 표시
        if (state != FloatState.Ready && activeFloatObj != null)
        {
            if (!lineRenderer.enabled) lineRenderer.enabled = true;

            lineRenderer.SetPosition(0, rodTip.position); // 시작점: 낚싯대 끝
            lineRenderer.SetPosition(1, activeFloatObj.transform.position); // 끝점: 실제 찌 위치
        }
        else
        {
            // Ready 상태이거나 찌가 없을 때는 낚싯줄 숨김
            if (lineRenderer.enabled) lineRenderer.enabled = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (state == FloatState.Ready)
        {
            if (dummyFloat != null && !dummyFloat.activeSelf)
                dummyFloat.SetActive(true);
            return;
        }

        if (activeFloatObj == null) return;

        if (state != FloatState.Reeling)
        {
            if (Vector3.Distance(rodTip.position, activeFloatObj.transform.position) > maxLineLength)
            {
                ResetFloat();
                return;
            }
        }

        if (state == FloatState.Reeling)
        {
            PerformReeling();
        }
    }

    public void Cast(Vector3 velocity)
    {
        if (!HasStateAuthority) return;

        if (dummyFloat != null) dummyFloat.SetActive(false);

        activeFloatObj = Runner.Spawn(floatPrefab, rodTip.position, rodTip.rotation, Object.InputAuthority, (runner, obj) =>
        {
            FlyingFloat ff = obj.GetComponent<FlyingFloat>();
            if (ff != null)
            {
                ff.ownerCaster = this;
                ff.floatRb.isKinematic = false;
                ff.floatRb.linearVelocity = velocity;
            }
        });

        if (activeFloatObj != null)
        {
            currentFlyingFloat = activeFloatObj.GetComponent<FlyingFloat>();
        }

        state = FloatState.Flying;

        // 캐스팅 직후 낚싯줄 즉시 켜기
        if (lineRenderer != null) lineRenderer.enabled = true;
    }

    public void StartReeling()
    {
        if (!HasStateAuthority)
        {
            RPC_StartReeling();
            return;
        }

        if (state == FloatState.Floating || state == FloatState.Flying)
        {
            state = FloatState.Reeling;
            if (currentFlyingFloat != null)
            {
                currentFlyingFloat.floatRb.isKinematic = false;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartReeling()
    {
        StartReeling();
    }

    private void PerformReeling()
    {
        if (currentFlyingFloat == null) return;

        Vector3 targetDirection = (rodTip.position - currentFlyingFloat.transform.position).normalized;
        float distance = Vector3.Distance(rodTip.position, currentFlyingFloat.transform.position);

        if (distance <= reelResetThreshold)
        {
            ResetFloat();
            return;
        }

        currentFlyingFloat.floatRb.linearVelocity = targetDirection * reelSpeed;
    }

    public void OnHitWater(Vector3 waterPoint)
    {
        if (!HasStateAuthority) return;

        // 이미 릴링(회수) 중인 상태라면 물에 부딪혀도 멈추지 않도록 가드
        if (state == FloatState.Reeling) return;

        state = FloatState.Floating;

        if (currentFlyingFloat != null)
        {
            currentFlyingFloat.floatRb.linearVelocity = Vector3.zero;
            currentFlyingFloat.floatRb.angularVelocity = Vector3.zero;
            currentFlyingFloat.floatRb.isKinematic = true;
            currentFlyingFloat.transform.position = waterPoint;
        }

        GameObject oceanObj = GameObject.Find("Ocean");
        if (oceanObj != null)
        {
            subject = oceanObj.GetComponent<FishingManager>();
            if (subject != null)
            {
                subject.ObserverRegister(this);
            }
        }
    }
    public void ResetFloat()
    {
        Debug.LogWarning($"[Debug] ResetFloat()가 실행되었습니다! 현재 State: {state}");

        if (!HasStateAuthority) return;

        if (subject != null)
        {
            subject.ObserverDeregister(this);
        }

        if (activeFloatObj != null)
        {
            Runner.Despawn(activeFloatObj);
            activeFloatObj = null;
            currentFlyingFloat = null;
        }

        if (dummyFloat != null)
        {
            dummyFloat.SetActive(true);
        }

        state = FloatState.Ready;

        // 리셋 시 낚싯줄 끄기
        if (lineRenderer != null) lineRenderer.enabled = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable, TickAligned = false)]
    public void RPC_TriggerHaptic()
    {
        // 로컬 VR 컨트롤러 진동 실행
        TriggerHaptic();
    }

    public void TriggerHaptic()
    {
        Debug.Log("Trigger 되었습니다");
        if (HasStateAuthority && grabInteractable != null && grabInteractable.isSelected)
        {
            var interactor = grabInteractable.firstInteractorSelecting;

            if (interactor != null)
            {
                string interactorName = interactor.transform.name.ToLower();
                string targetHand = interactorName.Contains("right") ? "RightHand" : "LeftHand";

                Debug.Log("햅틱 진동!");
                SendInputSystemHaptic(targetHand, intensity, duration);
            }
        }
    }

    private void SendInputSystemHaptic(string handUsage, float intensity, float duration)
    {
        var device = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(handUsage);

        if (device != null)
        {
            var command = UnityEngine.InputSystem.XR.Haptics.SendHapticImpulseCommand.Create(0, intensity, duration);
            device.ExecuteCommand(ref command);
        }
    }
}