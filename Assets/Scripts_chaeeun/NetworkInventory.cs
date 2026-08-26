using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class NetworkInventory : NetworkBehaviour
{
    public SLot[] itemSlots;

    public static NetworkInventory Instance { get; private set; }

    [Header("UI Panel")]
    [Tooltip("껐다 켤 실제 UI 패널 (inventory_panel 연결)")]
    public GameObject inventoryPanel;

    [Header("Filter Buttons UI")]
    public Button[] filterButtons;
    public Color selectedColor = Color.yellow;
    public Color defaultColor = new Color(1.0f, 0.8f, 0.3f, 1f);

    [Header("Item Detail Panel UI")]
    public GameObject detailPanel;
    public Image detailItemImage;
    public TMP_Text detailNameText;
    public TMP_Text detailDescText;

    [Header("🧪 시작 테스트용 아이템 목록")]
    [SerializeField] private List<Item> testStartItems = new List<Item>();

    private List<Item> masterItemList = new List<Item>();
    private int currentFilterType = 0;

    public override void Spawned()
    {
        Debug.Log($"[NetworkInventory] Spawned 호출. Object.Id={Object.Id}, HasInputAuthority={HasInputAuthority}, InputAuthority={Object.InputAuthority}, Runner.LocalPlayer={Runner.LocalPlayer}");
        if (!HasInputAuthority)
        {
            enabled = false;
            Debug.Log($"[NetworkInventory] 비활성화됨 (내 소유 아님). Object.Id={Object.Id}");
            return;
        }

        Instance = this;
        Debug.Log($"✅ [NetworkInventory] Instance로 등록됨! Object.Id={Object.Id}, GetInstanceID={GetInstanceID()}");

        if (detailPanel != null) detailPanel.SetActive(false);

        foreach (Item testItem in testStartItems)
        {
            if (testItem != null)
            {
                AddItem(testItem);
            }
        }

        FilterItems(0);
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isActive = inventoryPanel.activeSelf;
            inventoryPanel.SetActive(!isActive);
            Debug.Log($"[Inventory] 인벤토리 창이 {(inventoryPanel.activeSelf ? "열렸습니다" : "닫혔습니다")}");
        }
        else
        {
            Debug.LogWarning("❌ inventoryPanel 변수에 UI 패널이 연결되지 않았습니다!");
        }
    }

    public void AddItem(Item item)
    {
        if (item == null) return;

        Item existingItem = masterItemList.Find(x => x != null && x.getItemName() == item.getItemName());
        if (existingItem != null)
        {
            int amountToAdd = item.get_Item_Count() > 0 ? item.get_Item_Count() : 1;
            existingItem.add_Item_Count(amountToAdd);
        }
        else
        {
            Item newItem = Instantiate(item);
            if (newItem.get_Item_Count() <= 0) newItem.set_Item_Count(1);
            masterItemList.Add(newItem);
        }

        FilterItems(currentFilterType);
    }

    public void AddItemByName(string itemName)
    {
        Item foundItem = Resources.Load<Item>($"Items/{itemName}");

        if (foundItem != null)
        {
            Item itemInstance = Instantiate(foundItem);
            itemInstance.set_Item_Count(1);
            AddItem(itemInstance);
        }
        else
        {
            Debug.LogError($"❌ [SO 찾기 실패] Resources/Items/ 폴더 안에 '{itemName}' 이름의 Item SO가 없습니다!");
        }
    }

    public int GetItemCount(string itemName)
    {
        int count = 0;
        foreach (Item item in masterItemList)
        {
            if (item != null && item.getItemName() == itemName)
            {
                count += item.get_Item_Count();
            }
        }
        return count;
    }

    public void ConsumeItem(string itemName, int amountToConsume)
    {
        Item targetItem = masterItemList.Find(x => x != null && x.getItemName() == itemName);

        if (targetItem == null) return;

        int currentCount = targetItem.get_Item_Count();

        if (currentCount > amountToConsume)
        {
            targetItem.add_Item_Count(-amountToConsume);
        }
        else
        {
            masterItemList.Remove(targetItem);
            Destroy(targetItem);
        }

        FilterItems(currentFilterType);
    }

    public void FilterItems(int filterType)
    {
        if (itemSlots == null || itemSlots.Length == 0) return;

        currentFilterType = filterType;

        ShowItemDetails(null);

        List<Item> filteredList = new List<Item>();

        foreach (Item item in masterItemList)
        {
            if (item == null) continue;
            ItemType type = item.GetItemType();
            bool isMatch = false;

            switch (filterType)
            {
                case 0:
                    if (type == ItemType.Material) isMatch = true;
                    break;
                case 1:
                    if (type == ItemType.Tool) isMatch = true;
                    break;
                case 2:
                    if (type == ItemType.CookedFood || type == ItemType.RawFood || type == ItemType.Water) isMatch = true;
                    break;
            }

            if (isMatch) filteredList.Add(item);
        }

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null) continue;

            itemSlots[i].gameObject.SetActive(true);

            if (i < filteredList.Count)
            {
                itemSlots[i].SetSlot(filteredList[i], false);
            }
            else
            {
                itemSlots[i].Clear_Slot();
            }
        }

        UpdateButtonColors(filterType);
    }

    private void UpdateButtonColors(int selectedIndex)
    {
        if (filterButtons == null || filterButtons.Length == 0) return;

        for (int i = 0; i < filterButtons.Length; i++)
        {
            if (filterButtons[i] != null)
            {
                Image buttonImage = filterButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = (i == selectedIndex) ? selectedColor : defaultColor;
                }
            }
        }
    }

    public void ShowItemDetails(Item item)
    {
        if (item == null)
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            return;
        }

        if (detailPanel == null)
        {
            Transform found = transform.Find("DetailPanel");
            if (found != null) detailPanel = found.gameObject;
        }

        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("🚨 detailPanel 인스펙터 연결을 확인해 주세요!");
            return;
        }

        if (detailItemImage != null) detailItemImage.sprite = item.get_Item_Sprite();
        if (detailNameText != null) detailNameText.text = item.getItemName();
        if (detailDescText != null) detailDescText.text = item.getItemDescription();

        Debug.Log($"🔎 [디테일 패널 출력] 아이템: {item.getItemName()}");
    }

    // =========================================================================
    // 🌟 슬롯 아이템 스폰 요청 RPC (문자열 기반으로 안전하게 처리)
    // =========================================================================

    // =========================================================================
    // 🌟 슬롯 아이템 스폰 요청 RPC (어느 손인지 isLeftHand 추가)
    // =========================================================================

    public void RequestSpawnItemByName(string itemName, Vector3 position, Quaternion rotation, bool isGroundItem, PlayerRef requestingPlayer, bool isLeftHand)
    {
        if (Runner != null && Runner.IsRunning)
        {
            RPC_RequestSpawnItem(itemName, position, rotation, isGroundItem, requestingPlayer, isLeftHand);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSpawnItem(string itemName, Vector3 position, Quaternion rotation, bool isGroundItem, PlayerRef requestingPlayer, bool isLeftHand)
    {
        if (!Runner.IsRunning) return;

        string cleanItemName = itemName.Replace("(Clone)", "").Trim();

        Item foundItem = Resources.Load<Item>($"Items/{cleanItemName}");
        if (foundItem == null || foundItem.GetPrefab3D() == null)
        {
            Debug.LogError($"❌ [Spawn 실패] Resources/Items/ 폴더에서 '{cleanItemName}' Item SO 또는 3D 프리팹을 찾을 수 없습니다.");
            return;
        }

        GameObject prefabToSpawn = foundItem.GetPrefab3D();
        PlayerRef authority = isGroundItem ? PlayerRef.None : requestingPlayer;

        NetworkObject spawnedNetObj = Runner.Spawn(prefabToSpawn, position, rotation, authority);

        if (spawnedNetObj != null)
        {
            // 🌟 Host는 자기 자신이 스폰한 이 오브젝트를 즉시 렌더링하므로, 여기서 바로 IsGrabbed를
            //    확정해야 Host 화면에서 스폰과 동시에 손을 따라 움직인다. 이걸 안 하면 클라이언트가
            //    로컬 SelectEnter → RPC_RequestSetGrabState를 보내고 Host가 받을 때까지(RPC 왕복 1회)
            //    Host 화면에서는 아이템이 스폰 위치에 그대로 멈춰 있는 것처럼 보인다.
            if (!isGroundItem)
            {
                var grabItem = spawnedNetObj.GetComponentInChildren<NetworkGrabItem>();
                if (grabItem != null)
                {
                    grabItem.ServerInitGrab(requestingPlayer, isLeftHand);
                }
            }

            RPC_OnItemSpawned(spawnedNetObj, requestingPlayer, isGroundItem, isLeftHand);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnItemSpawned(NetworkObject spawnedNetObj, PlayerRef requestingPlayer, bool isGroundItem, bool isLeftHand)
    {
        if (spawnedNetObj == null) return;

        // 본인(로컬 플레이어)에게만 손 쥐기 처리
        if (!isGroundItem && Runner.LocalPlayer == requestingPlayer)
        {
            var grabInteractable = spawnedNetObj.GetComponentInChildren<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                var interactionManager = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();

                // 내 캐릭터 하위에서 인터랙터 탐색
                var myInteractors = GetComponentsInChildren<XRRayInteractor>(true);
                if (myInteractors == null || myInteractors.Length == 0)
                {
                    myInteractors = transform.root.GetComponentsInChildren<XRRayInteractor>(true);
                }

                XRRayInteractor targetHand = null;

                // 🌟 누른 손(Left vs Right)을 이름으로 정확히 타겟팅
                foreach (var interactor in myInteractors)
                {
                    string objName = interactor.gameObject.name.ToLower();
                    if (isLeftHand && objName.Contains("left"))
                    {
                        targetHand = interactor;
                        break;
                    }
                    else if (!isLeftHand && objName.Contains("right"))
                    {
                        targetHand = interactor;
                        break;
                    }
                }

                // 만약 특정 손을 못 찾았다면 비어있는 손 아무거나 폴백
                if (targetHand == null)
                {
                    foreach (var interactor in myInteractors)
                    {
                        if (interactor != null && !interactor.hasSelection)
                        {
                            targetHand = interactor;
                            break;
                        }
                    }
                }

                if (targetHand != null)
                {
                    // 위치/물리 초기화 후 정확한 손에 집기
                    spawnedNetObj.transform.position = targetHand.transform.position;
                    spawnedNetObj.transform.rotation = targetHand.transform.rotation;

                    var rb = spawnedNetObj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    interactionManager?.SelectEnter((IXRSelectInteractor)targetHand, (IXRSelectInteractable)grabInteractable);
                }
            }
        }
    }
}