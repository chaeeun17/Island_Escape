using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;

public class RodSwingDetector : NetworkBehaviour
{
    public FishingCaster caster;

    private XRGrabInteractable grabInteractable;
    private NetworkGrabItem networkGrabItem;
    public Transform rodTip;

    public float CurrentSpeed { get; private set; }
    public Vector3 CurrentVelocity { get; private set; }

    private Vector3 lastPosition;

    // �θ�(NetworkGrabItem)�� �����̳� Caster�� ���� �� �ϳ��� ������ ���� ���
    private bool HasParentInputAuthority =>
        (networkGrabItem != null && (networkGrabItem.HasInputAuthority || networkGrabItem.HasStateAuthority)) ||
        (caster != null && (caster.HasInputAuthority || caster.HasStateAuthority));

    private void Awake()
    {
        grabInteractable = GetComponentInParent<XRGrabInteractable>();
        networkGrabItem = GetComponentInParent<NetworkGrabItem>();
    }

    public override void Spawned()
    {
        if (grabInteractable != null)
        {
            // XRI Trigger �� �̺�Ʈ ����
            grabInteractable.deactivated.AddListener(OnTriggerReleased);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (grabInteractable != null)
        {
            grabInteractable.deactivated.RemoveListener(OnTriggerReleased);
        }
    }

    void Start()
    {
        if (rodTip != null)
        {
            lastPosition = rodTip.position;
        }
    }

    void Update()
    {
        if (!HasParentInputAuthority || rodTip == null) return;

        // DeltaTime 0 ����
        if (Time.deltaTime > 0)
        {
            CurrentVelocity = (rodTip.position - lastPosition) / Time.deltaTime;
            CurrentSpeed = CurrentVelocity.magnitude;
        }

        lastPosition = rodTip.position;
    }

    private void OnTriggerReleased(DeactivateEventArgs args)
    {
        // 1. Caster �� ���ų�, ��Ʈ��ũ ������ �Ϸ���� �ʾҴٸ� ����
        if (caster == null || caster.Object == null || !caster.Object.IsValid)
        {
            Debug.LogWarning("FishingCaster�� ���� ��Ʈ��ũ�� Spawn���� �ʾ� �Է��� �����մϴ�.");
            return;
        }

        // 2. ���� Ȯ�� (������ ��� �ִ� ������ ����)
        if (!HasParentInputAuthority) return;

        // 3. Floating(�� ���� ��) �����̸� ���� ����
        if (caster.state == FloatState.Floating)
        {
            Debug.Log("������ ȸ��!");
            caster.StartReeling();
        }
        // 4. Ready �����̸� ĳ����(������) ����
        else if (caster.state == FloatState.Ready)
        {
            Debug.Log("��ư ���� -> ĳ���� ��û!");

            Vector3 dir = (rodTip.forward * 0.9f + CurrentVelocity.normalized * 0.1f).normalized;
            float speed = CurrentSpeed * 2f;

            // 🌟 월드 공간 속도 벡터를 그대로 보내면 안 된다. 클라이언트의 로컬(지연 없는) rodTip
            //    기준으로 계산한 방향을, RPC가 Host에서 실제 처리되는(=Cast()가 실행되는) 시점에
            //    그대로 적용하면 문제가 생긴다. 그 사이 클라이언트가 낚싯대를 계속 휘두르고 있었다면,
            //    Cast()가 스폰 위치/회전으로 사용하는 (지연된) rodTip.rotation은 이미 이 방향과
            //    어긋난 상태라서 "찌가 낚싯대가 가리키는 방향과 다르게 날아가는" 현상이 발생한다.
            //    그래서 rodTip의 로컬 축 기준 방향(회전에 무관한 값)과 속력만 전달하고,
            //    실제 월드 벡터는 Cast()가 스폰 포즈로 쓰는 것과 동일한 rodTip.rotation을 이용해
            //    Host 쪽에서 그 자리에서 다시 계산한다. 이러면 스폰 방향과 던지는 방향이 항상 일치한다.
            Vector3 localDir = rodTip.InverseTransformDirection(dir);

            // ���ÿ��� ���� ĳ���� ������/�ӵ��� State Authority�� ����
            RPC_RequestCast(localDir, speed);
        }
    }

    // ĳ���� ���� RPC (State Authority���� Spawn ����)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCast(Vector3 localDir, float speed)
    {
        if (caster != null && caster.rodTip != null)
        {
            Vector3 velocity = caster.rodTip.TransformDirection(localDir) * speed;
            caster.Cast(velocity);
        }
    }

    // �� ���� OnTriggerEnter(�浹 ����)�� ������ �����Ǿ����ϴ�.
    // ������ �浹�� ���� �����Ǵ� ���� �� Prefab�� ���� FlyingFloat�� ����մϴ�.
}