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

    // 부모(NetworkGrabItem)의 권한이나 Caster의 권한 중 하나라도 있으면 동작 허용
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
            // XRI Trigger 뗌 이벤트 연결
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

        // DeltaTime 0 방지
        if (Time.deltaTime > 0)
        {
            CurrentVelocity = (rodTip.position - lastPosition) / Time.deltaTime;
            CurrentSpeed = CurrentVelocity.magnitude;
        }

        lastPosition = rodTip.position;
    }

    private void OnTriggerReleased(DeactivateEventArgs args)
    {
        // 1. Caster 가 없거나, 네트워크 스폰이 완료되지 않았다면 무시
        if (caster == null || caster.Object == null || !caster.Object.IsValid)
        {
            Debug.LogWarning("FishingCaster가 아직 네트워크에 Spawn되지 않아 입력을 무시합니다.");
            return;
        }

        // 2. 권한 확인 (손으로 잡고 있는 유저만 실행)
        if (!HasParentInputAuthority) return;

        // 3. Floating(물 위에 뜸) 상태이면 릴링 시작
        if (caster.state == FloatState.Floating)
        {
            Debug.Log("낚싯줄 회수!");
            caster.StartReeling();
        }
        // 4. Ready 상태이면 캐스팅(던지기) 실행
        else if (caster.state == FloatState.Ready)
        {
            Debug.Log("버튼 해제 -> 캐스팅 요청!");

            Vector3 dir = (rodTip.forward * 0.9f + CurrentVelocity.normalized * 0.1f).normalized;
            Vector3 castVelocity = dir * CurrentSpeed * 2f;

            // 로컬에서 계산된 캐스팅 속도를 State Authority로 전송
            RPC_RequestCast(castVelocity);
        }
    }

    // 캐스팅 실행 RPC (State Authority에서 Spawn 수행)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCast(Vector3 velocity)
    {
        if (caster != null)
        {
            caster.Cast(velocity);
        }
    }

    // ※ 기존 OnTriggerEnter(충돌 감지)는 완전히 삭제되었습니다.
    // 물과의 충돌은 새로 생성되는 실제 찌 Prefab에 붙은 FlyingFloat가 담당합니다.
}