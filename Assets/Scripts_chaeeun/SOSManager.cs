using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using System;

public class SOSManager : MonoBehaviour
{
    public static SOSManager Instance { get; private set; }

    [Header("SOS를 구성하는 총 소켓 개수")]
    [SerializeField] private int totalRequiredSlots = 9;
    private int currentFilledSlots = 0;

    [Header("성공 UI")]
    [SerializeField] private GameObject successCutsceneObject;

    void Awake()
    {
        if (Instance == null) Instance = Instance = this;
        else Destroy(gameObject);
    }

    // 소켓에 돌이 들어왔을 때 호출될 함수
    public void SlotFilled(XRSocketInteractor socket)
    {
        if (socket == null) return;

        currentFilledSlots++;
        Debug.Log($"[SOS] 돌 배치됨! ({currentFilledSlots} / {totalRequiredSlots})");

        socket.interactionLayers = 0;
        socket.enabled = false;

        var interactable = socket.hasSelection ? socket.interactablesSelected[0] : null;
    
        if (interactable != null)
        {
            GameObject stoneObject = interactable.transform.gameObject;
            
            var grabComponent = stoneObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabComponent != null)
            {
                grabComponent.enabled = false; 
            }

            // (선택사항) 만약 물리 엔진(Rigidbody) 때문에 돌이 굴러떨어질까 봐 걱정된다면 
            // 리짓바디를 완전히 정지(Kinematic) 시켜버리는 게 가장 안전합니다.
            var rb = stoneObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; 
                rb.linearVelocity = Vector3.zero; // 구버전 유니티면 velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (currentFilledSlots >= totalRequiredSlots)
        {
            TriggerSOSSuccess();
        }
    }

    public void OnSocketSelected(SelectEnterEventArgs args)
    {
        if (args == null) return;

        XRGrabInteractable interactable = args.interactableObject as XRGrabInteractable;
        XRSocketInteractor interactor = args.interactorObject as XRSocketInteractor;

        if (interactable == null || interactor == null) return;

        GameObject incomingObject = interactable.gameObject;

        // 돌이 맞는지 확인
        if (incomingObject.CompareTag("Stone"))
        {
            currentFilledSlots++;
            Debug.Log($"[SOS] 돌 배치 성공! ({currentFilledSlots} / {totalRequiredSlots})");

            // 돌멩이 Grab 기능 끄기
            interactable.enabled = false; 

            // 물리 엔진 고정
            var rb = incomingObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 소켓 기능 자체를 끔
            interactor.enabled = false;

            // 모든 슬롯이 다 잠겼다면 전체 미션 성공 처리
            if (currentFilledSlots >= totalRequiredSlots)
            {
                TriggerSOSSuccess();
            }
        }
        else
        {
            interactor.interactionManager.SelectCancel((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable);
        }
    }

    // 소켓에서 돌이 빠졌을 때 호출될 함수
    public void SlotEmptied()
    {
        currentFilledSlots = Mathf.Max(0, currentFilledSlots - 1);
    }

    private void TriggerSOSSuccess()
    {
        Debug.Log("SOS 신호 완성!");
        NetworkTutorialManager.Instance.CompleteCurrentStep();
    }
}
