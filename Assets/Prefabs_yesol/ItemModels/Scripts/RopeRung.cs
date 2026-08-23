using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RopeRung : MonoBehaviour
{
    [SerializeField] private HelicopterController helicopterController;
    private XRSimpleInteractable _simpleInteractable; // 🌟 XRGrabInteractable → XRSimpleInteractable

    private void Awake()
    {
        _simpleInteractable = GetComponent<XRSimpleInteractable>();
        if (helicopterController == null)
        {
            helicopterController = GetComponentInParent<HelicopterController>();
        }
    }

    private void OnEnable()
    {
        _simpleInteractable.selectEntered.AddListener(OnGrab);
        _simpleInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        _simpleInteractable.selectEntered.RemoveListener(OnGrab);
        _simpleInteractable.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        var interactorTransform = args.interactorObject.transform;
        var playerRoot = interactorTransform.root;
        var networkObject = playerRoot.GetComponent<NetworkObject>();
        var rider = playerRoot.GetComponent<PlayerHelicopterRider>();

        if (networkObject != null && networkObject.HasInputAuthority)
        {
            if (rider != null)
            {
                rider.AttachToLadder(transform, interactorTransform, helicopterController);
            }
            if (helicopterController != null)
            {
                helicopterController.RPC_RegisterEscapedPlayer(networkObject.InputAuthority, true);
            }
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        var interactorTransform = args.interactorObject.transform;
        var playerRoot = interactorTransform.root;
        var networkObject = playerRoot.GetComponent<NetworkObject>();
        var rider = playerRoot.GetComponent<PlayerHelicopterRider>();

        if (networkObject != null && networkObject.HasInputAuthority)
        {
            if (rider != null)
            {
                rider.DetachHand(interactorTransform);
            }

            if (helicopterController != null && (rider == null || !rider.IsCurrentlyAttached))
            {
                helicopterController.RPC_RegisterEscapedPlayer(networkObject.InputAuthority, false);
            }
        }
    }
}