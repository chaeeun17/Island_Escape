using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;
using Fusion;

public class NetworkXRRayInteractionDetector : MonoBehaviour
{
    [Header("하이라이트용 머티리얼")]
    public Material highlightMaterial;

    [Header("상호작용 UI 설정")]
    public InteractionGuideUI guideUI; 

    [Header("X 버튼 입력 매핑 (인풋 액션)")]
    [SerializeField] private InputActionReference xButtonActionReference;

    [Header("하이라이트 제외할 태그 (선택 사항)")]
    [SerializeField] private string ignoreTag = "Water";

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
        // 1. 내 손에 이미 잡고 있는 물건이 있다면 패스
        if (rayInteractor != null && rayInteractor.hasSelection) return;

        // 2. 다른 사람이나 내 다른 손에 이미 잡혀 있는 물건이라면 패스
        if (args.interactableObject is IXRSelectInteractable selectInteractable && selectInteractable.isSelected)
        {
            return;
        }

        var grabObject = args.interactableObject.transform.gameObject;
        if (grabObject == null) return;

        // 3. IInteractable 컴포넌트 탐색
        IInteractable targetInteractable = grabObject.GetComponentInParent<IInteractable>();
        if (targetInteractable == null)
        {
            targetInteractable = grabObject.GetComponentInChildren<IInteractable>();
        }

        ResetDetectorState();

        // 🌟 4. [바다 제외 로직]
        // - 태그가 ignoreTag("Sea")이거나
        // - 이름에 "Sea" 또는 "Ocean" 또는 "Water"가 포함되어 있거나
        // - NetworkSeaWaterScooper 컴포넌트가 붙어있는 경우 하이라이트 적용 제외
        bool isSeaObject = grabObject.CompareTag(ignoreTag) ||
                           grabObject.name.ToLower().Contains("sea") ||
                           grabObject.name.ToLower().Contains("ocean") ||
                           grabObject.GetComponentInParent<NetworkSeaWaterScooper>() != null;

        bool isPurifierObject = grabObject.name.ToLower().Contains("purifier") ||
                                grabObject.GetComponentInParent<NetworkWaterPurifier>() != null ||
                                grabObject.GetComponentInChildren<NetworkWaterPurifier>() != null;

        bool isBowDrillObject = grabObject.name.ToLower().Contains("bowdrill") ||
                                grabObject.name.ToLower().Contains("bow_drill") ||
                                grabObject.name.ToLower().Contains("bow drill");

        // 바다가 아닐 때만 하이라이트 머티리얼 교체
        if (!isSeaObject && !isPurifierObject && !isBowDrillObject)
        {
            MeshRenderer targetRenderer = grabObject.GetComponentInChildren<MeshRenderer>();
            if (targetRenderer != null && highlightMaterial != null)
            {
                if (targetRenderer.sharedMaterial != highlightMaterial)
                {
                    previousRenderer = targetRenderer;
                    previousOriginalMaterial = targetRenderer.material;
                }
                targetRenderer.material = highlightMaterial;
            }
        }

        // 5. 상호작용 UI 가이드 출력 (IInteractable이 있다면 바다라도 텍스트 가이드는 출력됨)
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
        if (rayInteractor != null && rayInteractor.hasSelection) return;

        if (currentTargetInteractable != null) 
        {
            currentTargetInteractable.Interact();
        }

        Debug.Log("X버튼 클릭됨");
    }
}