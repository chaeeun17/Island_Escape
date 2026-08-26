using UnityEngine;
using Fusion;

[RequireComponent(typeof(Collider))]
public class NetworkMouthZone : NetworkBehaviour
{
    private bool isEating = false;
    private NetworkPlayerStats myPlayerStats;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public override void Spawned()
    {
        // 내 머리가 속한 플레이어의 Stats 컴포넌트 캐싱
        myPlayerStats = GetComponentInParent<NetworkPlayerStats>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 🌟 물리/삭제 권한을 가진 호스트(StateAuthority)에서만 충돌 판정 수행
        if (Runner == null || !Runner.IsServer) return;

        if (isEating) return;

        // 1. 닿은 오브젝트의 NetworkObject 확인
        NetworkObject itemNetObj = other.GetComponentInParent<NetworkObject>();
        if (itemNetObj == null) return;

        // 2. 이 입의 주인인 플레이어 스탯 컴포넌트 확인
        if (myPlayerStats == null) myPlayerStats = GetComponentInParent<NetworkPlayerStats>();
        if (myPlayerStats == null || myPlayerStats.IsPlayerFainted()) return;

        // 3. SO 로드
        string cleanName = itemNetObj.gameObject.name.Replace("(Clone)", "").Trim();
        Item itemSO = Resources.Load<Item>($"Items/{cleanName}");
        if (itemSO == null) itemSO = Resources.Load<Item>($"Items/Item_{cleanName}");
        if (itemSO == null) itemSO = Resources.Load<Item>(cleanName);

        if (itemSO == null) return;

        ItemType type = itemSO.GetItemType();

        // 4. 음식 타입 확인 후 해당 플레이어에게 스탯 적용 RPC 전송 & 아이템 삭제
        if (type == ItemType.RawFood || type == ItemType.Water || type == ItemType.CookedFood)
        {
            isEating = true;

            // 🌟 이 입의 주인(InputAuthority)에게만 스탯 변경 RPC 실행
            myPlayerStats.RPC_ApplyEatEffect(type);

            // 호스트가 아이템 Despawn
            Runner.Despawn(itemNetObj);
            Debug.Log($"🍽️ [Host] {cleanName} 먹기 처리 완료 -> 대상 플레이어에게 스탯 반영");

            Invoke(nameof(ResetEatingState), 0.5f);
        }
    }

    private void ResetEatingState()
    {
        isEating = false;
    }
}