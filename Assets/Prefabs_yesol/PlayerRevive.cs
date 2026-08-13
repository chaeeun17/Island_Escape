using UnityEngine;

public class PlayerRevive : MonoBehaviour, IInteractable
{
    [SerializeField] private NetworkPlayerStats stats;
    public string GetInteractText() 
    {
        return ("일으켜주기");
    }

    public void Interact() 
    {
        stats.Revive();
    }
}
