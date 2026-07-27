using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class NetworkGrabItem : NetworkBehaviour
{
    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rb;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();

        // XRI 이벤트 연결
        _grabInteractable.selectEntered.AddListener(OnGrabEntered);
        _grabInteractable.selectExited.AddListener(OnGrabExited);
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabEntered);
            _grabInteractable.selectExited.RemoveListener(OnGrabExited);
        }
    }

    // 1. 물건을 잡았을 때 호출
    private void OnGrabEntered(SelectEnterEventArgs args)
    {
        // 1. 네트워크 초기화(Spawn)가 완전히 끝났는지 체크
        if (Object == null || !Object.IsValid)
        {
            Debug.LogWarning("[NetworkGrabItem] 아직 네트워크에 스폰되지 않아 권한을 요청할 수 없습니다.");
            return;
        }

        // 2. 권한 요청 (Shared Mode / Host Mode 구분 처리)
        if (Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Shared)
        {
            // Shared Mode에서는 RequestStateAuthority가 State + Input 권한을 모두 가져옴
            Object.RequestStateAuthority();
        }
        else
        {
            // Host/Server Mode에서는 Host에게 Input Authority 할당을 요청해야 함
            if (!HasStateAuthority)
            {
                RPC_RequestAuthority(Runner.LocalPlayer);
            }
        }

        Debug.Log($"[NetworkGrabItem] 권한 요청 완료! (현재 StateAuth: {HasStateAuthority}, InputAuth: {HasInputAuthority})");
    }

    // 2. 물건을 놓았을 때 호출
    private void OnGrabExited(SelectExitEventArgs args)
    {
        // 필요한 경우 권한을 다시 호스트/서버에게 반납할 수 있습니다.
        // (반납하지 않아도 다음 플레이어가 잡을 때 RequestStateAuthority()를 하면 넘어갑니다)
    }

    // Host Mode 전용: 서버에 Input Authority 할당 요청
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAuthority(PlayerRef player)
    {
        Object.AssignInputAuthority(player);
    }

    public override void FixedUpdateNetwork()
    {
        // 권한이 없는 클라이언트에서는 물리가 예측(Prediction)되어 충돌이 튀는 것을 방지하기 위해
        // 권한 소유 여부에 따라 Is Kinematic을 제어해줍니다.
        if (_rb != null)
        {
            // 권한이 있으면 물리 계산을 직접 하고(false), 없으면 네트워크 동기화만 받음(true)
            _rb.isKinematic = !HasStateAuthority;
        }
    }
}