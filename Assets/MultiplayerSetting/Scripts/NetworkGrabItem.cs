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
        // 잡은 손(Interactor)이 로컬 플레이어의 손인지 확인
        // (XRI 내부적으로 로컬 플레이어만 이 이벤트를 먼저 탑니다)

        // 서버에게 이 오브젝트의 상태 권한(State Authority)을 나에게 달라고 요청합니다.
        Object.RequestStateAuthority();
    }

    // 2. 물건을 놓았을 때 호출
    private void OnGrabExited(SelectExitEventArgs args)
    {
        // 필요한 경우 권한을 다시 호스트/서버에게 반납할 수 있습니다.
        // (반납하지 않아도 다음 플레이어가 잡을 때 RequestStateAuthority()를 하면 넘어갑니다)
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