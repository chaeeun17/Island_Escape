using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.ParticleSystemJobs;

public class SLot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public PointerEventData.InputButton dragButton;
    public bool isResultSlot = false;
    public System.Action OnSlotChanged;

    public Item item;
    [SerializeField] private Image slot_Item_Image;
    [SerializeField] private Image Event_Image;
    [SerializeField] private TMP_Text slot_Item_Count_TXT;
    private bool isIn = false;

    private Canvas canvas;
    private GameObject draggingIcon;
    private RectTransform draggingPlane;

    void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
        draggingPlane = canvas.transform as RectTransform;
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

    private void Slot_Update(Item get_item)
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
        Color color = setImage.color;
        color.a = alpha;
        setImage.color = color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetColor(1f, Event_Image);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(0f, Event_Image);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (this.item != null && slot_Item_Image.sprite != null)
        {
            dragButton = eventData.button;
            if (dragButton == PointerEventData.InputButton.Right && this.item.get_Item_Count() <= 1)
            {
                dragButton = PointerEventData.InputButton.Left;
            }

            draggingIcon = new GameObject("dragging icon");
            draggingIcon.transform.SetParent(canvas.transform, false);
            draggingIcon.transform.SetAsLastSibling();
            var image = draggingIcon.AddComponent<Image>();
            image.sprite = slot_Item_Image.sprite;
            image.SetNativeSize();
            image.raycastTarget = false;

            RectTransform slotRectTransform = GetComponent<RectTransform>();
            RectTransform imageRectTransform = image.GetComponent<RectTransform>();
            imageRectTransform.sizeDelta = slotRectTransform.sizeDelta;

            slot_Item_Image.color = new Color(slot_Item_Image.color.r, slot_Item_Image.color.g, slot_Item_Image.color.b, 0.5f);

            SetDraggedPosition(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingIcon != null)
        {
            SetDraggedPosition(eventData);
        }
    }

    public void SetDraggedPosition(PointerEventData eventData)
    {
        if (eventData.pointerEnter != null && draggingPlane != null)
        {
            Vector2 globalMousePos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(draggingPlane, eventData.position, eventData.pressEventCamera, out globalMousePos))
            {
                draggingIcon.transform.localPosition = globalMousePos;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingIcon != null)
        {
            Destroy(draggingIcon);
        }

        if (item != null)
        {
            slot_Item_Image.color = new Color(slot_Item_Image.color.r, slot_Item_Image.color.g, slot_Item_Image.color.b, 1f);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isResultSlot) return;

        if (eventData.pointerDrag != null)
        {
            SLot draggedSlot = eventData.pointerDrag.GetComponent<SLot>();

            if (draggedSlot == this || draggedSlot == null || draggedSlot.item == null) return;

            int moveCount = (draggedSlot.dragButton == PointerEventData.InputButton.Right) ? 1 : draggedSlot.item.get_Item_Count();

            // 1. 빈 슬롯에 드롭할 때
            if (this.item == null)
            {
                if (moveCount == draggedSlot.item.get_Item_Count())
                {
                    this.Slot_Update(draggedSlot.item);
                    draggedSlot.Clear_Slot();
                }
                else
                {
                    // ScriptableObject 복제 (Instance 생성)
                    Item splitItem = Instantiate(draggedSlot.item);
                    splitItem.set_Item_Count(moveCount);
                    this.Slot_Update(splitItem);

                    draggedSlot.item.add_Item_Count(-moveCount);
                }
            }
            // 2. 같은 아이템에 드롭할 때 (합치기)
            else if (this.item != null && this.item.isSameItem(draggedSlot.item))
            {
                this.item.add_Item_Count(moveCount);

                if (moveCount == draggedSlot.item.get_Item_Count())
                {
                    Destroy(draggedSlot.item);
                    draggedSlot.Clear_Slot();
                }
                else
                {
                    draggedSlot.item.add_Item_Count(-moveCount);
                }
            }
            // 3. 다른 아이템과 자리 바꾸기
            else
            {
                if (draggedSlot.dragButton == PointerEventData.InputButton.Left)
                {
                    Item tempItem = this.item;
                    this.Slot_Update(draggedSlot.item);
                    draggedSlot.Slot_Update(tempItem);
                }
            }

            this.Update_Count_text();
            draggedSlot.Update_Count_text();
        }
    }

    public void SetSlotItem(Item setitem) { item = setitem; }
    public void setSlotImage(Image SlotImage) { slot_Item_Image = SlotImage; }
    public void Set__isIn(bool IsItem) { isIn = IsItem; }
    public void set_Count_txt(int count) { slot_Item_Count_TXT.text = count.ToString(); }
    public bool isin() { return isIn; }
    public Item getItem() { return item; }
}