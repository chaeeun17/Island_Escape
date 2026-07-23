using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class InventoryZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 오브젝트가 잡을 수 있는 아이템인지 확인
        XRGrabInteractable grabInteractable = other.GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            // 손에서 놓았을 때(Select Exited) 감지할 이벤트 리스너 연결
            grabInteractable.selectExited.AddListener(OnItemReleasedInZone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        XRGrabInteractable grabInteractable = other.GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            // 아이템을 쥔 채로 인벤토리 영역을 벗어났다면 이벤트를 해제 (밖에서 놓으면 저장 안 되게)
            grabInteractable.selectExited.RemoveListener(OnItemReleasedInZone);
        }
    }

    // 인벤토리 영역 안에서 손을 놓았을 때 실행될 함수
    private void OnItemReleasedInZone(SelectExitEventArgs args)
    {
        // args.interactableObject를 통해 방금 손에서 놓은 아이템의 컴포넌트를 가져옴
        IXRInteractable releasedItem = args.interactableObject;
        GameObject itemGameObject = releasedItem.transform.gameObject;

        // 데이터 저장 처리
        

        Debug.Log($"{itemGameObject.name} 보관 성공!");

        // 메모리 누수 방지를 위해 파괴하기 전 리스너를 확실히 제거
        if (releasedItem is XRGrabInteractable grabItem)
        {
            grabItem.selectExited.RemoveListener(OnItemReleasedInZone);
        }

        // 오브젝트 파괴
        Destroy(itemGameObject);
    }
}