using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;

[RequireComponent(typeof(XRSimpleInteractable))]
public class PurifiedWaterDispenser : NetworkBehaviour
{
    [Header("손에 쥐여줄 정수물 네트워크 프리팹")]
    [SerializeField] private NetworkPrefabRef cleanWaterItemPrefab;

    [SerializeField] private GameObject waterObject;  

    private XRSimpleInteractable simpleInteractable;

    private void Awake()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.AddListener(OnGrabWater);
        }
    }

    private void OnDisable()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.RemoveListener(OnGrabWater);
        }
    }

    private void OnGrabWater(SelectEnterEventArgs args)
    {
        IXRSelectInteractor interactor = args.interactorObject;
        if (interactor == null) return;

        // 내 손(HasInputAuthority)으로 그랩을 시도했는지 확인
        NetworkObject handNetObj = (interactor as MonoBehaviour)?.GetComponentInParent<NetworkObject>();
        if (handNetObj != null && handNetObj.HasInputAuthority)
        {
            RPC_RequestTakeWater();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestTakeWater()
    {
        // 1. 호스트가 정수물 프리팹을 스폰
        if (cleanWaterItemPrefab.IsValid && Runner.IsRunning)
        {
            NetworkObject spawnedWater = Runner.Spawn(cleanWaterItemPrefab, transform.position, transform.rotation);
            
            if (spawnedWater != null)
            {
                // 플레이어 손에 바로 쥐여주는 로직 처리용
                var grabInteractable = spawnedWater.GetComponent<XRGrabInteractable>();
                if (grabInteractable != null)
                {
                    // 스폰 후 자동 그랩 처리가 필요한 경우 여기서 핸들링
                }
            }
        }

        // 2. 전체 플레이어 화면에서 정수기 내부 waterObject 비활성화
        RPC_BroadcastHideWater();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastHideWater()
    {
        waterObject.SetActive(false);
    }
}