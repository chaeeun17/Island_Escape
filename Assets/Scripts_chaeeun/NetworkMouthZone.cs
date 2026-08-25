using UnityEngine;
using Fusion;

public class NetworkMouthZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. 내 캐릭터(Local Player)의 입에 들어왔을 때만 감지
        if (!Object.HasInputAuthority) return;

        NetworkObject itemNetworkObj = other.GetComponentInParent<NetworkObject>();
        if (itemNetworkObj == null) return;

        // 2. 프리팹/오브젝트 이름에서 "(Clone)" 제거
        string cleanPrefabName = itemNetworkObj.gameObject.name.Replace("(Clone)", "").Trim();

        // 3. SO 로드
        Item itemSO = Resources.Load<Item>($"Items/{cleanPrefabName}");
        if (itemSO == null)
        {
            return;
        }

        // 4. 먹을 수 있는 타입인지 확인 후 로컬 스탯 적용 + 호스트에 Despawn 요청
        if (ApplyEatEffect(itemSO))
        {
            // 호스트에게 아이템 삭제(Despawn) 요청 RPC
            RPC_RequestEat(itemNetworkObj);
        }
    }

    /// <summary>
    /// 로컬 플레이어의 스탯 회복/감소 처리
    /// </summary>
    private bool ApplyEatEffect(Item itemSO)
    {
        if (NetworkPlayerStats.LocalInstance == null)
        {
            Debug.LogWarning("❌ NetworkPlayerStats.LocalInstance를 찾을 수 없습니다.");
            return false;
        }

        switch (itemSO.GetItemType())
        {
            case ItemType.RawFood:
                NetworkPlayerStats.LocalInstance.ReduceStamina(20);
                Debug.Log("🤢 (날음식)을 먹었습니다! 체력 감소");
                return true;

            case ItemType.Water:
                NetworkPlayerStats.LocalInstance.RestoreThirst(20);
                Debug.Log("💧 (물/음료)을 마셨습니다! 갈증 회복");
                return true;

            case ItemType.CookedFood:
                NetworkPlayerStats.LocalInstance.RestoreHunger(20);
                Debug.Log("🍎 (음식)을 먹었습니다! 배고픔 회복");
                return true;

            default:
                Debug.Log("📦 먹을 수 있는 아이템이 아닙니다.");
                return false;
        }
    }

    /// <summary>
    /// 호스트(StateAuthority)에서 실행되어 네트워크 오브젝트를 안전하게 제거하는 RPC
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestEat(NetworkObject itemNetworkObj)
    {
        if (itemNetworkObj != null && Runner != null && Runner.IsRunning)
        {
            Runner.Despawn(itemNetworkObj);
        }
    }
}