using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class NetworkInventory : NetworkBehaviour
{
    public SLot[] itemSlots;

    // 🌟 [핵심] 정적 싱글톤 - 내 조종 권한(HasInputAuthority)이 있는 캐릭터의 인벤토리만 할당!
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

    // 🌟 아이템 원본 데이터 관리용 리스트 및 현재 필터 탭
    private List<Item> masterItemList = new List<Item>();
    private int currentFilterType = 0;

    public override void Spawned()
    {
        // 🌟 수정됨: 권한 체크를 잠시 주석 처리하거나, 무조건 Instance를 등록하도록 변경
        
        if (!HasInputAuthority)
        {
            enabled = false;
            return;
        }
       

        // 내 캐릭터의 인벤토리만 싱글톤으로 등록 (일단 무조건 등록)
        Instance = this;
        Debug.Log("✅ [Inventory Debug] 싱글톤 Instance가 강제로 등록되었습니다!");

        if (detailPanel != null) detailPanel.SetActive(false);

        // 테스트 아이템 지급
        foreach (Item testItem in testStartItems)
        {
            if (testItem != null)
            {
                AddItem(testItem);
            }
        }

        FilterItems(0);
    }

    // 🌟 인벤토리 창 껐다 켜기 (최상위 오브젝트 말고 UI 패널만 조작)
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
            Debug.LogWarning("❌ inventoryPanel 변수에 UI 패널(inventory_panel)이 연결되지 않았습니다!");
        }
    }

    // 🌟 아이템 추가
    public void AddItem(Item item)
    {
        if (item == null) return;

        // 1. 데이터 원본(masterItemList)에서 기존 아이템 수량 증가
        Item existingItem = masterItemList.Find(x => x != null && x.getItemName() == item.getItemName());
        if (existingItem != null)
        {
            int amountToAdd = item.get_Item_Count() > 0 ? item.get_Item_Count() : 1;
            existingItem.add_Item_Count(amountToAdd);
        }
        else
        {
            // 2. 신규 아이템 추가 (새 인스턴스 생성)
            Item newItem = Instantiate(item);
            if (newItem.get_Item_Count() <= 0) newItem.set_Item_Count(1);
            masterItemList.Add(newItem);
        }

        // UI 갱신
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

    // 🌟 데이터 원본(masterItemList)에서 아이템 총수량 조회
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

    // 🌟 아이템 소모 (데이터 원본에서 깎고 UI 자동 재정렬)
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
            // 수량이 다 떨어지면 원본 리스트에서 제거 후 메모리 해제
            masterItemList.Remove(targetItem);
            Destroy(targetItem);
        }

        // UI 자동 재정렬 갱신
        FilterItems(currentFilterType);
    }

    // 🌟 탭 이동 시 0번 슬롯부터 앞쪽으로 빽빽하게 재배치
    public void FilterItems(int filterType)
    {
        if (itemSlots == null || itemSlots.Length == 0) return;

        currentFilterType = filterType;

        ShowItemDetails(null);

        // 선택된 탭 조건에 맞는 아이템 추출
        List<Item> filteredList = new List<Item>();

        foreach (Item item in masterItemList)
        {
            if (item == null) continue;
            ItemType type = item.GetItemType();
            bool isMatch = false;

            switch (filterType)
            {
                case 0: // Material 탭
                    if (type == ItemType.Material) isMatch = true;
                    break;
                case 1: // Tool 탭 
                    if (type == ItemType.Tool) isMatch = true;
                    break;
                case 2: // Food 탭
                    if (type == ItemType.CookedFood || type == ItemType.RawFood || type == ItemType.Water) isMatch = true;
                    break;
            }

            if (isMatch) filteredList.Add(item);
        }

        // 0번 슬롯부터 필터링된 아이템 차곡차곡 배치
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null) continue;

            itemSlots[i].gameObject.SetActive(true); // 칸 레이아웃 유지

            if (i < filteredList.Count)
            {
                itemSlots[i].SetSlot(filteredList[i], false);
            }
            else
            {
                itemSlots[i].Clear_Slot(); // 남은 빈 칸 처리
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

    // 🌟 아이템 상세 정보 패널 출력
    public void ShowItemDetails(Item item)
    {
        if (item == null)
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            return;
        }

        // 부모 인벤토리에 detailPanel 참조가 비어있다면 자동 바인딩 (안전장치)
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
}