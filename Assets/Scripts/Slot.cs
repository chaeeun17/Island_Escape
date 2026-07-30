using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SLot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public bool isResultSlot = false;
    public System.Action OnSlotChanged;

    public Item item;
    [SerializeField] private Image slot_Item_Image;
    [SerializeField] private Image Event_Image;
    [SerializeField] private TMP_Text slot_Item_Count_TXT;
    private bool isIn = false;
    public static Item currentlyHeldItem;
    public static SLot sourceSlot;

    void Awake()
    {
        slot_Item_Count_TXT.text = "";
    }

    public void SetSlot(Item setitem, bool is_New_Item)
    {
        if (setitem != null)
        {
            get_Item(setitem);
            if (is_New_Item)
            {
                setitem.add_Item_Count(1);
            }
            Slot_Update(setitem);
        }
        Update_Count_text();
    }

    private void get_Item(Item get_item)
    {
        get_item.set_isGet(true);
    }

    public void Slot_Update(Item get_item)
    {
        this.item = get_item;
        this.slot_Item_Image.sprite = get_item.get_Item_Sprite();
        SetColor(1f, this.slot_Item_Image);
        this.isIn = true;
        get_item.set_isGet(true);
    }

    public void Clear_Slot()
    {
        SetColor(0f, slot_Item_Image);
        this.item = null;
        this.slot_Item_Image.sprite = null;
        this.isIn = false;
        Update_Count_text();
    }

    public void Update_Count_text()
    {
        if (item != null && item.get_Item_Count() > 1)
        {
            slot_Item_Count_TXT.text = item.get_Item_Count().ToString();
        }
        else
        {
            slot_Item_Count_TXT.text = "";
        }
    }

    public void addCount(int count)
    {
        if (item != null)
        {
            item.add_Item_Count(count);
            Update_Count_text();
        }
    }

    private void SetColor(float alpha, Image setImage)
    {
        if (setImage != null)
        {
            Color color = setImage.color;
            color.a = alpha;
            setImage.color = color;
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetColor(1f, Event_Image);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(0f, Event_Image);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (this.item != null)
        {
            Inventory.Instance.ShowItemDetails(this.item);
        }
        else
        {
            Inventory.Instance.ShowItemDetails(null); // 빈 슬롯 클릭 시 패널을 끕니다.
        }

        if (currentlyHeldItem == null)
        {
            if (this.item != null)
            {
                currentlyHeldItem = this.item;
                sourceSlot = this;

                SetColor(0.5f, slot_Item_Image);
            }
        }
        else
        {
            if (this == sourceSlot)
            {
                SetColor(1f, sourceSlot.slot_Item_Image); 
                ClearHeldItem();
                return;
            }

            if (isResultSlot) return;

            if (this.item == null)
            {
                this.Slot_Update(currentlyHeldItem);
                sourceSlot.Clear_Slot();
            }
            else if (this.item.isSameItem(currentlyHeldItem))
            {
                this.item.add_Item_Count(currentlyHeldItem.get_Item_Count());

          
                if (sourceSlot.item != this.item)
                {
                    Destroy(sourceSlot.item);
                }
                sourceSlot.Clear_Slot();
            }
            else
            {
                Item tempItem = this.item;
                this.Slot_Update(currentlyHeldItem);
                sourceSlot.Slot_Update(tempItem);
                SetColor(1f, sourceSlot.slot_Item_Image);
            }

            this.Update_Count_text();
            if (sourceSlot != null)
            {
                sourceSlot.Update_Count_text();
            }

            ClearHeldItem();
        }
    }

    private void ClearHeldItem()
    {
        currentlyHeldItem = null;
        sourceSlot = null;
    }

    public void SetSlotItem(Item setitem) { item = setitem; }
    public void setSlotImage(Image SlotImage) { slot_Item_Image = SlotImage; }
    public void Set__isIn(bool IsItem) { isIn = IsItem; }
    public void set_Count_txt(int count) { slot_Item_Count_TXT.text = count.ToString(); }
    public bool isin() { return isIn; }
    public Item getItem() { return item; }
}