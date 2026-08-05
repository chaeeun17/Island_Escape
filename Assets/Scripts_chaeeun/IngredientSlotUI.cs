using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientSlotUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image ingredientIcon;         // 재료 아이콘 이미지
    [SerializeField] private TMP_Text ingredientNameText;   // 재료 이름
    [SerializeField] private TMP_Text amountText;           // 보유 수량 / 필요 수량 (예: 2 / 5)

    [Header("상태에 따른 텍스트 색상")]
    [SerializeField] private Color sufficientColor = Color.green; // 재료 충분 시 색상
    [SerializeField] private Color lackingColor = Color.red;      // 재료 부족 시 색상

    /// <summary>
    /// 재료 정보 및 현재 보유 수량을 받아 UI를 갱신합니다.
    /// </summary>
    public void Setup(Item item, int hasAmount, int totalNeeded)
    {
        if (item == null) return;

        // 1. 아이콘 설정
        if (ingredientIcon != null)
        {
            ingredientIcon.sprite = item.get_Item_Sprite();
            ingredientIcon.gameObject.SetActive(true);
        }

        // 2. 이름 설정
        if (ingredientNameText != null)
        {
            ingredientNameText.text = item.getItemName();
        }

        // 3. 수량 텍스트 및 색상 적용
        if (amountText != null)
        {
            amountText.text = $"{hasAmount} / {totalNeeded}";
            amountText.color = (hasAmount >= totalNeeded) ? sufficientColor : lackingColor;
        }
    }
}