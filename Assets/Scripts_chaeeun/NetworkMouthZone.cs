using UnityEngine;
using Fusion;

public class NetworkMouthZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        NetworkObject itemNetworkObj = other.GetComponentInParent<NetworkObject>();
        if (itemNetworkObj == null) return;

        // 2. 내 플레이어가 집어서 권한(Authority)을 가지고 있는 아이템만 처리
        if (itemNetworkObj.HasStateAuthority || itemNetworkObj.HasInputAuthority)
        {
            // 🌟 프리팹/오브젝트 이름에서 "(Clone)" 문자열 제거하여 원래 프리팹 이름 구하기
            string cleanPrefabName = itemNetworkObj.gameObject.name.Replace("(Clone)", "").Trim();

            // 🌟 인벤토리 방식: Resources 폴더에서 프리팹 이름과 동일한 SO 로드
            Item itemSO = Resources.Load<Item>($"{"Items/"}{cleanPrefabName}");

            if (itemSO != null)
            {
                Eat(itemNetworkObj, itemSO);
            }
            else
            {
                Debug.LogWarning($"❌ [NetworkMouthZone] 경로에서 SO를 찾을 수 없습니다.");
            }
        }
    }

    // 진짜 음식을 먹고 처리하는 함수
    private void Eat(NetworkObject itemNetworkObj, Item itemSO)
    {
        if (NetworkPlayerStats.LocalInstance == null)
        {
            Debug.LogWarning("❌ NetworkPlayerStats.LocalInstance를 찾을 수 없습니다.");
            return;
        }

        // 🌟 SO에 정의된 ItemType 기반으로 스탯 분기 처리
        switch (itemSO.GetItemType())
        {
            case ItemType.RawFood:
                // 날음식 ➔ 체력 감소
                NetworkPlayerStats.LocalInstance.ReduceStamina(20);
                Debug.LogWarning($"🤢 (날음식)을 먹었습니다! 체력 감소");
                break;

            case ItemType.Water:
                // 물/음료 ➔ 갈증 회복
                NetworkPlayerStats.LocalInstance.RestoreThirst(20);
                Debug.LogWarning($"💧  (물/음료)을 마셨습니다! 갈증 회복");
                break;

            case ItemType.CookedFood:
                NetworkPlayerStats.LocalInstance.RestoreHunger(20);
                Debug.LogWarning($"🍎 (음식)을 먹었습니다! 배고픔 회복");
                break;
            default:
                Debug.Log($"📦먹을 수 있는 아이템이 아닙니다.");
                return;
        }

        // Fusion의 Despawn을 호출해 네트워크상에서 아이템 파괴
        if (itemNetworkObj.Runner != null && itemNetworkObj.Runner.IsRunning)
        {
            itemNetworkObj.Runner.Despawn(itemNetworkObj);
        }
    }
}

