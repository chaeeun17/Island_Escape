using UnityEngine;
using UnityEngine.InputSystem;


public class VRItemPlacer: MonoBehaviour
{
    [Header("VR 설치 설정")]
    public Transform controllerTransform;
    public InputActionReference placeAction;
    public LayerMask groundLayer;

    private void OnEnable()
    {
        if (placeAction != null) placeAction.action.Enable();
    }

    private void OnDisable()
    {
        if(placeAction != null) placeAction.action.Disable();
    }

  
    private void Update()
    {
        //설치 버튼을 이번 프레임에 눌렀는지 확인
        if(placeAction!=null && placeAction.action.WasPressedThisFrame())
        {
            if(SLot.currentlyHeldItem !=null)
            {
                TryPlaceItem(SLot.currentlyHeldItem);

            }
        }
    }

    private void TryPlaceItem(Item itemToPlace)
    {
        GameObject prefab = itemToPlace.GetPrefab3D();
        if(prefab == null)
        {
            Debug.Log("이 아이템은 바닥에 설치할 수 없습니다.");
            return;
        }

        if(Physics.Raycast(controllerTransform.position, controllerTransform.forward, out RaycastHit hit, 10f, groundLayer))
        {
            Instantiate(prefab, hit.point, Quaternion.identity);

            SLot.sourceSlot.item.add_Item_Count(-1);

            if(SLot.sourceSlot.item.get_Item_Count() <= 0)
            {
                Destroy(SLot.sourceSlot.item);
                SLot.sourceSlot.Clear_Slot();

                SLot.currentlyHeldItem = null;
                SLot.sourceSlot = null;
            }

            else
            {
                SLot.sourceSlot.Update_Count_text();
            }
            Debug.Log("설치 완료");
        }
    }

}
