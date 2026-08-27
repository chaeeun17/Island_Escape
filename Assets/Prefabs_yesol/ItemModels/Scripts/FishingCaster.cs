using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;

public enum FloatState
{
    Ready,      // ���˴뿡 �پ�����
    Flying,     // ���ư��� ��
    Floating,   // �� ���� ������
    Reeling     // ȸ�� ��
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
    [Header("Line Renderer (������)")]
    public LineRenderer lineRenderer; // Inspector���� LineRenderer ����

    [Header("Fish")]
    public GameObject redFish;
    public GameObject blueFish;

    [Header("FishItem")]
    [Tooltip("�κ��丮�� �߰��� ������ ������ ������")]
    public Item redFishItem;
    public Item blueFishItem;

    [Header("Fishing State Values")]
    private FishingManager subject;

    [Networked] public float waitingTime { get; set; }
    [Networked] public float catchTime { get; set; }
    [Networked] public float targetWaitTime { get; set; }
    [Networked] public bool isCatch { get; set; }
    [Networked] public Fish fishType { get; set; }

    [HideInInspector] public float hapticTimer;

    [Header("Float Reference")]
    [Tooltip("���˴� �� �ڽĿ� �پ��ִ� �ܼ� Visual�� ���� ��")]
    public GameObject dummyFloat;

    [Tooltip("������ �� ������ ���� NetworkFloat Prefab")]
    public NetworkObject floatPrefab;

    public Transform rodTip;

    // ���� �����Ǿ� ���ư��� �ִ� ���� ���� ����
    [Networked] private NetworkObject activeFloatObj { get; set; }
    private FlyingFloat currentFlyingFloat;

    [Header("Line & Reeling")]
    public float maxLineLength = 15f;
    public float reelSpeed = 12f;
    public float reelResetThreshold = 0.5f;

    [Networked] public FloatState state { get; set; } = FloatState.Ready;

    [Header("���� ����")]
    [Range(0, 1)][SerializeField] private float intensity = 0.5f;
    [SerializeField] private float duration = 0.2f;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    // ������ ��ġ ������ �ð��� ����̹Ƿ� Update���� ������ �����Ӹ��� ó��
    private void Update()
    {
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || rodTip == null) return;

        // � ���ư��ų� ���� ���ְų� ȸ�� ���� �� ������ ǥ��
        if (state != FloatState.Ready && activeFloatObj != null)
        {
            if (!lineRenderer.enabled) lineRenderer.enabled = true;

            lineRenderer.SetPosition(0, rodTip.position); // ������: ���˴� ��
            lineRenderer.SetPosition(1, activeFloatObj.transform.position); // ����: ���� �� ��ġ
        }
        else
        {
            // Ready �����̰ų� � ���� ���� ������ ����
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

    // 🌟 spawnPos/spawnRot을 인자로 받는다 - Host에서 이 시점의 rodTip.position/rotation을 다시
    //    읽으면 안 된다. 클라이언트가 캐스팅할 때는 RPC로 넘어오는 값이라, Host의 rodTip은
    //    네트워크로 복제된(지연된) 값이라서 클라이언트가 실제로 놓은 순간의 낚싯대 방향과 다를 수 있다.
    //    그러면 velocity(그 순간 기준으로 계산됨)와 스폰 포즈가 서로 다른 시점 기준이 되어
    //    찌가 엉뚱한(심하면 반대) 방향으로 날아가 보인다. 그래서 클라이언트가 놓은 "그 순간"의
    //    rodTip 값을 그대로 전달받아 스폰 위치/회전/속도를 전부 같은 기준으로 맞춘다.
    public void Cast(Vector3 spawnPos, Quaternion spawnRot, Vector3 velocity)
    {
        if (!HasStateAuthority) return;

        if (dummyFloat != null) dummyFloat.SetActive(false);

        activeFloatObj = Runner.Spawn(floatPrefab, spawnPos, spawnRot, Object.InputAuthority, (runner, obj) =>
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

        // ĳ���� ���� ������ ��� �ѱ�
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
            SEManager.Instance.PlayWaterSplash();
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
            if (isCatch)
            {
                Debug.Log("[Fishing Debug] ���� ���� : �����⸦ ���������� ���� �÷Ƚ��ϴ�!");
                AddFishToInventory();
            }
            else
            {
                // �߰��� ���� ����� �α� (������ ���� ���� ���ų�, �̹� ��ģ �� ��� ���)
                Debug.Log("[Fishing Debug] ���� ���� : ������ �ƽ��ϴ�. ������ ���ų� �����Ⱑ ���������ϴ�.");
            }
            ResetFloat();
            return;
        }

        currentFlyingFloat.floatRb.linearVelocity = targetDirection * reelSpeed;
    }

    public void OnHitWater(Vector3 waterPoint)
    {
        if (!HasStateAuthority) return;

        // �̹� ����(ȸ��) ���� ���¶�� ���� �ε����� ������ �ʵ��� ����
        if (state == FloatState.Reeling) return;

        state = FloatState.Floating;
        SEManager.Instance.PlayWaterDrop();

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
        Debug.LogWarning($"[Debug] ResetFloat()�� ����Ǿ����ϴ�! ���� State: {state}");

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

        // ���� �� ������ ����
        if (lineRenderer != null) lineRenderer.enabled = false;
    }

    // 🌟 RpcTargets.StateAuthority였던 걸 InputAuthority로 수정.
    //    Host(StateAuthority)가 물림을 감지하고 이 RPC를 보내는데, 원래 대상이 StateAuthority였던
    //    탓에 Host 자기 자신에게만 실행되고(루프백) 실제로 낚싯대를 쥐고 있는 클라이언트에게는
    //    전혀 전달되지 않았다. 진동은 실제로 컨트롤러를 쥔 사람(InputAuthority)에게 가야 한다.
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Unreliable, TickAligned = false)]
    public void RPC_TriggerHaptic()
    {
        // ���� VR ��Ʈ�ѷ� ���� ����
        TriggerHaptic();
    }

    public void TriggerHaptic()
    {
        Debug.Log("Trigger �Ǿ����ϴ�");
        // 🌟 이 코드는 이제 InputAuthority(실제로 낚싯대를 쥔 사람)에서 실행되므로,
        //    HasStateAuthority가 아니라 HasInputAuthority로 체크해야 한다.
        if (HasInputAuthority && grabInteractable != null && grabInteractable.isSelected)
        {
            var interactor = grabInteractable.firstInteractorSelecting;

            if (interactor != null)
            {
                string interactorName = interactor.transform.name.ToLower();
                string targetHand = interactorName.Contains("right") ? "RightHand" : "LeftHand";

                Debug.Log("��ƽ ����!");
                SEManager.Instance.PlayFish();
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

    public void AddFishToInventory()
    {
        //  ������: Inventory.Instance -> NetworkInventory.Instance
        Debug.Log($"[Fishing Debug] AddFishToInventory �����! ������ Ÿ��: {fishType}, �κ��丮 ���� ����: {NetworkInventory.Instance != null}");

        //  ������: Inventory.Instance -> NetworkInventory.Instance
        if (fishType == Fish.NONE || NetworkInventory.Instance == null)
        {
            Debug.LogWarning("[Fishing Debug]  �κ��丮�� ���ų� ������ Ÿ���� �������� �ʾҽ��ϴ�.");
            return;
        }

        Item caughtFish = null;
        switch (fishType)
        {
            case Fish.RED:
                caughtFish = redFishItem;
                break;
            case Fish.BLUE:
                caughtFish = blueFishItem;
                break;
        }

        if (caughtFish == null)
        {
            Debug.LogWarning($"[Fishing Debug]  {fishType} �����⸦ ��������, Inspector�� Fish Item ������ ����ֽ��ϴ�!");
            return;
        }

        Item itemToAdd = Instantiate(caughtFish);
        itemToAdd.set_Item_Count(1);

        //  ������: Inventory.Instance -> NetworkInventory.Instance
        NetworkInventory.Instance.AddItem(itemToAdd);

        Debug.Log($"[Fishing Debug]  {fishType} �����⸦ ���ҽ��ϴ�! �κ��丮�� �߰� �Ϸ�.");
    }
}
