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
            // 영역을 벗어나면 이벤트 해제ㅁㅊ
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

        // 메모리 누수 방지 및 중복 호출 차단을 위해 리스너 즉시 제거
        if (releasedItem is XRGrabInteractable grabItem)
        {
            grabItem.selectExited.RemoveListener(OnItemReleasedInZone);
        }

        // 🌟 1. 3D 오브젝트 이름에서 "(Clone)" 제거 및 깔끔하게 정리
        string cleanItemName = itemGameObject.name.Replace("(Clone)", "").Trim();

        // 🌟 2. [데이터 저장 처리] - 이름 기반으로 SO를 찾아서 로컬 가방에 넣기!
        if (NetworkInventory.Instance != null)
        {
            NetworkInventory.Instance.AddItemByName(cleanItemName);
            Debug.Log($"🎒 [{cleanItemName}] 인벤토리 보관 성공!");
        }
        else
        {
            Debug.LogError("🚨 NetworkInventory.Instance가 존재하지 않습니다!");
            return;
        }

        // 🌟 3. 네트워크 상에서 3D 오브젝트 제거 (Despawn)
        // (단, Despawn은 StateAuthority 권한이 필요하므로 RPC를 거치거나 HasStateAuthority 체크)
        if (netObj.Runner != null && netObj.Runner.IsRunning)
        {
            if (netObj.HasStateAuthority)
            {
                netObj.Runner.Despawn(netObj);
            }
            else
            {
                // 내가 호스트가 아니라 클라이언트라면 호스트에게 Despawn 요청 RPC 호출 또는 RequestStateAuthority 필요
                // Fusion 기본 설정에서 Client가 잡고있는 상호작용 오브젝트의 권한을 얻는 경우가 많습니다.
                netObj.RequestStateAuthority();
                netObj.Runner.Despawn(netObj);
            }
        }
    }
}
