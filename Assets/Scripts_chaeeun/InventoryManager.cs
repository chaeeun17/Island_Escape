using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("XRI 상호작용 매니저 연동")]
    [SerializeField] private XRInteractionManager interactionManager;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (interactionManager == null)
        {
            interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
        }
    }

    // 🌟 VR 컨트롤러 인터랙터를 직접 건네받아 손에 쥐여주는 핵심 로직
    public void SpawnItemInHand(GameObject itemPrefab, IXRSelectInteractor interactor)
    {
        if (interactor == null || interactionManager == null) return;

        // 1. 이미 손에 무언가(나침반, 도끼 등)를 쥐고 있다면 중복 스폰 방지
        if (interactor.hasSelection)
        {
            Debug.LogWarning("⚠️ 이미 손에 물건을 쥐고 있어서 인벤토리 아이템을 꺼낼 수 없습니다.");
            return;
        }

        // 2. VR 컨트롤러의 현재 3D 위치와 회전값 확보
        Transform handTransform = interactor.transform;
        
        // 3. 진짜 3D 아이템 오브젝트를 컨트롤러 바로 그 자리에 소환
        GameObject spawnedItem = Instantiate(itemPrefab, handTransform.position, handTransform.rotation);
        
        // 4. 소환된 진짜 아이템의 그랩 컴포넌트 추출
        var grabInteractable = spawnedItem.GetComponent<XRGrabInteractable>();
        
        if (grabInteractable != null)
        {
            // 🌟 [VR 핵심 마술] 유니티 XRI 시스템에 명령하여 
            // "방금 소환한 물체를 이 VR 컨트롤러가 즉시 그랩(Select)한 상태로 만들어라!" 처리
            interactionManager.SelectEnter(interactor, (IXRSelectInteractable)grabInteractable);
            
            Debug.Log($"📦 [VR] 인벤토리에서 {spawnedItem.name}을(를) 꺼내 컨트롤러에 강제 장착 완료!");
        }
    }
}