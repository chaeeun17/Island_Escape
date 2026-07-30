using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Inventory: MonoBehaviour 
{
    public SLot[] itemSlots;
    public static Inventory Instance;

    [Header("Filter Buttons UI")]
    public Button[] filterButtons;     // 인스펙터에서 할당할 버튼 배열 (0:전체, 1:재료, 2:음식, 3:도구)
    public Color selectedColor = Color.yellow;  // 선택되었을 때의 색상 
    public Color defaultColor = new Color(1.0f, 0.8f, 0.3f, 1f);

    [Header("Item Detail Panel UI")]
    public GameObject detailPanel;         // 상세 정보 패널 배경 (전체 끄고 켜기 용도)
    public Image detailItemImage;          // 띄울 아이템 이미지
    public TMP_Text detailNameText;        // 아이템 이름 텍스트
    public TMP_Text detailDescText;        // 아이템 설명 텍스트

    public void AddItem(Item item)
    { 
        if(itemSlots != null) 
        {
            //1. check for an existing item of the same type
            for(int i = 0; i<itemSlots.Length; i++)
            {
                if (itemSlots[i].isin() && itemSlots[i].getItem().getItemName()==item.getItemName())
                {
                    itemSlots[i].item.add_Item_Count(item.get_Item_Count());
                    itemSlots[i].Update_Count_text();
                    Debug.Log("Increased count of " + item.getItemName() + " in slot " + i);
                    return;
                }
            }
            //2. if no existing item found, add to the first empty slot
            for(int i =0; i< itemSlots.Length; i++)
            {
                if (!itemSlots[i].isin())
                {
                    Item newItem = Instantiate(item);

                    //Add the new item instance to the inventory slot
                    itemSlots[i].SetSlot(newItem, true);

                    Debug.Log("New item added to slot" + i);
                    return; //exit the method after adding the new item
                }
            }


            //if no empty slot found
            Debug.Log("Inventory is full");
        }
    }

    private void Awake()
    {
        Instance = this;
        if (detailPanel != null) detailPanel.SetActive(false);
    }
    //  이름으로 아이템 총 개수 찾기 
    public int GetItemCount(string itemName)
    {
        int count = 0;
        if (itemSlots == null) return 0;
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].isin() && itemSlots[i].getItem().getItemName() == itemName)
            {
                count += itemSlots[i].getItem().get_Item_Count();
            }
        }
        return count;
    }

    // 이름으로 아이템 소모하기 
    public void ConsumeItem(string itemName, int amountToConsume)
    {
        if (itemSlots == null) return;
        int remaining = amountToConsume;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].isin() && itemSlots[i].getItem().getItemName() == itemName)
            {
                int available = itemSlots[i].item.get_Item_Count();

                if (available > remaining)
                {
                    itemSlots[i].item.add_Item_Count(-remaining);
                    itemSlots[i].Update_Count_text();
                    return; // 소모 완료
                }
                else
                {
                    remaining -= available;
                    Destroy(itemSlots[i].item); // 메모리 누수 방지
                    itemSlots[i].Clear_Slot();

                    if (remaining <= 0) return; // 소모 완료
                }
            }
        }
    } 

    public void FilterItems(int filterType)
    {
        if (itemSlots == null) return;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].isin())
            {
                itemSlots[i].gameObject.SetActive(false);

                if(filterType == 0) itemSlots[i].gameObject.SetActive(true);
                continue;
            }
            ItemType currentItemType = itemSlots[i].getItem().GetItemType();
            bool souldShow = false;

            switch(filterType)
            {
                case 0:
                    souldShow = true; // Show all items
                    break;

                case 1:
                    if(currentItemType == ItemType.Material || currentItemType == ItemType.RawFood)
                        souldShow = true;
                     break;

                case 2:
                    if(currentItemType == ItemType.CookedFood)
                        souldShow = true;
                    break;
                case 3:
                    if (currentItemType == ItemType.General)
                        souldShow = true;
                    break;

            }
            itemSlots[i].gameObject.SetActive(souldShow);
        }
        UpdateButtonColors(filterType);
    }
    private void UpdateButtonColors(int selectedIndex)
    {
        if (filterButtons == null || filterButtons.Length == 0) return;

        for(int i =0;i<filterButtons.Length; i++)
        {
            if (filterButtons[i] != null)
            {
                Image buttonImage = filterButtons[i].GetComponent<Image>();
                if(buttonImage !=null)
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
            // 빈 슬롯을 클릭하면 패널 숨김
            if (detailPanel != null) detailPanel.SetActive(false);
            return;
        }

        if (detailPanel != null) detailPanel.SetActive(true);

        if (detailItemImage != null) detailItemImage.sprite = item.get_Item_Sprite();
        if (detailNameText != null) detailNameText.text = item.getItemName();
        if (detailDescText != null) detailDescText.text = item.getItemDescription();
    }

}


