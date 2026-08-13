using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;

[RequireComponent(typeof(XRGrabInteractable))]
public class NetworkWaterPurifier : NetworkBehaviour, IInteractable
{
    [Header("정수 소요 시간 (초)")]
    [SerializeField] private float purifyingTime = 3.0f;

    [Header("정수기 연출용 오브젝트")]
    [SerializeField] private GameObject bigWaterObject; // 정수 진행 중 연출용
    [SerializeField] private GameObject waterObject;    // 정수 완료 후 노출될 물 오브젝트

    [Header("손에 쥐여줄 정수물 네트워크 프리팹")]
    [SerializeField] private NetworkPrefabRef cleanWaterItemPrefab;

    [Networked] private NetworkBool isPurifying { get; set; }
    [Networked] private NetworkBool hasWater { get; set; } // 정수 완료 상태 저장

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            // 정수기를 그랩하려고 할 때(selectEntered) 이벤트를 가로챔!
            grabInteractable.selectEntered.AddListener(OnGrabPurifier);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabPurifier);
        }
    }

    public override void Spawned()
    {
        if (bigWaterObject != null) bigWaterObject.SetActive(false);
        if (waterObject != null) waterObject.SetActive(false);
    }

    public string GetInteractText()
    {
        if (isPurifying) return "정수 진행 중...";
        if (hasWater) return "정수 완료 (그랩 시 정수물 획득)";
        return "정수하기 (바닷물 아이템 필요)";
    }

    // X버튼 상호작용 (바닷물 넣고 정수 시작)
    public void Interact()
    {
        if (isPurifying || hasWater) return;

        IXRSelectInteractor currentInteractor = GetLocalHandInteractor();
        if (currentInteractor == null) return;

        if (currentInteractor.hasSelection)
        {
            var selectedInteractable = currentInteractor.interactablesSelected[0];
            GameObject heldObject = selectedInteractable.transform.gameObject;

            if (heldObject.name.Contains("Item_SeaWater"))
            {
                NetworkObject bucketNetObj = heldObject.GetComponentInParent<NetworkObject>();
                if (bucketNetObj != null)
                {
                    RPC_RequestPurifying(bucketNetObj);
                }
            }
        }
    }

    // 🌟 핵심: 정수기 본체를 그랩(Grip)하려고 시도할 때 작동하는 함수
    private void OnGrabPurifier(SelectEnterEventArgs args)
    {
        IXRSelectInteractor interactor = args.interactorObject;
        if (interactor == null) return;

        // 1. 일단 정수기 본체가 손에 들리는 그랩 상태를 즉시 취소/해제시킴!
        if (grabInteractable.interactionManager != null)
        {
            grabInteractable.interactionManager.SelectCancel(interactor, grabInteractable);
        }

        // 2. 만약 정수가 완료되어 물이 차있는 상태라면 ➔ 정수물 아이템을 손에 쥐여줌!
        if (hasWater)
        {
            NetworkObject handNetObj = (interactor as MonoBehaviour)?.GetComponentInParent<NetworkObject>();
            if (handNetObj != null && handNetObj.HasInputAuthority)
            {
                RPC_RequestTakeWater();
            }
        }
        else
        {
            Debug.Log("💧 정수된 물이 없어서 아무것도 집히지 않습니다.");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPurifying(NetworkObject bucketNetObj)
    {
        if (isPurifying) return;

        isPurifying = true;

        if (bucketNetObj != null && Runner.IsRunning)
        {
            Runner.Despawn(bucketNetObj);
        }

        RPC_BroadcastPurifySequence();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastPurifySequence()
    {
        StartCoroutine(PurifyingProcessRoutine());
    }

    private IEnumerator PurifyingProcessRoutine()
    {
        if (bigWaterObject != null) bigWaterObject.SetActive(true);
        if (waterObject != null) waterObject.SetActive(false);

        yield return new WaitForSeconds(purifyingTime);

        if (bigWaterObject != null) bigWaterObject.SetActive(false);
        if (waterObject != null) waterObject.SetActive(true);

        if (HasStateAuthority)
        {
            isPurifying = false;
            hasWater = true; // 물이 차오름!
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestTakeWater()
    {
        if (!hasWater) return;

        // 1. 손 위치/정수기 위치에 정수물 네트워크 아이템 스폰
        if (cleanWaterItemPrefab.IsValid && Runner.IsRunning)
        {
            Vector3 spawnPos = waterObject != null ? waterObject.transform.position : transform.position;
            NetworkObject spawnedWater = Runner.Spawn(cleanWaterItemPrefab, spawnPos, Quaternion.identity);

            if (spawnedWater != null)
            {
                //spawnedWater.name = "Item_CleanWater";
            }
        }

        hasWater = false;
        RPC_BroadcastResetWaterUI();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastResetWaterUI()
    {
        // 정수물 아이템을 꺼내갔으므로 정수기 내부 waterObject 비활성화
        if (waterObject != null)
        {
            waterObject.SetActive(false);
        }
    }

    private IXRSelectInteractor GetLocalHandInteractor()
    {
        foreach (var interactor in UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
        {
            NetworkObject netObj = interactor.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority && interactor is IXRSelectInteractor selectInteractor)
            {
                if (selectInteractor.hasSelection) return selectInteractor;
            }
        }

        foreach (var interactor in UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
        {
            NetworkObject netObj = interactor.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority && interactor is IXRSelectInteractor selectInteractor)
            {
                return selectInteractor;
            }
        }

        return null;
    }
}