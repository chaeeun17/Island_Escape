using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;

[RequireComponent(typeof(XRSimpleInteractable))]
public class NetworkWaterPurifier : NetworkBehaviour
{
    [Header("정수 소요 시간 (초)")]
    [SerializeField] private float purifyingTime = 3.0f;

    [Header("정수기 연출용 오브젝트")]
    [SerializeField] private GameObject bigWaterObject; // 정수 진행 중 연출용
    [SerializeField] private GameObject waterObject;    // 정수 완료 후 차오르는 물 오브젝트

    [Header("스폰할 정수물 네트워크 프리팹")]
    [SerializeField] private NetworkPrefabRef cleanWaterItemPrefab;

    [Networked] private NetworkBool isPurifying { get; set; }
    [Networked] private NetworkBool hasWater { get; set; } // 정수 완료 상태

    private XRSimpleInteractable _simpleInteractable;

    private void Awake()
    {
        _simpleInteractable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        if (_simpleInteractable != null)
        {
            _simpleInteractable.selectEntered.AddListener(OnInteractPurifier);
        }
    }

    private void OnDisable()
    {
        if (_simpleInteractable != null)
        {
            _simpleInteractable.selectEntered.RemoveListener(OnInteractPurifier);
        }
    }

    public override void Spawned()
    {
        if (bigWaterObject != null) bigWaterObject.SetActive(false);
        if (waterObject != null) waterObject.SetActive(false);
    }

    // 바닷물 아이템과 충돌 감지 시 정수 시작
    private void OnTriggerEnter(Collider other)
    {
        if (isPurifying || hasWater) return;

        if (other.name.Contains("Item_SeaWater") || (other.transform.parent != null && other.transform.parent.name.Contains("Item_SeaWater")))
        {
            NetworkObject seaWaterNetObj = other.GetComponentInParent<NetworkObject>();
            if (seaWaterNetObj != null)
            {
                RPC_RequestPurifying(seaWaterNetObj);
            }
        }
    }

    // VR 손으로 정수기를 잡았을 때(SelectEntered) 호출
    private void OnInteractPurifier(SelectEnterEventArgs args)
    {
        if (!hasWater)
        {
            Debug.Log("💧 정수된 물이 아직 없습니다.");
            return;
        }

        IXRSelectInteractor interactor = args.interactorObject;
        if (interactor == null) return;

        NetworkObject handNetObj = (interactor as MonoBehaviour)?.GetComponentInParent<NetworkObject>();
        if (handNetObj != null && handNetObj.HasInputAuthority)
        {
            RPC_RequestTakeWater();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPurifying(NetworkObject bucketNetObj)
    {
        if (isPurifying || hasWater) return;

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
            hasWater = true;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestTakeWater()
    {
        if (!hasWater) return;

        if (cleanWaterItemPrefab.IsValid && Runner.IsRunning)
        {
            Vector3 spawnPos = waterObject != null ? waterObject.transform.position : transform.position;
            Runner.Spawn(cleanWaterItemPrefab, spawnPos, Quaternion.identity);
        }

        hasWater = false;
        RPC_BroadcastResetWaterUI();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastResetWaterUI()
    {
        if (waterObject != null)
        {
            waterObject.SetActive(false);
        }
    }
}