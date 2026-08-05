using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CraftingManager : MonoBehaviour
{
    /*
    [Header("등록된 조합법 목록")]
    public List<CraftingRecipe> availableRecipes;

    [Header("레시피 목록 UI (좌측 패널)")]
    public Transform recipeListContent;
    public GameObject recipeButtonPrefab;    // 레시피 버튼 프리팹

    [Header("상세 정보 패널 UI (우측 패널)")]
    public GameObject detailPanel;           // 선택 시 켜질 상세 패널 배경
    public Image resultItemIcon;             // 결과물 이미지
    public TMP_Text resultItemName;          // 결과물 이름
    public TMP_Text ingredientsText;         // 필요 재료 텍스트 (가진 개수 / 필요 개수)
    public TMP_Text craftQuantityText;       // 제작할 수량 텍스트
    public Button craftButton;               // 제작 버튼

    private CraftingRecipe currentRecipe;    // 현재 선택된 레시피
    private int currentCraftQuantity = 1;    // 현재 제작 수량

    private void Start()
    {
        // 시작 시 상세 패널을 꺼둡니다.
        if (detailPanel != null) detailPanel.SetActive(false);

        GenerateRecipeUI();
    }

    private void GenerateRecipeUI()
    {
        if (recipeListContent == null || recipeButtonPrefab == null) return;

        for(int i = 0; i<availableRecipes.Count; i++)
        {
            int recipeIndex = i;

            GameObject newButtonObj = Instantiate(recipeButtonPrefab, recipeListContent, false);

            newButtonObj.transform.localScale = Vector3.one; // 스케일 초기화

            Image buttonImage = newButtonObj.GetComponent<Image>();
            if (buttonImage != null && availableRecipes[i].resultPrefab != null)
            {
                buttonImage.sprite = availableRecipes[i].resultPrefab.get_Item_Sprite();
            }

            Button buttonComponent = newButtonObj.GetComponent<Button>();
            if(buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => SelectRecipe(recipeIndex));
            }
        }
    }

    // 왼쪽 레시피 목록의 버튼들이 클릭될 때 호출할 메서드 (매개변수로 레시피 인덱스 전달)
    public void SelectRecipe(int recipeIndex)
    {
        if (recipeIndex >= 0 && recipeIndex < availableRecipes.Count)
        {
            currentRecipe = availableRecipes[recipeIndex];
            currentCraftQuantity = 1; // 다른 항목을 누르면 기본 수량 1로 초기화
            UpdateDetailPanel();
        }
    }

    // 수량 조절 버튼 (+, -) 에서 호출할 메서드
    public void ChangeQuantity(int amount)
    {
        if (currentRecipe == null) return;

        currentCraftQuantity += amount;
        if (currentCraftQuantity < 1) currentCraftQuantity = 1; // 1개 이하로는 내려가지 않음

        UpdateDetailPanel();
    }

    // 상세 패널의 UI 내용과 제작 버튼 활성화 여부를 갱신하는 메서드
    private void UpdateDetailPanel()
    {
        if (currentRecipe == null) return;
        if (detailPanel != null) detailPanel.SetActive(true);

        // 결과물 아이콘 및 이름 갱신
        if (resultItemIcon != null) resultItemIcon.sprite = currentRecipe.resultPrefab.get_Item_Sprite();
        if (resultItemName != null) resultItemName.text = currentRecipe.resultPrefab.getItemName();
        if (craftQuantityText != null) craftQuantityText.text = currentCraftQuantity.ToString();

        bool canCraft = true;
        string ingredientsString = "";

        // 등록된 재료(requiredIngredients)를 하나씩 검사[cite: 4]
        foreach (var req in currentRecipe.requiredIngredients)
        {
            // 인벤토리를 뒤져서 이 재료를 몇 개나 가지고 있는지 확인
            int hasAmount = Inventory.Instance.GetItemCount(req.itemName);
            int totalNeeded = req.amount * currentCraftQuantity;

            if (hasAmount < totalNeeded)
            {
                canCraft = false; // 재료 하나라도 부족하면 제작 불가
                // 부족한 경우 빨간색 텍스트로 표시
                ingredientsString += $"<color=red>{req.itemName} : {hasAmount} / {totalNeeded}</color>\n";
            }
            else
            {
                // 충분한 경우 초록색 텍스트로 표시
                ingredientsString += $"<color=green>{req.itemName} : {hasAmount} / {totalNeeded}</color>\n";
            }
        }

        if (ingredientsText != null) ingredientsText.text = ingredientsString;

        // 재료가 충분할 때만 제작 버튼 활성화
        if (craftButton != null) craftButton.interactable = canCraft;
    }

    // 제작 버튼 클릭 시 호출
    public void OnCraftButtonClick()
    {
        if (currentRecipe == null) return;

        // 1. 방어 코드: 재료가 충분한지 다시 한번 확실히 체크
        foreach (var req in currentRecipe.requiredIngredients)
        {
            int totalNeeded = req.amount * currentCraftQuantity;
            if (Inventory.Instance.GetItemCount(req.itemName) < totalNeeded)
            {
                Debug.Log("재료가 부족하여 제작할 수 없습니다.");
                return;
            }
        }

        // 2. 인벤토리에서 재료 소모
        foreach (var req in currentRecipe.requiredIngredients)
        {
            int totalNeeded = req.amount * currentCraftQuantity;
            Inventory.Instance.ConsumeItem(req.itemName, totalNeeded);
        }

        // 3. 인벤토리에 완성품 추가 (원래 생성 개수 * 제작 수량)[cite: 4]
        Item craftedItem = Instantiate(currentRecipe.resultPrefab);
        craftedItem.set_Item_Count(currentRecipe.resultAmount * currentCraftQuantity);

        Inventory.Instance.AddItem(craftedItem);

        Debug.Log($"{craftedItem.getItemName()} {craftedItem.get_Item_Count()}개 인벤토리로 지급 완료!");

        // 4. 재료가 소모되었으므로 현재 띄워진 UI 갱신
        UpdateDetailPanel();
    }*/
}