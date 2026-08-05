using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RecipeButtonUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    [SerializeField] private Image iconImage;       // 자식에 있는 아이콘 Image
    [SerializeField] private TMP_Text nameText;     // 자식에 있는 이름 TextMeshPro
    [SerializeField] private Button button;         // 버튼 컴포넌트 (자기 자신)

    /// <summary>
    /// 레시피 데이터와 클릭 이벤트를 넘겨받아 버튼 UI를 초기화합니다.
    /// </summary>
    public void Setup(CraftingRecipe recipe, Action onClickAction)
    {
        if (recipe == null || recipe.resultItem == null) return;

        // 1. 아이콘 이미지 세팅
        if (iconImage != null)
        {
            iconImage.sprite = recipe.resultItem.get_Item_Sprite();
            iconImage.gameObject.SetActive(true);
        }

        // 2. 이름 텍스트 세팅
        if (nameText != null)
        {
            nameText.text = recipe.resultItem.getItemName();
        }

        // 3. 버튼 클릭 이벤트 연결
        if (button == null) button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners(); // 기존 리스너 초기화
            button.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }
}