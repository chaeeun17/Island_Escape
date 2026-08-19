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
using Fusion;

public class SLot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("슬롯 데이터")]
    public Item item;

    [Header("UI 연결")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text slot_Item_Count_TXT;

    [Header("바닥 설치형 아이템 설정")]
    [Tooltip("손에 쥐지 않고 바닥에 바로 스폰할 아이템/프리팹 이름 키워드")]
    private List<string> groundSpawnItemNames = new List<string> { "Purifier", "Tent", "BowDrill", "FirePit" };
    private float spawnForwardDistance = 1.2f;       // 일반 설치물 플레이어 앞거리
    private float tentSpawnForwardDistance = 3.0f;  // 텐트 전용 앞거리

    private XRRayInteractor leftRayInteractor;
    private XRRayInteractor rightRayInteractor;

    private InputAction leftGripAction;
    private InputAction rightGripAction;
    private bool isHovered = false;

    private NetworkInventory ownerInventory;

    private void Awake()
    {
        ownerInventory = GetComponentInParent<NetworkInventory>();

        // VR 컨트롤러 그랩 입력 바인딩
        leftGripAction = new InputAction("LeftGripDirect", binding: "<XRController>{LeftHand}/grip");
        rightGripAction = new InputAction("RightGripDirect", binding: "<XRController>{RightHand}/grip");
    }

    private void Start()
    {
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

    private void FindRayInteractors()
    {
        XRRayInteractor[] interactors = Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var interactor in interactors)
        {
            string objectName = interactor.gameObject.name.ToLower();

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

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
        if (!isHovered || !isin() || item == null) return;

        bool leftGripPressed = leftGripAction != null && leftGripAction.ReadValue<float>() > 0.5f;
        bool rightGripPressed = rightGripAction != null && rightGripAction.ReadValue<float>() > 0.5f;

        if (leftGripPressed || rightGripPressed)
        {
            isHovered = false;

            GameObject prefabToSpawn = item.GetPrefab3D();
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"⚠️ [{item.getItemName()}] 스폰할 3D 아이템 프리팹이 없습니다!");
                return;
            }

            XRRayInteractor targetInteractor = leftGripPressed ? leftRayInteractor : rightRayInteractor;
            if (targetInteractor == null)
            {
                FindRayInteractors();
                targetInteractor = leftGripPressed ? leftRayInteractor : rightRayInteractor;
            }

            // 바닥 설치형 아이템 여부 확인
            bool isGroundItem = IsGroundSpawnItem(item);

            // 손에 쥐는 아이템인데 이미 다른 물건을 들고 있다면 패스
            if (!isGroundItem && (targetInteractor == null || targetInteractor.hasSelection))
            {
                return;
            }

            // 스폰 위치 및 회전 계산
            Vector3 spawnPos;
            Quaternion spawnRot;

            if (isGroundItem)
            {
                GetGroundSpawnPose(out spawnPos, out spawnRot);
            }
            else
            {
                Transform handTransform = targetInteractor != null ? targetInteractor.transform : transform;
                spawnPos = handTransform.position;
                spawnRot = handTransform.rotation;
            }

            // NetworkRunner를 통한 네트워크 스폰
            NetworkInventory targetInventory = ownerInventory != null ? ownerInventory : NetworkInventory.Instance;
            GameObject spawned = null;

            if (targetInventory != null && targetInventory.Runner != null && targetInventory.Runner.IsRunning)
            {
                NetworkObject spawnedNetObj = targetInventory.Runner.Spawn(prefabToSpawn, spawnPos, spawnRot);
                if (spawnedNetObj != null)
                {
                    spawned = spawnedNetObj.gameObject;
                }
            }
            else
            {
                spawned = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            }

            if (spawned == null) return;

            // 설치형 아이템 vs 손 쥐기 아이템 분기 처리
            if (isGroundItem)
            {
                Debug.Log($"⛺ [설치형 아이템] {item.getItemName()}을(를) 바닥에 네트워크 스폰했습니다.");
            }
            else
            {
                var grabInteractable = spawned.GetComponent<XRGrabInteractable>();
                if (grabInteractable != null && targetInteractor != null)
                {
                    XRInteractionManager interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
                    if (interactionManager != null)
                    {
                        interactionManager.SelectEnter((IXRSelectInteractor)targetInteractor, (IXRSelectInteractable)grabInteractable);
                        Debug.Log($"★ [성공] {item.getItemName()}을(를) 꺼내 손에 쥐었습니다! ★");
                    }
                }
            }

            // 인벤토리 수량 1개 차감
            if (targetInventory != null)
            {
                targetInventory.ConsumeItem(item.getItemName(), 1);
            }
        }
    }

    private bool IsGroundSpawnItem(Item targetItem)
    {
        if (targetItem == null) return false;

        string itemName = targetItem.getItemName();
        string prefabName = targetItem.GetPrefab3D() != null ? targetItem.GetPrefab3D().name : "";

        string cleanItemName = itemName.Replace(" ", "").Replace("_", "").ToLower();
        string cleanPrefabName = prefabName.Replace(" ", "").Replace("_", "").ToLower();

        foreach (var keyword in groundSpawnItemNames)
        {
            string cleanKeyword = keyword.Replace(" ", "").Replace("_", "").ToLower();
            
            if (cleanItemName.Contains(cleanKeyword) || cleanPrefabName.Contains(cleanKeyword))
            {
                return true;
            }
        }

        return false;
    }

    private void GetGroundSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;

        Vector3 forward = camTransform.forward;
        forward.y = 0;
        forward.Normalize();

        float targetDistance = spawnForwardDistance;
        if (item != null)
        {
            string itemName = item.getItemName().ToLower();
            string prefabName = item.GetPrefab3D() != null ? item.GetPrefab3D().name.ToLower() : "";

            if (itemName.Contains("tent") || prefabName.Contains("tent"))
            {
                targetDistance = tentSpawnForwardDistance;
            }
        }

        Vector3 targetXZ = camTransform.position + (forward * targetDistance);
        
        // 레이 시작점을 머리 위 0.5m 지점으로 잡고 아래로 20m 넉넉하게 투사 (트리거 콜라이더 무시)
        Vector3 rayStart = new Vector3(targetXZ.x, camTransform.position.y + 0.5f, targetXZ.z);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            position = hit.point + (Vector3.up * 0.05f);
        }
        else
        {
            // 바닥을 찾지 못했을 경우 눈높이가 아닌 지면 추정치(눈높이 아래 1.4m)로 배치
            position = new Vector3(targetXZ.x, camTransform.position.y - 1.4f, targetXZ.z);
        }

        rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

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