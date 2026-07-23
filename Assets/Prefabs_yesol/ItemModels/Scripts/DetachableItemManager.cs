using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DetachableItemManager : MonoBehaviour
{
    [Header("Child Grab Interactable")]
    [SerializeField] private XRGrabInteractable[] childInteractables;

    private XRGrabInteractable mainInteractable;
    private Rigidbody mainRigidbody;
    private bool isActivated = false;

    void Awake()
    {
        mainInteractable = GetComponent<XRGrabInteractable>();
        mainRigidbody = GetComponent<Rigidbody>();

        foreach (var child in childInteractables)
        {
            child.enabled = false;

            if (child.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }
        }

        mainInteractable.activated.AddListener(OnItemActivated);
    }

    private void OnItemActivated(ActivateEventArgs args)
    {
        if (isActivated) return;
        isActivated = true;

        DetachParts();
    }

    private void DetachParts()
    {
        foreach (var child in childInteractables)
        {
            child.transform.SetParent(null);

            if (child.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
            }

            child.enabled = true;
        }

        mainInteractable.enabled = false;

        if (TryGetComponent<Collider>(out var mainCollider)) mainCollider.enabled = false;
        Destroy(mainInteractable);
        Destroy(mainRigidbody);
    }

    void OnDestroy()
    {
        if (mainInteractable != null)
        {
            mainInteractable.activated.RemoveListener(OnItemActivated);
        }
    }
}