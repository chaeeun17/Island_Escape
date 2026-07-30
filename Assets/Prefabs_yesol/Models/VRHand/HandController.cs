using Fusion;
using UnityEngine;

public class HandController : NetworkBehaviour
{
    private Animator animator;

    [Networked] public float Grip { get; set; }
    [Networked] public float Trigger { get; set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public override void FixedUpdateNetwork()
    {
        animator.SetFloat("Grip", Grip);
        animator.SetFloat("Trigger", Trigger);
    }
}
