using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RopeRung : MonoBehaviour
{
    [SerializeField] private HelicopterController helicopterController;
    private XRGrabInteractable _grabInteractable;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        _grabInteractable.selectEntered.AddListener(OnGrab);
        _grabInteractable.selectExited.AddListener(OnRelease); 
    }

    private void OnDisable()
    {
        _grabInteractable.selectEntered.RemoveListener(OnGrab);
        _grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        var interactorTransform = args.interactorObject.transform; // 잡은 컨트롤러/손 Transform
        var playerRoot = interactorTransform.root;
        var networkObject = playerRoot.GetComponent<NetworkObject>();
        var rider = playerRoot.GetComponent<PlayerHelicopterRider>();

        if (networkObject != null && networkObject.HasInputAuthority)
        {
            if (rider != null)
            {
                // 잡은 손(interactorTransform)을 함께 전달
                rider.AttachToLadder(transform, interactorTransform);
            }

            if (helicopterController != null)
            {
                helicopterController.RPC_RegisterEscapedPlayer(networkObject.InputAuthority, true);
            }
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        var playerRoot = args.interactorObject.transform.root;
        var networkObject = playerRoot.GetComponent<NetworkObject>();
        var rider = playerRoot.GetComponent<PlayerHelicopterRider>();

        if (networkObject != null && networkObject.HasInputAuthority)
        {
            // 다른 손으로 여전히 사다리 다른 발판을 잡고 있는지 확인 (양손 클라이밍 대응)
            bool isStillClimbing = _grabInteractable.isSelected;

            if (!isStillClimbing)
            {
                // 완전히 손을 놓았을 때만 탑승 해제
                if (rider != null)
                {
                    rider.DetachFromLadder();
                }

                if (helicopterController != null)
                {
                    helicopterController.RPC_RegisterEscapedPlayer(networkObject.InputAuthority, false);
                }
            }
        }
    }
}