using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkInventoryZone : MonoBehaviour
{
    [SerializeField] private Collider zoneCollider; // 이 Zone의 트리거 콜라이더 (인스펙터 연결)
    private readonly HashSet<NetworkObject> _processedObjects = new HashSet<NetworkObject>();

    // 🌟 이 Zone이 내 로컬 플레이어에 붙어있는지 판별용 (남의 아바타에 붙은 Zone 복제본이 반응하면 안 됨)
    private NetworkObject _ownerNetworkObject;

    private void Awake()
    {
        if (zoneCollider == null) zoneCollider = GetComponent<Collider>();
        _ownerNetworkObject = GetComponentInParent<NetworkObject>();
    }

    private void OnEnable()
    {
        // 🌟 Client에서는 Physics.Simulate()가 돌지 않아(ClientPhysicsSimulation.Disabled) 트리거
        //    콜백(OnTriggerEnter/Exit)이 불안정하게 발생/누락되는 문제가 있었다.
        //    대신 "실제로 이 아이템을 쥐고 있던 로컬 유저가 놓는 순간" 확실히 발생하는
        //    NetworkGrabItem.LocallyReleased 콜백을 구독해서, 놓은 위치가 내 Zone 범위 안인지 직접 검사한다.
        NetworkGrabItem.LocallyReleased += HandleItemLocallyReleased;
    }

    private void OnDisable()
    {
        NetworkGrabItem.LocallyReleased -= HandleItemLocallyReleased;
    }

    private void HandleItemLocallyReleased(NetworkGrabItem item, Vector3 releasePosition)
    {
        if (item == null || zoneCollider == null) return;

        // 🌟 이 Zone은 네트워크 오브젝트가 아니라 모든 클라이언트(호스트 포함)에서 각자 로컬로
        //    실행되므로, 같은 프로세스 안에 있는 남의 아바타에 붙은 Zone 복제본도 이 이벤트를
        //    구독하고 있다. 내 로컬 플레이어의 Zone일 때만 반응해야 남의 인벤토리에 잘못 들어가지 않는다.
        if (_ownerNetworkObject == null || !_ownerNetworkObject.HasInputAuthority) return;

        // 🌟 이 Zone에 보관 가능한 아이템만 대상으로 함 (기존 OnTriggerEnter의 ItemRemove 체크와 동일한 조건)
        if (item.GetComponent<ItemRemove>() == null) return;

        if (!zoneCollider.bounds.Contains(releasePosition)) return;

        var grabInteractable = item.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null) return;

        TrySaveAndDespawn(grabInteractable);
    }

    private void TrySaveAndDespawn(XRGrabInteractable grabInteractable)
    {
        if (grabInteractable == null) return;

        GameObject itemGameObject = grabInteractable.transform.gameObject;
        NetworkObject netObj = itemGameObject.GetComponentInParent<NetworkObject>();
        if (netObj == null || netObj.Runner == null) return;

        if (_processedObjects.Contains(netObj)) return;
        _processedObjects.Add(netObj);

        string cleanItemName = itemGameObject.name.Replace("(Clone)", "").Trim();

        if (NetworkInventory.Instance != null)
        {
            NetworkInventory.Instance.AddItemByName(cleanItemName);
            Debug.Log($"🎒 [{cleanItemName}] 인벤토리 보관 성공!");
        }
        else
        {
            Debug.LogError("🚨 NetworkInventory.Instance가 존재하지 않습니다!");
            _processedObjects.Remove(netObj);
            return;
        }

        if (netObj.HasStateAuthority)
        {
            netObj.Runner.Despawn(netObj);
        }
        else
        {
            // 🌟 Host 토폴로지에서는 StateAuthority(Host)만 Despawn을 실제로 수행할 수 있음.
            //    RequestStateAuthority()는 Host 모드에서 클라이언트로 권한을 넘겨주지 않으므로,
            //    Host에 Despawn을 대신 요청하는 RPC를 사용해야 함.
            NetworkGrabItem grabItemNetComp = netObj.GetComponentInChildren<NetworkGrabItem>();
            if (grabItemNetComp != null)
            {
                grabItemNetComp.RPC_RequestDespawn();
            }
            else
            {
                Debug.LogError($"🚨 [{netObj.name}]에 NetworkGrabItem이 없어 Despawn 요청을 보낼 수 없습니다!");
            }
        }
    }
}
