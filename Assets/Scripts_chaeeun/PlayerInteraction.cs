using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 2f; // 상호작용 가능 거리
    [Header("Global UI Settings")]
    // 씬에 하나만 배치한 전역 월드스페이스 캔버스 레이아웃
    public GameObject globalInteractCanvas; 
    public TextMeshProUGUI globalInteractText;

    public float uiYOffset = 0.5f;  // 물체 중심에서 ui를 얼마나 내릴지 

    public InputActionProperty interactAction;

    private IInteractable currentInteractable;

    void Update()
    {
        CheckForInteractable();

        // 상호작용 가능한 대상이 있고, A 버튼(여기서는 키보드 E 또는 콘솔 A 버튼)을 눌렀을 때
        if (currentInteractable != null && interactAction.action != null && interactAction.action.WasPressedThisFrame()) 
        {
            currentInteractable.Interact(); // 대상이 음식이든 상자든 자기 할 일을 알아서 함!

            globalInteractText.text = $"[A] {currentInteractable.GetInteractText()}";
        }
    }

    void CheckForInteractable()
    {
        // 플레이어 시선 방향으로 레이캐스트를 쏨
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // 부딪힌 오브젝트에서 IInteractable 컴포넌트를 가져옴
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;
                
                // 전역 UI 위치를 조준된 물체의 아래쪽에 위치
                Vector3 targetPosition = hit.collider.transform.position;
                targetPosition.y -= uiYOffset; // 물체 중심 좌표에서 설정한 만큼 아래로 빼기
                
                globalInteractCanvas.transform.position = targetPosition;

                // 텍스트 변경 및 UI 활성화
                globalInteractText.text = $"[A] {interactable.GetInteractText()}";
                globalInteractCanvas.SetActive(true);
                return;
            }
        }

        // 아무것도 감지되지 않으면 UI를 끔
        currentInteractable = null;
       globalInteractCanvas.SetActive(false);
    }
}