using UnityEngine;
using UnityEngine.InputSystem;

public class RodSwingDetector : MonoBehaviour
{
    public FishingCaster caster;

    // 인스펙터에서 Input Action Reference를 할당합니다.
    [SerializeField] private InputActionReference triggerActionReference;
    public Transform rodTip;

    public float CurrentSpeed { get; private set; }
    public Vector3 CurrentVelocity { get; private set; }

    Vector3 lastPosition;

    public bool released = false;

    private void OnEnable()
    {
        if (triggerActionReference != null)
        {
            // 1. 액션을 활성화합니다.
            triggerActionReference.action.Enable();

            // 2. 버튼이 완전히 해제되었을 때(Canceled) 실행할 함수를 등록합니다.
            triggerActionReference.action.canceled += OnTriggerReleased;
        }
    }

    private void OnDisable()
    {
        if (triggerActionReference != null)
        {
            // 메모리 누수 방지를 위해 이벤트 구독을 해제합니다.
            triggerActionReference.action.canceled -= OnTriggerReleased;
            triggerActionReference.action.Disable();
        }
    }

    void Start()
    {
        lastPosition = rodTip.position;
    }

    void Update()
    {
        CurrentVelocity = (rodTip.position - lastPosition) / Time.deltaTime;
        CurrentSpeed = CurrentVelocity.magnitude;

        lastPosition = rodTip.position;
    }

    private void OnTriggerReleased(InputAction.CallbackContext context)
    {
        Debug.Log("버튼이 해제되었습니다!");

        Vector3 dir = (rodTip.forward * 0.7f + CurrentVelocity.normalized * 0.3f).normalized;

        caster.Cast(CurrentVelocity * 1.8f);
        // Rigidbody rb = GetComponent<Rigidbody>();

        //rb.linearVelocity = dir * CurrentSpeed * 1.8f;
        // rb.linearVelocity = CurrentVelocity * 1.8f;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        if (other.CompareTag("Fishingrod"))
        {
            return;
        }

        if (!other.CompareTag("Water"))
        {
            caster.ResetFloat();
            return;
        }

        caster.OnHitWater(transform.position);
    }
}