using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class NetworkCraft : MonoBehaviour
{
    public static NetworkCraft Instance { get; private set; }

    [Header("등록된 조합법 목록")]
    public List<CraftingRecipe> availableRecipes;

    [Header("레시피 목록 UI (좌측 패널)")]
    public Transform recipeListContent;
    public GameObject recipeButtonPrefab;    // 레시피 버튼 프리팹

    [Header("상세 정보 패널 UI (우측 패널)")]
    public GameObject detailPanel;           // 선택 시 켜질 상세 패널 배경
    public Image resultItemIcon;             // 결과물 이미지
    public TMP_Text resultItemName;          // 결과물 이름
    public TMP_Text craftQuantityText;       // 제작할 수량 텍스트
    public Button craftButton;               // 제작 버튼

    // 🌟 [변경] 글씨 써주는 텍스트 대신, 프리팹을 소환할 컨테이너와 프리팹 연결
    [Header("필요 재료 목록 UI (우측 패널 내부)")]
    public Transform ingredientsContent;     // 재료 슬롯들이 생성될 부모 Transform (Vertical Layout Group 권장)
    public GameObject ingredientSlotPrefab;  // IngredientSlotUI가 붙은 슬롯 프리팹

    private CraftingRecipe currentRecipe;    // 현재 선택된 레시피
    private int currentCraftQuantity = 1;    // 현재 제작 수량

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        GenerateRecipeUI();
    }

    private void GenerateRecipeUI()
    {
        if (recipeListContent == null || recipeButtonPrefab == null) return;

        foreach (Transform child in recipeListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < availableRecipes.Count; i++)
        {
            int recipeIndex = i;
            var recipe = availableRecipes[i];
            if (recipe == null || recipe.resultItem == null) continue;

            GameObject newButtonObj = Instantiate(recipeButtonPrefab, recipeListContent, false);
            newButtonObj.transform.localScale = Vector3.one;

            RecipeButtonUI buttonUI = newButtonObj.GetComponent<RecipeButtonUI>();
            if (buttonUI != null)
            {
                buttonUI.Setup(recipe, () => SelectRecipe(recipeIndex));
            }
        }
    }

    public void SelectRecipe(int recipeIndex)
    {
        if (recipeIndex >= 0 && recipeIndex < availableRecipes.Count)
        {
            currentRecipe = availableRecipes[recipeIndex];
            currentCraftQuantity = 1;
            UpdateDetailPanel();
        }
    }

    public void ChangeQuantity(int amount)
    {
        if (currentRecipe == null) return;

        currentCraftQuantity += amount;
        if (currentCraftQuantity < 1) currentCraftQuantity = 1;

        UpdateDetailPanel();
    }

    // 🌟 [수정] 필요 재료 목록을 프리팹 슬롯 기반으로 동적 생성
    public void UpdateDetailPanel()
    {
        if (currentRecipe == null || currentRecipe.resultItem == null) return;
        if (detailPanel != null) detailPanel.SetActive(true);

        // 결과물 기본 정보
        if (resultItemIcon != null) resultItemIcon.sprite = currentRecipe.resultItem.get_Item_Sprite();
        if (resultItemName != null) resultItemName.text = currentRecipe.resultItem.getItemName();
        if (craftQuantityText != null) craftQuantityText.text = currentCraftQuantity.ToString();

        // 1. 기존에 생성되어 있던 필요 재료 프리팹 슬롯들 정리
        if (ingredientsContent != null)
        {
            foreach (Transform child in ingredientsContent)
            {
                Destroy(child.gameObject);
            }
        }

        bool canCraft = true;
        NetworkInventory netInv = NetworkInventory.Instance;

        // 2. 필요 재료 순회하며 프리팹 슬롯 동적 생성
        if (netInv != null && ingredientsContent != null && ingredientSlotPrefab != null)
        {
            foreach (var req in currentRecipe.requiredIngredients)
            {
                if (req.item == null) continue;

                int hasAmount = netInv.GetItemCount(req.item.getItemName());
                int totalNeeded = req.amount * currentCraftQuantity;

                if (hasAmount < totalNeeded)
                {
                    canCraft = false; // 재료 하나라도 부족 시 제작 불가
                }

                // 재료 프리팹 슬롯 생성 및 데이터 전달
                GameObject slotObj = Instantiate(ingredientSlotPrefab, ingredientsContent, false);
                slotObj.transform.localScale = Vector3.one;

                IngredientSlotUI slotUI = slotObj.GetComponent<IngredientSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(req.item, hasAmount, totalNeeded);
                }
            }
        }
        else if (netInv == null)
        {
            canCraft = false;
        }

        // 재료가 모두 만족될 때만 제작 버튼 활성화
        if (craftButton != null) craftButton.interactable = canCraft;
    }

    public void OnCraftButtonClick()
    {
        Debug.Log("1. 크래프트 버튼 클릭됨!");

        if (currentRecipe == null || currentRecipe.resultItem == null) {
            Debug.LogWarning("2. currentRecipe 또는 resultItem이 null입니다.");
            return;
        }

        NetworkInventory netInv = NetworkInventory.Instance;
        if (netInv == null) return;

        // 1. 재료 검증
        foreach (var req in currentRecipe.requiredIngredients)
        {
            if (req.item == null) return;
            int totalNeeded = req.amount * currentCraftQuantity;
            if (netInv.GetItemCount(req.item.getItemName()) < totalNeeded) return;
        }

        // 2. 재료 소모
        foreach (var req in currentRecipe.requiredIngredients)
        {
            int totalNeeded = req.amount * currentCraftQuantity;
            netInv.ConsumeItem(req.item.getItemName(), totalNeeded);
        }

        // 3. 완성품 지급
        Item craftedItem = Instantiate(currentRecipe.resultItem);
        craftedItem.set_Item_Count(currentRecipe.resultAmount * currentCraftQuantity);
        netInv.AddItem(craftedItem);

        // 4. UI 갱신 (재료 차감 후 변경된 수량 재반영)
        UpdateDetailPanel();
    }
}