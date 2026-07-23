using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
//using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.ParticleSystemJobs;

public class SLot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IDropHandler
{
    public PointerEventData.InputButton dragButton;
    public bool isResultSlot = false;
    public System.Action OnSlotChanged;

    public Item item;
    [SerializeField] private Image slot_Item_Image;//아이템 이미지
    [SerializeField] private Image Event_Image;//마우스 이펙트
    [SerializeField] private TMP_Text slot_Item_Count_TXT;//아이템 갯수
    private bool isIn = false; //아이템이 들어이있는지 여부

  
 



    void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
        draggingPlane = canvas.transform as RectTransform;
        slot_Item_Count_TXT.text = "";

    }

    public void SetSlot(Item setitem, bool is_New_Item)
    {
        if(setitem != null) 
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
        this.item = get_item; // 1. 슬롯에 아이템 데이터를 저장합니다.
        this.slot_Item_Image.sprite = get_item.get_Item_Sprite(); // 2. 슬롯의 이미지를 아이템 이미지로 바꿉니다.
        SetColor(1f, this.slot_Item_Image); // 3. 투명도를 100%로 만들어서 이미지가 화면에 보이게 합니다.
        this.isIn = true; // 4. 이제 슬롯 안에 아이템이 들어있다고(true) 상태를 변경합니다.

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
        if(item !=null && item.get_Item_Count() > 1)
        {
            slot_Item_Count_TXT.text = item.get_Item_Count().ToString();
            Debug.Log(item.get_Item_Count());
        }
        else
        {
            {
                slot_Item_Count_TXT.text = "";
            }

    }
    }

    public void addCount(int count)
    {
        if(item !=null)
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
        SetColor(1f,Event_Image);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(0f, Event_Image);
    }

    private Canvas canvas;
    private GameObject draggingIcon;
    private RectTransform draggingPlane;


    public void OnBeginDrag(PointerEventData eventData)
    {
        if(this.item !=null && slot_Item_Image.sprite!=null)
        {
            dragButton = eventData.button;
            if (dragButton == PointerEventData.InputButton.Right && this.item.get_Item_Count() <= 1)
            {
                dragButton = PointerEventData.InputButton.Left;
            }

            draggingIcon = new GameObject("dragging icon");
            draggingIcon.transform.SetParent(canvas.transform, false);


            draggingIcon = new GameObject("dragging icon");
            draggingIcon.transform.SetParent(canvas.transform, false);
            draggingIcon.transform.SetAsLastSibling();
            var image = draggingIcon.AddComponent<Image>() ;
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
        if(draggingIcon != null)
        {
            SetDraggedPosition(eventData);
        }
    }

    public void SetDraggedPosition(PointerEventData eventData)
    {
        if(eventData.pointerEnter != null && draggingPlane !=null)
        {
            Vector2 globalMousePos;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(draggingPlane, eventData.position, eventData.pressEventCamera, out globalMousePos))
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

        if(item !=null)
        {
            slot_Item_Image.color = new Color(slot_Item_Image.color.r, slot_Item_Image.color.g, slot_Item_Image.color.b, 1f);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isResultSlot) return; // 결과 슬롯 차단 (제작대용)

        if (eventData.pointerDrag != null)
        {
            SLot draggedSlot = eventData.pointerDrag.GetComponent<SLot>();

            // 오류 방지 방어 코드 (자기 자신에게 놓거나, 드래그한 슬롯이 비어있으면 무시)
            if (draggedSlot == this || draggedSlot == null || draggedSlot.item == null) return;

            // ⭐ 이동할 개수 결정: 우클릭이면 1개, 좌클릭이면 전체 개수
            int moveCount = (draggedSlot.dragButton == PointerEventData.InputButton.Right) ? 1 : draggedSlot.item.get_Item_Count();

            // 1. 빈 슬롯에 드롭할 때
            if (this.item == null)
            {
                if (moveCount == draggedSlot.item.get_Item_Count())
                {
                    // 좌클릭: 전체 이동
                    this.Slot_Update(draggedSlot.item);
                    draggedSlot.Clear_Slot();
                }
                else
                {
                    // 우클릭: 1개만 분리해서 새 슬롯에 넣기
                    Item splitItem = Instantiate(draggedSlot.item); // 아이템 완벽 복제
                    splitItem.set_Item_Count(moveCount); // 개수를 1개로 설정
                    this.Slot_Update(splitItem); // 현재 슬롯에 띄우기

                    draggedSlot.item.add_Item_Count(-moveCount); // 원래 슬롯에서 1개 빼기
                }
            }
            // 2. 같은 아이템에 드롭할 때 (합치기)
            else if (this.item != null && this.item.isSameItem(draggedSlot.item))
            {
                this.item.add_Item_Count(moveCount);

                if (moveCount == draggedSlot.item.get_Item_Count())
                {
                    // 좌클릭: 전체 이동해서 합쳤으므로 원본 파괴
                    Destroy(draggedSlot.item.gameObject);
                    draggedSlot.Clear_Slot();
                }
                else
                {
                    // 우클릭: 1개만 빼서 합쳤으므로 원본 개수만 1 차감
                    draggedSlot.item.add_Item_Count(-moveCount);
                }
            }
            // 3. 다른 아이템과 자리 바꾸기
            else
            {
                // 우클릭으로 분리할 때는 아이템 자리를 바꾸지 못하도록 막음
                if (draggedSlot.dragButton == PointerEventData.InputButton.Left)
                {
                    Item tempItem = this.item;
                    this.Slot_Update(draggedSlot.item);
                    draggedSlot.Slot_Update(tempItem);
                }
            }

            // UI 최종 갱신
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
