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
    [Header("UI Panel")]
    public GameObject uiPanel;
    [Header("Filter Buttons UI")]
    public Button[] filterButtons;     // ÀÎ½ºÆåÅÍ¿¡¼­ ÇÒ´çÇÒ ¹öÆ° ¹è¿­ (0:ÀüÃ¼, 1:Àç·á, 2:À½½Ä, 3:µµ±¸)
    public Color selectedColor = Color.yellow;  // ¼±ÅÃµÇ¾úÀ» ¶§ÀÇ »ö»ó 
    public Color defaultColor = new Color(1.0f, 0.8f, 0.3f, 1f);

    [Header("Item Detail Panel UI")]
    public GameObject detailPanel;         // »ó¼¼ Á¤º¸ ÆÐ³Î ¹è°æ (ÀüÃ¼ ²ô°í ÄÑ±â ¿ëµµ)
    public Image detailItemImage;          // ¶ç¿ï ¾ÆÀÌÅÛ ÀÌ¹ÌÁö
    public TMP_Text detailNameText;        // ¾ÆÀÌÅÛ ÀÌ¸§ ÅØ½ºÆ®
    public TMP_Text detailDescText;        // ¾ÆÀÌÅÛ ¼³¸í ÅØ½ºÆ®

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
    //  ÀÌ¸§À¸·Î ¾ÆÀÌÅÛ ÃÑ °³¼ö Ã£±â 
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

    // ÀÌ¸§À¸·Î ¾ÆÀÌÅÛ ¼Ò¸ðÇÏ±â 
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
                    return; // ¼Ò¸ð ¿Ï·á
                }
                else
                {
                    remaining -= available;
                    Destroy(itemSlots[i].item); // ¸Þ¸ð¸® ´©¼ö ¹æÁö
                    itemSlots[i].Clear_Slot();

                    if (remaining <= 0) return; // ¼Ò¸ð ¿Ï·á
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
            // ºó ½½·ÔÀ» Å¬¸¯ÇÏ¸é ÆÐ³Î ¼û±è
            if (detailPanel != null) detailPanel.SetActive(false);
            return;
        }

        if (detailPanel != null) detailPanel.SetActive(true);

        if (detailItemImage != null) detailItemImage.sprite = item.get_Item_Sprite();
        if (detailNameText != null) detailNameText.text = item.getItemName();
        if (detailDescText != null) detailDescText.text = item.getItemDescription();
    }

<<<<<<< Updated upstream
=======
    public void ToggleInventory()
    {
        if (uiPanel != null)
        {
            // í˜„ìž¬ êº¼ì ¸ìžˆìœ¼ë©´ ì¼œê³ , ì¼œì ¸ìžˆìœ¼ë©´ ë•ë‹ˆë‹¤.
            uiPanel.SetActive(!uiPanel.activeSelf);
        }
    }
>>>>>>> Stashed changes

}


