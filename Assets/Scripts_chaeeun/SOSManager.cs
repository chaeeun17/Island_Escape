using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using System;
using Fusion;
using System.Collections.Generic;

public class SOSManager : NetworkBehaviour
{
    public static SOSManager Instance { get; private set; }

    [Header("🧪 디버그/테스트 모드")]
    [Tooltip("체크 시 게임 시작 즉시 강제로 미션 완료 신호를 전체 플레이어에게 보냅니다.")]
    [SerializeField] private bool testForceComplete = false;

    [Header("SOS를 구성하는 총 소켓 개수")]
    [SerializeField] private int totalRequiredSlots = 9;

    [Networked]
    private int currentFilledSlots { get; set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject tutorialNoticeUI;    // 시작 시 띄울 튜토리얼 안내 UI
    [SerializeField] private GameObject successCutsceneObject; // 성공 시 연출 UI

    public event Action OnTutorialCompleted;

    [Header("관리할 소켓 목록 (Inspector에서 드래그 연결)")]
    [SerializeField] private List<XRSocketInteractor> socketInteractors = new List<XRSocketInteractor>();

    private bool isGameStarted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void Spawned()
    {
        // 🌟 시작 시 자동으로 UI가 켜지지 않도록 초기에는 모두 비활성화
        if (tutorialNoticeUI != null) tutorialNoticeUI.SetActive(false);
        if (successCutsceneObject != null) successCutsceneObject.SetActive(false);

    }

    /// <summary>
    /// 🌟 [외부 호출용] SOS 미션을 시작시키는 함수 (로컬 또는 RPC 호출)
    /// </summary>
    public void StartSOSGame()
    {
        if (Object.HasStateAuthority)
        {
            RPC_StartSOSGameAll();
        }
        else
        {
            // 클라이언트에서 호출 시 호스트에게 시작 요청
            RPC_RequestStartSOSGame();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestStartSOSGame()
    {
        RPC_StartSOSGameAll();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartSOSGameAll()
    {
        if (isGameStarted) return;
        isGameStarted = true;

        Debug.Log("🚀 [SOSManager] SOS 미션이 시작되었습니다!");

        // 🌟 테스트 강제 완료가 켜져 있으면 즉시 성공 트리거 호출
        if (testForceComplete)
        {
            Debug.Log("🧪 [SOSManager] 테스트 모드 활성화: 즉시 미션 완료 신호를 브로드캐스트합니다.");
            if (Object.HasStateAuthority)
            {
                RPC_TriggerSuccessAll();
            }
            return;
        }

        // 튜토리얼 UI 활성화
        if (tutorialNoticeUI != null)
        {
            tutorialNoticeUI.SetActive(true);
        }

        if (successCutsceneObject != null)
        {
            successCutsceneObject.SetActive(false);
        }
    }

    // 튜토리얼 UI의 '확인' 버튼 클릭 시
    public void OnClickCloseTutorial()
    {
        if (tutorialNoticeUI != null)
        {
            tutorialNoticeUI.SetActive(false);
        }

        if (successCutsceneObject != null)
        {
            successCutsceneObject.SetActive(false);
        }
    }

    public void OnSocketSelected(SelectEnterEventArgs args)
    {
        if (!isGameStarted || args == null) return;

        XRGrabInteractable interactable = args.interactableObject as XRGrabInteractable;
        XRSocketInteractor interactor = args.interactorObject as XRSocketInteractor;

        if (interactable == null || interactor == null) return;

        GameObject incomingObject = interactable.gameObject;
        string objName = incomingObject.name;

        if (objName.Equals("Item_Stone(Clone)") || objName.Contains("Item_Stone"))
        {
            int socketIndex = socketInteractors.IndexOf(interactor);

            NetworkObject stoneNetworkObj = incomingObject.GetComponentInParent<NetworkObject>();
            NetworkId stoneId = stoneNetworkObj != null ? stoneNetworkObj.Id : default;

            RPC_AddSlotCount();
            RPC_SyncDisableSocket(socketIndex, stoneId);
        }
        else
        {
            if (interactor.interactionManager != null)
            {
                interactor.interactionManager.SelectCancel((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SyncDisableSocket(int socketIndex, NetworkId stoneId)
    {
        StartCoroutine(DisableSocketAndFixStoneRoutine(socketIndex, stoneId));
    }

    private IEnumerator DisableSocketAndFixStoneRoutine(int socketIndex, NetworkId stoneId)
    {
        yield return null;

        // 돌 물리 고정 및 인터랙션 해제
        if (stoneId != default && Runner.TryFindObject(stoneId, out NetworkObject stoneObj))
        {
            var grab = stoneObj.GetComponentInChildren<XRGrabInteractable>();
            if (grab != null) grab.enabled = false;

            var rb = stoneObj.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // 해당 소켓 끄기
        if (socketIndex >= 0 && socketIndex < socketInteractors.Count)
        {
            var targetSocket = socketInteractors[socketIndex];
            if (targetSocket != null)
            {
                targetSocket.enabled = false;
                targetSocket.gameObject.SetActive(false);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_AddSlotCount()
    {
        currentFilledSlots++;
        Debug.Log($"[SOS] 돌 배치 성공! ({currentFilledSlots} / {totalRequiredSlots})");

        if (currentFilledSlots >= totalRequiredSlots)
        {
            RPC_TriggerSuccessAll();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerSuccessAll()
    {
        Debug.Log("🎉 [SOS] 신호 완성! 모든 플레이어 성공!");          
        
        if (tutorialNoticeUI != null) tutorialNoticeUI.SetActive(false);
        if (successCutsceneObject != null) successCutsceneObject.SetActive(true);

        OnTutorialCompleted?.Invoke();
    }
}