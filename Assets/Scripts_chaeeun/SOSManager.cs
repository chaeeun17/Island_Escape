using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using System;
using Fusion; // Fusion 네임스페이스

public class SOSManager : NetworkBehaviour
{
    public static SOSManager Instance { get; private set; }

    [Header("SOS를 구성하는 총 소켓 개수")]
    [SerializeField] private int totalRequiredSlots = 9;

    [Networked]
    private int currentFilledSlots { get; set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject tutorialNoticeUI;   // 🌟 시작 시 떠오를 튜토리얼 안내 UI
    [SerializeField] private GameObject successCutsceneObject; // 성공 시 연출 UI

    public event Action OnTutorialCompleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 🌟 Fusion 네트워크 오브젝트가 씬에 스폰되었을 때 (게임 시작 시) 호출
    public override void Spawned()
    {
        // 내 화면(클라이언트)에 튜토리얼 UI 표시
        if (tutorialNoticeUI != null)
        {
            tutorialNoticeUI.SetActive(true);
            
            // (선택사항) 5초 뒤에 튜토리얼 안내창을 자동으로 끄고 싶다면 주석 해제
            // StartCoroutine(HideTutorialAfterDelay(5f));
        }

        if (successCutsceneObject != null)
        {
            successCutsceneObject.SetActive(false);
        }
    }

    // 튜토리얼 UI의 '확인' 버튼을 클릭할 때 연결해 줄 함수
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

    private IEnumerator HideTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tutorialNoticeUI != null)
        {
            tutorialNoticeUI.SetActive(false);
        }
    }

    /// <summary>
    /// 소켓에 돌이 들어왔을 때 호출
    /// </summary>
    public void OnSocketSelected(SelectEnterEventArgs args)
    {
        if (args == null) return;

        XRGrabInteractable interactable = args.interactableObject as XRGrabInteractable;
        XRSocketInteractor interactor = args.interactorObject as XRSocketInteractor;

        if (interactable == null || interactor == null) return;

        GameObject incomingObject = interactable.gameObject;

        string objName = incomingObject.name;

        if (objName.Equals("Item_Stone(Clone)") || objName.Contains("Item_Stone"))
        {
            RPC_AddSlotCount();

            // 1. 돌 Grab 기능 끄기
            interactable.enabled = false; 

            // 2. 물리 고정
            var rb = incomingObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 3. 소켓 기능 끄기
            interactor.enabled = false;
        }
        else
        {
            if (interactor.interactionManager != null)
            {
                interactor.interactionManager.SelectCancel((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable);
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
        
        // 성공 시 혹시 떠있던 튜토리얼 UI도 끄고 성공 연출 켜기
        if (tutorialNoticeUI != null) tutorialNoticeUI.SetActive(false);
        if (successCutsceneObject != null) successCutsceneObject.SetActive(true);

        OnTutorialCompleted?.Invoke();
    }
}