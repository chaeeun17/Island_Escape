using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class XRRayInteractionDetector : MonoBehaviour
{
    [Header("하이라이트용 머티리얼")]
    public Material highlightMaterial;

    [Header("상호작용 UI 설정")]
    public InteractionGuideUI guideUI; 

    [Header("X 버튼 입력 매핑 (인풋 액션)")]
    [SerializeField] private InputActionReference xButtonActionReference;

    private NearFarInteractor nearFarInteractor;
    private MeshRenderer previousRenderer;
    private Material previousOriginalMaterial;
    private IInteractable currentTargetInteractable;

    void Awake()
    {
        nearFarInteractor = GetComponent<NearFarInteractor>();
    }

    void OnEnable()
    {
        if (nearFarInteractor != null)
        {
            nearFarInteractor.hoverEntered.AddListener(OnHoverEntered);
            nearFarInteractor.hoverExited.AddListener(OnHoverExited);
            nearFarInteractor.selectEntered.AddListener(OnSelectEntered);
        }

        if (xButtonActionReference != null)
        {
            xButtonActionReference.action.Enable();
            xButtonActionReference.action.performed += OnXButtonPressed;
        }
    }

    void OnDisable()
    {
        if (nearFarInteractor != null)
        {
            nearFarInteractor.hoverEntered.RemoveListener(OnHoverEntered);
            nearFarInteractor.hoverExited.RemoveListener(OnHoverExited);
            nearFarInteractor.selectEntered.RemoveListener(OnSelectEntered);
        }

        if (xButtonActionReference != null)
        {
            xButtonActionReference.action.performed -= OnXButtonPressed;
            xButtonActionReference.action.Disable();
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        var grabObject = args.interactableObject.transform.gameObject;
        if (grabObject == null) return;

        MeshRenderer targetRenderer = grabObject.GetComponentInChildren<MeshRenderer>();
        IInteractable targetInteractable = grabObject.GetComponent<IInteractable>();

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
        // 레이저로 조준하고 있는 타겟이 있을 때만 실행
        if (currentTargetInteractable != null) 
        {
            currentTargetInteractable.Interact();
        }

        Debug.Log("X버튼 클릭됨");
    }
}