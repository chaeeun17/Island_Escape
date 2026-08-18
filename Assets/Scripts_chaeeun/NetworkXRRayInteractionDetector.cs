using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;
using Fusion; // 🌟 Fusion 라이브러리

public class NetworkXRRayInteractionDetector : MonoBehaviour
{
    [Header("하이라이트용 머티리얼")]
    public Material highlightMaterial;

    [Header("상호작용 UI 설정")]
    public InteractionGuideUI guideUI; 

    [Header("X 버튼 입력 매핑 (인풋 액션)")]
    [SerializeField] private InputActionReference xButtonActionReference;

    // 🌟 XRRayInteractor 컴포넌트
    private XRRayInteractor rayInteractor;
    private MeshRenderer previousRenderer;
    private Material previousOriginalMaterial;
    private IInteractable currentTargetInteractable;

    private NetworkObject localNetworkObject;

    void Awake()
    {
        rayInteractor = GetComponent<XRRayInteractor>();
        localNetworkObject = GetComponentInParent<NetworkObject>();
    }

    void OnEnable()
    {
        // 내 네트워크 캐릭터(HasInputAuthority)가 아닐 때는 이 감지 스크립트를 끕니다.
        if (localNetworkObject != null && !localNetworkObject.HasInputAuthority)
        {
            enabled = false;
            return;
        }

        if (rayInteractor != null)
        {
            rayInteractor.hoverEntered.AddListener(OnHoverEntered);
            rayInteractor.hoverExited.AddListener(OnHoverExited);
            rayInteractor.selectEntered.AddListener(OnSelectEntered);
        }

        if (xButtonActionReference != null)
        {
            xButtonActionReference.action.Enable();
            xButtonActionReference.action.performed += OnXButtonPressed;
        }
    }

    void OnDisable()
    {
        if (rayInteractor != null)
        {
            rayInteractor.hoverEntered.RemoveListener(OnHoverEntered);
            rayInteractor.hoverExited.RemoveListener(OnHoverExited);
            rayInteractor.selectEntered.RemoveListener(OnSelectEntered);
        }

        if (xButtonActionReference != null)
        {
            xButtonActionReference.action.performed -= OnXButtonPressed;
            xButtonActionReference.action.Disable();
        }

        ResetDetectorState();
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // 🌟 1. 내 손(Interactor)이 이미 무언가를 잡고 있는 중이라면 하이라이트 스킵
        if (rayInteractor != null && rayInteractor.hasSelection) return;

        // 🌟 2. 레이저에 닿은 대상이 이미 잡혀있는(isSelected) 상태라면 하이라이트 스킵
        if (args.interactableObject is IXRSelectInteractable selectInteractable && selectInteractable.isSelected)
        {
            return;
        }

        var grabObject = args.interactableObject.transform.gameObject;
        if (grabObject == null) return;

        MeshRenderer targetRenderer = grabObject.GetComponentInChildren<MeshRenderer>();
        
        // IInteractable 인터페이스를 부모나 자식 오브젝트에서 탐색
        IInteractable targetInteractable = grabObject.GetComponentInParent<IInteractable>();
        if (targetInteractable == null)
        {
            targetInteractable = grabObject.GetComponentInChildren<IInteractable>();
        }

        ResetDetectorState(); 

        // 하이라이트 머티리얼 교체
        if (targetRenderer != null && highlightMaterial != null)
        {
            if (targetRenderer.sharedMaterial != highlightMaterial)
            {
                previousRenderer = targetRenderer;
                previousOriginalMaterial = targetRenderer.material; // 원본 저장
            }
            
            targetRenderer.material = highlightMaterial; // 하이라이트 적용
        }

        // 상호작용 UI 가이드 출력
        currentTargetInteractable = targetInteractable;
        if (currentTargetInteractable != null && guideUI != null)
        {
            guideUI.ShowUI(true, grabObject.transform);
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        ResetDetectorState();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // 🌟 3. 물건을 잡는 순간 하이라이트 및 상호작용 UI 가이드 즉시 해제
        ResetDetectorState();
    }

    private void ResetDetectorState()
    {
        if (previousRenderer != null && previousOriginalMaterial != null)
        {
            previousRenderer.material = previousOriginalMaterial;
        }
        
        if (guideUI != null) 
        {
            guideUI.ShowUI(false);
        }
        
        previousRenderer = null;
        previousOriginalMaterial = null;
        currentTargetInteractable = null;
    }

    private void OnXButtonPressed(InputAction.CallbackContext context)
    {
        // 손에 물건을 쥐고 있지 않을 때만 X 버튼 상호작용 수행
        if (rayInteractor != null && rayInteractor.hasSelection) return;

        if (currentTargetInteractable != null) 
        {
            currentTargetInteractable.Interact();
        }

        Debug.Log("X버튼 클릭됨");
    }
}