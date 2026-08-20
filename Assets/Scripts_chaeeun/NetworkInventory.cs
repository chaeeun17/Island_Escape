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
        // 내 조종 권한이 없는 멀티 상대방 캐릭터라면 스크립트 비활성화
        if (!HasInputAuthority)
        {
            enabled = false;
            return;
        }

        // 내 캐릭터의 인벤토리만 싱글톤으로 등록
        Instance = this;

        if (detailPanel != null) detailPanel.SetActive(false);

        // 테스트 아이템 지급
        foreach (Item testItem in testStartItems)
        {
            if (testItem != null)
            {
                AddItem(testItem);
                Debug.Log($"🧪 [테스트 아이템 지급 완료] {testItem.getItemName()}");
            }
        }

        // 시작 시 0번(Material) 탭 화면 갱신
        FilterItems(0);
    }

    // 🌟 아이템 추가
    /*
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
    }*/

    public bool AddItem(Item item)
    { 
        if (item == null) return false;

        // 1. 데이터 원본(masterItemList)에서 기존 아이템 수량 증가 (중첩 가능)
        Item existingItem = masterItemList.Find(x => x != null && x.getItemName() == item.getItemName());
        if (existingItem != null)
        {
            int amountToAdd = item.get_Item_Count() > 0 ? item.get_Item_Count() : 1;
            existingItem.add_Item_Count(amountToAdd);
            FilterItems(currentFilterType);
            return true;
        }

        // 2. 🌟 [핵심 방어] 신규 아이템 추가 전, 해당 탭의 슬롯이 꽉 찼는지 검사
        int categoryItemCount = GetCategoryItemCount(item.GetItemType());
        int maxSlotCount = itemSlots != null ? itemSlots.Length : 0;

        if (categoryItemCount >= maxSlotCount)
        {
            Debug.LogWarning($"⚠️ [인벤토리 가득 참] 슬롯이 부족하여 '{item.getItemName()}'을(를) 넣을 수 없습니다!");
            return false; // 인벤토리 꽉 참으로 추가 실패
        }

        // 3. 신규 아이템 추가
        Item newItem = Instantiate(item);
        if (newItem.get_Item_Count() <= 0) newItem.set_Item_Count(1);
        masterItemList.Add(newItem);

        // UI 갱신
        FilterItems(currentFilterType);
        return true;
    }

    // 🌟 해당 아이템 타입이 속한 카테고리의 현재 아이템 종류 개수 계산
    private int GetCategoryItemCount(ItemType type)
    {
        int count = 0;
        foreach (Item item in masterItemList)
        {
            if (item == null) continue;
            ItemType t = item.GetItemType();

            if (type == ItemType.Material && t == ItemType.Material) count++;
            else if (type == ItemType.Tool && t == ItemType.Tool) count++;
            else if ((type == ItemType.CookedFood || type == ItemType.RawFood || type == ItemType.Water) &&
                     (t == ItemType.CookedFood || t == ItemType.RawFood || t == ItemType.Water)) count++;
        }
        return count;
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

    // 🌟 [수정 완료] 데이터 원본(masterItemList)에서 아이템 총수량 조회
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

    // 🌟 [수정 완료] 아이템 소모 (데이터 원본에서 깎고 UI 자동 재정렬)
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
                    if (type == ItemType.CookedFood || type == ItemType.RawFood|| type == ItemType.Water) isMatch = true;
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