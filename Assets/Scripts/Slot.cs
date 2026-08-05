using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SLot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("슬롯 데이터")]
    public Item item;

    [Header("UI 연결")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text slot_Item_Count_TXT;

    // 🌟 인스펙터 할당 없이 코드로 찾아 저장할 참조 변수
    private XRRayInteractor leftRayInteractor;
    private XRRayInteractor rightRayInteractor;

    private InputAction leftGripAction;
    private InputAction rightGripAction;
    private bool isHovered = false;

    private NetworkInventory ownerInventory;

    private void Awake()
    {
        ownerInventory = GetComponentInParent<NetworkInventory>();

        // VR 컨트롤러 그랩 입력 직접 바인딩
        leftGripAction = new InputAction("LeftGripDirect", binding: "<XRController>{LeftHand}/grip");
        rightGripAction = new InputAction("RightGripDirect", binding: "<XRController>{RightHand}/grip");
    }

    private void Start()
    {
        // 🌟 코드로 XRRayInteractor 자동 탐색 및 연결
        FindRayInteractors();
    }

    private void OnEnable()
    {
        leftGripAction?.Enable();
        rightGripAction?.Enable();
    }

    private void OnDisable()
    {
        leftGripAction?.Disable();
        rightGripAction?.Disable();
    }

    /// <summary>
    /// 씬/캐릭터 내의 왼손/오른손 XRRayInteractor를 코드로 찾아서 연결합니다.
    /// </summary>
    private void FindRayInteractors()
    {
        XRRayInteractor[] interactors = Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var interactor in interactors)
        {
            string objectName = interactor.gameObject.name.ToLower();

            // 왼손/오른손 구분해서 자동 할당
            if (objectName.Contains("left"))
            {
                leftRayInteractor = interactor;
            }
            else if (objectName.Contains("right"))
            {
                rightRayInteractor = interactor;
            }
        }
    }

    // VR 포인터가 슬롯 UI 영역 안으로 들어올 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    // VR 포인터가 슬롯 UI 영역 밖으로 나갈 때
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    // 슬롯 클릭 시 디테일 패널 출력
    public void OnPointerClick(PointerEventData eventData)
    {
        NetworkInventory targetInventory = ownerInventory != null ? ownerInventory : NetworkInventory.Instance;

        if (targetInventory == null) return;

        if (isin() && item != null)
        {
            targetInventory.ShowItemDetails(item);
        }
        else
        {
            targetInventory.ShowItemDetails(null);
        }
    }

    private void Update()
    {
        // 호버링 중이 아니거나 슬롯이 비어있으면 체크하지 않음
        if (!isHovered || !isin() || item == null) return;

        // 양손 Grip 버튼 입력 감지 (0.5f 이상 눌렀는지 확인)
        bool leftGripPressed = leftGripAction != null && leftGripAction.ReadValue<float>() > 0.5f;
        bool rightGripPressed = rightGripAction != null && rightGripAction.ReadValue<float>() > 0.5f;

        if (leftGripPressed || rightGripPressed)
        {
            // 중복 스폰 방지
            isHovered = false;

            // 스폰할 3D 아이템 프리팹 확인
            GameObject prefabToSpawn = item.GetPrefab3D();

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"⚠️ [{item.getItemName()}] 스폰할 3D 아이템 프리팹이 없습니다!");
                return;
            }

            // 눌린 손에 맞게 XRRayInteractor 지정 (null일 경우 백업 재탐색)
            XRRayInteractor targetInteractor = leftGripPressed ? leftRayInteractor : rightRayInteractor;

            if (targetInteractor == null)
            {
                FindRayInteractors();
                targetInteractor = leftGripPressed ? leftRayInteractor : rightRayInteractor;
            }

            // 여전히 못 찾았거나 손에 이미 다른 물건을 쥐고 있다면 스킵
            if (targetInteractor == null || targetInteractor.hasSelection) return;

            // 3D 아이템 생성
            Transform handTransform = targetInteractor.transform;
            GameObject spawned = Instantiate(prefabToSpawn, handTransform.position, handTransform.rotation);

            var grabInteractable = spawned.GetComponent<XRGrabInteractable>();

            if (grabInteractable != null)
            {
                // XRInteractionManager를 불러 컨트롤러 손에 즉시 착 붙여주기
                XRInteractionManager interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
                if (interactionManager != null)
                {
                    interactionManager.SelectEnter((IXRSelectInteractor)targetInteractor, (IXRSelectInteractable)grabInteractable);
                    Debug.Log($"★ [성공] {item.getItemName()}을(를) 꺼내 손에 쥐었습니다! ★");

                    // 인벤토리 데이터에서 수량 1개 차감 및 UI 재정렬
                    NetworkInventory targetInventory = ownerInventory != null ? ownerInventory : NetworkInventory.Instance;
                    if (targetInventory != null)
                    {
                        targetInventory.ConsumeItem(item.getItemName(), 1);
                    }
                }
            }
        }
    }

    // 슬롯에 아이템 세팅
    public void SetSlot(Item newItem, bool addCount = false)
    {
        this.item = newItem;

        if (iconImage != null && item != null)
        {
            iconImage.sprite = item.get_Item_Sprite();
            iconImage.gameObject.SetActive(true);
        }

        Update_Count_text();
    }

    // 슬롯 비우기
    public void Clear_Slot()
    {
        this.item = null;

        if (iconImage != null) iconImage.gameObject.SetActive(false);
        if (slot_Item_Count_TXT != null) slot_Item_Count_TXT.gameObject.SetActive(false);
    }

    public bool isin()
    {
        return item != null;
    }

    public Item getItem()
    {
        return item;
    }

    public void Update_Count_text()
    {
        if (slot_Item_Count_TXT != null && item != null)
        {
            slot_Item_Count_TXT.text = item.get_Item_Count().ToString();
            slot_Item_Count_TXT.gameObject.SetActive(true);
        }
    }
}