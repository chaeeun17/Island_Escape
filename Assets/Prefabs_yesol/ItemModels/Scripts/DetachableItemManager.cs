using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion; // Photon Fusion 필수

public class DetachableItemManager : NetworkBehaviour
{
    [System.Serializable]
    public struct DetachablePartInfo
    {
        public NetworkObject partPrefab; // 분리되어 생길 네트워크 자식 프리팹
        public Transform spawnPoint;     // 생겨날 위치 (메인 오브젝트 내의 자식 Transform)
    }

    [Header("Detachable Parts Settings")]
    [SerializeField] private DetachablePartInfo[] partsToSpawn;

    private XRGrabInteractable mainInteractable;

    // 네트워크 상태 동기화 (이중 실행 방지)
    [Networked] private NetworkBool IsDetached { get; set; }

    private void Awake()
    {
        mainInteractable = GetComponent<XRGrabInteractable>();
        mainInteractable.activated.AddListener(OnItemActivated);
    }

    private void OnItemActivated(ActivateEventArgs args)
    {
        // 이미 분리되었으면 무시
        if (IsDetached) return;

        // XR Trigger를 누른 클라이언트가 호스트(StateAuthority)에게 분리 요청
        if (HasStateAuthority)
        {
            DetachAllParts();
        }
        else
        {
            RPC_RequestDetach();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDetach()
    {
        if (IsDetached) return;
        DetachAllParts();
    }

    private void DetachAllParts()
    {
        // 1. 중복 실행 방지 플래그
        IsDetached = true;

        NetworkObject handle = Runner.Spawn(
            partsToSpawn[0].partPrefab, 
            partsToSpawn[0].spawnPoint.position, 
            partsToSpawn[0].spawnPoint.rotation,
            Object.InputAuthority
            );

        NetworkObject bow = Runner.Spawn(
            partsToSpawn[1].partPrefab,
            partsToSpawn[1].spawnPoint.position,
            partsToSpawn[1].spawnPoint.rotation,
            Object.InputAuthority
            );

        NetworkObject pedestal = Runner.Spawn(
            partsToSpawn[2].partPrefab,
            partsToSpawn[2].spawnPoint.position,
            partsToSpawn[2].spawnPoint.rotation,
            Object.InputAuthority
            );

        // 3. 서로의 참조 설정 (NetworkId 전달)
        var handleScript = handle.GetComponent<HandlePart>();
        handleScript.InitPedestalReference(pedestal);

        Runner.Despawn(Object);
    }

    private void OnDestroy()
    {
        if (mainInteractable != null)
        {
            mainInteractable.activated.RemoveListener(OnItemActivated);
        }
    }
}
/*
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;

public class DetachableItemManager : NetworkBehaviour
{
    [Header("Child Grab Interactable")]
    [SerializeField] private XRGrabInteractable[] childInteractables;

    private XRGrabInteractable mainInteractable;
    private Rigidbody mainRigidbody;
    // private bool isActivated = false;

    // 네트워크 상태 동기화 변수 (모든 클라이언트에게 분리 여부 공유)
    [Networked] private NetworkBool isActivated { get; set; }

    void Awake()
    {
        mainInteractable = GetComponent<XRGrabInteractable>();
        mainRigidbody = GetComponent<Rigidbody>();

        foreach (var child in childInteractables)
        {
            child.enabled = false;

            if (child.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }
        }

        mainInteractable.activated.AddListener(OnItemActivated);
    }

    private void OnItemActivated(ActivateEventArgs args)
    {
        if (isActivated) return;

        // Fusion 권한 체크: State Authority(호스트)가 아니라면 권한 요청 또는 서버 RPC 호출 필요
        if (!HasStateAuthority)
        {
            RPC_RequestDetach();
            return;
        }

        isActivated = true;
        DetachParts();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDetach()
    {
        isActivated = true;
        DetachParts();
    }

    private void DetachParts()
    {
        foreach (var child in childInteractables)
        {
            // 자식 오브젝트 각각이 독립된 'NetworkObject'라면 Fusion 전용 분리 방식을 쓰거나,
            // 단순 시각적 오브젝트라면 SetParent(null) 후 NetworkTransform을 갱신해주어야 합니다.
            child.transform.SetParent(null);

            if (child.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
            }

            child.enabled = true;
        }

        mainInteractable.enabled = false;

        if (TryGetComponent<Collider>(out var mainCollider))
        {
            mainCollider.enabled = false;
        }

        Destroy(mainInteractable);
        Destroy(mainRigidbody);
    }

    void OnDestroy()
    {
        if (mainInteractable != null)
        {
            mainInteractable.activated.RemoveListener(OnItemActivated);
        }
    }
}
*/