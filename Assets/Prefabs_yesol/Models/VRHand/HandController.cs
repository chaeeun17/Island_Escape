using UnityEngine;
using UnityEngine.InputSystem;

public class HandController : MonoBehaviour
{
    public InputActionReference gripInput;
    public InputActionReference triggerInput;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!animator) return;

        float trigger = triggerInput.action.ReadValue<float>();
        float grip = gripInput.action.ReadValue<float>();

        animator.SetFloat("Trigger", trigger);
        animator.SetFloat("Grip", grip);
    }
}
