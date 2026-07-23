using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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

public class FishingCaster : MonoBehaviour
{
    [Header("Fishing State Values")]
    private FishingManager subject;
    public float waitingTime = 0;
    public float catchTime = 0;
    public bool isCatch = false;
    public Fish fishType;

    public Transform floatHolder;
    public GameObject fishingFloat;
    public Transform rodTip;
    public Rigidbody floatRb;

    [Header("Line")]
    public float maxLineLength = 15f;

    public bool IsCasting { get; private set; }

    [Header("Reeling Settings (회수 설정)")]
    [Tooltip("찌가 회수될 때의 속도")]
    public float reelSpeed = 12f;
    [Tooltip("찌가 낚싯대에 거의 닿았다고 판단할 거리")]
    public float reelResetThreshold = 0.5f;

    public FloatState state = FloatState.Ready;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    public void Cast(Vector3 velocity)
    {
        fishingFloat.transform.SetParent(null);
        state = FloatState.Flying;

        floatRb.isKinematic = false;
        floatRb.linearVelocity = velocity;
    }

    void FixedUpdate()
    {
        // 1.일반적인 한계 거리 초과 시 강제 리셋
        if (state != FloatState.Ready && state != FloatState.Reeling)
        {
            if (Vector3.Distance(rodTip.position, fishingFloat.transform.position) > maxLineLength)
            {
                ResetFloat();
            }
        }

        // 2. 릴링(회수) 상태일 때 처리
        if (state == FloatState.Reeling)
        {
            PerformReeling();
        }
    }

    /// <summary>
    /// 외부(예: 릴 버튼 입력, 트리거 클릭 등)에서 호출하여 릴링을 시작하는 메서드
    /// </summary>
    public void StartReeling()
    {
        if (state == FloatState.Floating || state == FloatState.Flying)
        {
            state = FloatState.Reeling;
            floatRb.isKinematic = false; // 등속 운동 처리를 위해 키네마틱 해제 (또는 직접 이동 처리)
        }
    }

    /// <summary>
    /// 물리 업데이트 프레임마다 찌를 낚싯대 쪽으로 당겨오는 메서드
    /// </summary>
    private void PerformReeling()
    {
        Vector3 targetDirection = (rodTip.position - fishingFloat.transform.position).normalized;
        float distance = Vector3.Distance(rodTip.position, fishingFloat.transform.position);

        // 낚싯대 끝에 거의 도달했다면 원래 낚싯대 찌 홀더 위치로 원상복구(Reset)
        if (distance <= reelResetThreshold)
        {
            ResetFloat();
            return;
        }

        // 찌가 낚싯대를 향해 날아가도록 강제 속도(Velocity) 부여
        floatRb.linearVelocity = targetDirection * reelSpeed;
    }

    public void OnHitWater(Vector3 waterPoint)
    {
        state = FloatState.Floating;

        floatRb.linearVelocity = Vector3.zero;
        floatRb.angularVelocity = Vector3.zero;

        floatRb.isKinematic = true;

        floatRb.position = waterPoint;

        subject = GameObject.Find("Ocean").GetComponent<FishingManager>();
        subject.ObserverRegister(this);
    }

    public void ResetFloat()
    {
        fishingFloat.transform.SetParent(floatHolder);

        fishingFloat.transform.localPosition = Vector3.zero;
        fishingFloat.transform.localRotation = Quaternion.identity;

        state = FloatState.Ready;

        floatRb.position = rodTip.position;
        floatRb.rotation = rodTip.rotation;

        floatRb.linearVelocity = Vector3.zero;
        floatRb.angularVelocity = Vector3.zero;

        floatRb.isKinematic = true;
    }


    [Header("진동 설정")]
    [Range(0, 1)][SerializeField] private float intensity = 0.5f;
    [SerializeField] private float duration = 0.2f;
    private XRGrabInteractable grabInteractable;

    public void TriggerHaptic()
    {
        // 1. 현재 이 오브젝트를 쥐고 있는 인터랙터가 있는지 확인
        if (grabInteractable.isSelected)
        {
            // 가장 먼저 잡은 손(인터랙터) 정보를 가져옵니다.
            var interactor = grabInteractable.firstInteractorSelecting;

            if (interactor != null)
            {
                // 2. 인터랙터의 게임 오브젝트 이름이나 태그를 분석하여 왼손/오른손을 구별합니다.
                string interactorName = interactor.transform.name.ToLower();
                string targetHand = "LeftHand"; // 기본값 기본 세팅

                if (interactorName.Contains("right"))
                {
                    targetHand = "RightHand";
                }
                else if (interactorName.Contains("left"))
                {
                    targetHand = "LeftHand";
                }

                // 3. 1번 방법(Input System API)을 사용하여 타겟 컨트롤러에 진동 전송
                SendInputSystemHaptic(targetHand, intensity, duration);
            }
        }
    }
    private void SendInputSystemHaptic(string handUsage, float intensity, float duration)
    {
        // "LeftHand" 또는 "RightHand" 사용 처리를 가진 XRController 디바이스를 찾습니다.
        var device = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(handUsage);

        if (device != null)
        {
            var command = UnityEngine.InputSystem.XR.Haptics.SendHapticImpulseCommand.Create(0, intensity, duration);
            device.ExecuteCommand(ref command);
            Debug.LogWarning($"{handUsage} 디바이스 진동!");
        }
        else
        {
            Debug.LogWarning($"InputSystem에서 {handUsage} 디바이스를 찾을 수 없습니다.");
        }
    }
}