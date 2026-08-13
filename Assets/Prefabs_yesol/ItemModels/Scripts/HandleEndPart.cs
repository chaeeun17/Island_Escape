using UnityEngine;
using Fusion;
public class HandleEndPart : NetworkBehaviour
{
    public bool isOnPedestal = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;

        if (other.CompareTag("Pedestal"))
        {
            isOnPedestal = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasStateAuthority) return;

        if (other.CompareTag("Pedestal"))
        {
            isOnPedestal = false;
        }
    }
}
