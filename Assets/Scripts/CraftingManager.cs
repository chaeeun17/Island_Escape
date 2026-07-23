using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public SLot[] inputSlots;
    public SLot resultSlot;

    [Header("등록된 조합법 목록")]
    public List<CraftingRecipe> availableRecipes;

    public void OnCraftButtonClick()
    {
        // 1. 제작대가 비어있는지 먼저 확인
        bool isTableEmpty = true;
        if (inputSlots != null)
        {
            foreach (var slot in inputSlots)
            {
                if (slot != null && slot.item != null)
                {
                    isTableEmpty = false;
                    break;
                }
            }
        }

        if (isTableEmpty)
        {
            Debug.Log("제작대에 재료가 없습니다.");
            return;
        }

        // 2. 현재 올려둔 재료와 일치하는 조합법이 있는지 확인
        CraftingRecipe matchedRecipe = GetMatchingRecipe();

        if (matchedRecipe != null)
        {
            // 결과 슬롯에 다른 아이템이 있는지 체크
            if (resultSlot.item != null)
            {
                if (resultSlot.item.getItemName() != matchedRecipe.resultPrefab.getItemName())
                {
                    Debug.Log("결과 슬롯에 다른 아이템이 있습니다. 아이템을 먼저 수령하세요.");
                    return;
                }
            }

            // 3. 일치하는 조합법이 있고, 결과 슬롯도 비어있거나 같은 아이템이라면 재료 소모
            ConsumeIngredients(matchedRecipe);

            // 4. 결과 슬롯에 아이템 생성 또는 누적
            if (resultSlot.item == null)
            {
                // ScriptableObject 원본 에셋을 복제(Instance)하여 사용
                Item craftedItem = Instantiate(matchedRecipe.resultPrefab);
                craftedItem.set_Item_Count(matchedRecipe.resultAmount);
                resultSlot.SetSlot(craftedItem, false);
            }
            else
            {
                // 이미 슬롯에 동일한 아이템이 있다면 새로 만들지 않고 개수만 더해줍니다.
                resultSlot.item.add_Item_Count(matchedRecipe.resultAmount);
                resultSlot.Update_Count_text(); // UI 숫자 갱신
            }

            Debug.Log("조합 성공: " + matchedRecipe.resultPrefab.getItemName() + " (현재 누적 개수: " + resultSlot.item.get_Item_Count() + ")");
        }
        else
        {
            Debug.Log("일치하는 조합법이 없거나 재료가 부족합니다.");
        }
    }

    // 등록된 레시피 중 현재 올려둔 재료와 일치하는 레시피를 찾습니다.
    private CraftingRecipe GetMatchingRecipe()
    {
        if (availableRecipes == null) return null;

        foreach (CraftingRecipe recipe in availableRecipes)
        {
            if (IsRecipeMatch(recipe))
                return recipe;
        }
        return null;
    }

    // 슬롯에 올라간 아이템들이 특정 레시피의 조건을 만족하는지 검사합니다.
    private bool IsRecipeMatch(CraftingRecipe recipe)
    {
        // 🛡️ 방어 코드: 레시피 자체가 비어있거나, 재료 리스트가 없으면 검사하지 않고 패스
        if (recipe == null || recipe.requiredIngredients == null)
        {
            return false;
        }

        Dictionary<string, int> currentIngredients = new Dictionary<string, int>();

        if (inputSlots == null) return false;

        foreach (var slot in inputSlots)
        {
            if (slot != null && slot.item != null)
            {
                string name = slot.item.getItemName();
                if (string.IsNullOrEmpty(name)) continue;

                if (currentIngredients.ContainsKey(name))
                    currentIngredients[name] += slot.item.get_Item_Count();
                else
                    currentIngredients[name] = slot.item.get_Item_Count();
            }
        }

        // 조합법의 요구 재료 종류 개수와 올려둔 재료 종류 개수가 다르면 실패
        if (currentIngredients.Count != recipe.requiredIngredients.Count)
        {
            return false;
        }

        // 각 재료의 개수가 충분한지 확인
        foreach (var req in recipe.requiredIngredients)
        {
            // 인스펙터 상에서 비어있는 재료 항목 무시
            if (string.IsNullOrEmpty(req.itemName)) continue;

            if (!currentIngredients.ContainsKey(req.itemName) || currentIngredients[req.itemName] < req.amount)
            {
                return false; // 재료 부족 혹은 없음
            }
        }

        return true;
    }

    // 조합법에 맞게 재료 슬롯의 아이템 개수를 차감합니다.
    private void ConsumeIngredients(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.requiredIngredients == null) return;

        Dictionary<string, int> remainingToConsume = new Dictionary<string, int>();
        foreach (var req in recipe.requiredIngredients)
        {
            if (!string.IsNullOrEmpty(req.itemName))
            {
                remainingToConsume[req.itemName] = req.amount;
            }
        }

        if (inputSlots == null) return;

        foreach (var slot in inputSlots)
        {
            if (slot != null && slot.item != null)
            {
                string itemName = slot.item.getItemName();

                if (remainingToConsume.ContainsKey(itemName) && remainingToConsume[itemName] > 0)
                {
                    int consumeAmount = Mathf.Min(slot.item.get_Item_Count(), remainingToConsume[itemName]);

                    slot.item.add_Item_Count(-consumeAmount);
                    remainingToConsume[itemName] -= consumeAmount;

                    if (slot.item.get_Item_Count() <= 0)
                    {
                        // ScriptableObject 인스턴스 파괴 (메모리 누수 방지)
                        Destroy(slot.item);
                        slot.Clear_Slot();
                    }
                    else
                    {
                        slot.Update_Count_text();
                    }
                }
            }
        }
    }
}