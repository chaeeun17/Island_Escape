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
            Vector3 castVelocity = dir * CurrentSpeed * 2f;

            // 🌟 Host의 rodTip은 네트워크로 복제된(지연된) 값이라, 클라이언트가 실제로 트리거를
            //    놓은 "그 순간"의 낚싯대 위치/회전과 다를 수 있다(휘두르는 동작 중이라 특히 심함).
            //    velocity만 넘기고 Host가 자기 rodTip으로 스폰 포즈를 다시 읽으면, 서로 다른
            //    시점의 값이 섞여서 찌가 엉뚱한(심하면 반대) 방향으로 날아간다. 그래서 스폰
            //    위치/회전까지 클라이언트가 놓은 순간의 값 그대로 함께 전달해서 기준을 통일한다.
            RPC_RequestCast(rodTip.position, rodTip.rotation, castVelocity);
        }
    }

    // ĳ���� ���� RPC (State Authority���� Spawn ����)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCast(Vector3 spawnPos, Quaternion spawnRot, Vector3 velocity)
    {
        if (caster != null)
        {
            caster.Cast(spawnPos, spawnRot, velocity);
        }
    }

    // �� ���� OnTriggerEnter(�浹 ����)�� ������ �����Ǿ����ϴ�.
    // ������ �浹�� ���� �����Ǵ� ���� �� Prefab�� ���� FlyingFloat�� ����մϴ�.
}