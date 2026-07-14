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
        if (caster.state == FloatState.Floating)
        {
            Debug.Log("낚싯줄 회수!");
            caster.StartReeling();
        }
        else
        {
            Debug.Log("버튼이 해제되었습니다!");

            Vector3 dir = (rodTip.forward * 0.9f + CurrentVelocity.normalized * 0.1f).normalized;
            caster.Cast(dir * CurrentSpeed * 2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Water"))
        {
            caster.ResetFloat();
            return;
        }

        caster.OnHitWater(transform.position);
    }
}