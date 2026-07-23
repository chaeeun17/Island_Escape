using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; 
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("아이템 정보 및 프리팹")]
    [SerializeField] private GameObject itemPrefab; 
    [SerializeField] private string itemName = "아이템 이름";
    [TextArea] [SerializeField] private string itemDescription = "아이템에 대한 상세 설명입니다.";

    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("양손 레이저")]
    // 왼손/오른손 레이저 오브젝트
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor leftRayInteractor;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor rightRayInteractor;

    // 하드웨어 그랩 버튼을 다이렉트로 감시하기 위한 변수
    private InputAction leftGripAction;
    private InputAction rightGripAction;
    
    // 현재 이 슬롯 위에 레이저가 도달해 있는지 여부
    private bool isHovered = false;

    void Awake()
    {
        leftGripAction = new InputAction("LeftGripDirect", binding: "<XRController>{LeftHand}/grip");
        rightGripAction = new InputAction("RightGripDirect", binding: "<XRController>{RightHand}/grip");
    }

    void OnEnable()
    {
        leftGripAction?.Enable();
        rightGripAction?.Enable();
    }

    void OnDisable()
    {
        leftGripAction?.Disable();
        rightGripAction?.Disable();
    }

    // 레이저 포인터가 UI 슬롯 영역 안으로 들어오는 순간
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    // 레이저 포인터가 UI 슬롯 영역 밖으로 나가는 순간
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    void Update()
    {
        // 레이저가 이 슬롯을 조준하고 있을 때만 매 프레임 그랩 버튼을 감시
        if (!isHovered || itemPrefab == null) return;

        // 양손의 그랩 버튼이 눌렸는지 확인 (0.5f 이상 꾹 쥐었는지 판독)
        bool leftGripPressed = leftGripAction != null && leftGripAction.ReadValue<float>() > 0.5f;
        bool rightGripPressed = rightGripAction != null && rightGripAction.ReadValue<float>() > 0.5f;

        if (leftGripPressed || rightGripPressed)
        {
            // 연속 스폰 버그 방지를 위해 감지 상태를 즉시 강제 종료
            isHovered = false;

            // 왼손으로 쥐었으면 왼손 레이저에, 오른손으로 쥐었으면 오른손 레이저에 아이템을 할당
            UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor targetInteractor = leftGripPressed ? leftRayInteractor : rightRayInteractor;

            // 혹시 인스펙터 연결을 깜빡했다면 씬에서 탐색 시도
            if (targetInteractor == null) targetInteractor = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>();
            if (targetInteractor == null || targetInteractor.hasSelection) return;

            // 진짜 3D 아이템 프리팹 소환
            Transform handTransform = targetInteractor.transform;
            GameObject spawned = Instantiate(itemPrefab, handTransform.position, handTransform.rotation);
            
            // 생성된 프리팹에 달린 그랩 컴포넌트 확보
            var grabInteractable = spawned.GetComponent<XRGrabInteractable>();

            if (grabInteractable != null)
            {
                // XRI 상호작용 매니저를 호출해 컨트롤러 손아귀에 강제로 챡! 쥐여주기
                Object.FindFirstObjectByType<XRInteractionManager>()?.SelectEnter(targetInteractor, (IXRSelectInteractable)grabInteractable);
                Debug.LogWarning($"★ [성공] {itemName}이 차단 우회하여 손에 쥐어졌습니다! ★");
            }
        }
    }

    public void UpdateDescriptionUI()
    {
        if (titleText != null && bodyText != null)
        {
            titleText.text = itemName;
            bodyText.text = itemDescription;
        }
    }
}