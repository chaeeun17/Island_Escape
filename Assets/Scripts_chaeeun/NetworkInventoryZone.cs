using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkInventoryZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 오브젝트가 XRGrabInteractable을 가지고 있는지 확인
        XRGrabInteractable grabInteractable = other.GetComponentInParent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            // 중복 리스너 등록 방지를 위해 기존 이벤트 제거 후 추가
            grabInteractable.selectExited.RemoveListener(OnItemReleasedInZone);
            grabInteractable.selectExited.AddListener(OnItemReleasedInZone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        XRGrabInteractable grabInteractable = other.GetComponentInParent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            // 영역을 벗어나면 이벤트 해제
            grabInteractable.selectExited.RemoveListener(OnItemReleasedInZone);
        }
    }

    // 인벤토리 영역 안에서 손을 놓았을 때 실행될 함수
    private void OnItemReleasedInZone(SelectExitEventArgs args)
    {
        IXRInteractable releasedItem = args.interactableObject;
        GameObject itemGameObject = releasedItem.transform.gameObject;

        // [포톤 퓨전 네트워크 오브젝트 검사]
        NetworkObject netObj = itemGameObject.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        // 소유권 확인
        if (netObj.HasStateAuthority)
        {
            // 메모리 누수 방지 및 중복 호출 차단을 위해 리스너 즉시 제거
            if (releasedItem is XRGrabInteractable grabItem)
            {
                grabItem.selectExited.RemoveListener(OnItemReleasedInZone);
            }

            // ==========================================
            // 💾 [데이터 저장 처리]
            // 예: InventoryManager.Instance.AddItem(itemGameObject.name);
            // ==========================================

            Debug.Log($"🎒 [{itemGameObject.name}] 인벤토리 보관 성공!");

            // Destroy(itemGameObject) 대신 Fusion의 Despawn을 써야 모든 플레이어 화면에서 아이템이 지워집니다.
            if (netObj.Runner != null && netObj.Runner.IsRunning)
            {
                netObj.Runner.Despawn(netObj);
            }
        }
    }
}
