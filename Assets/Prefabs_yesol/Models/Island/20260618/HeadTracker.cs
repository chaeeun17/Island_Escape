using UnityEngine;
using UnityEngine.InputSystem;

public class HeadTracker : MonoBehaviour
{
    public Transform head;
    public Transform body;

    // public InputActionReference PositionInput;
    public InputActionReference RotationInput;
    // public InputActionReference TrackingStateInput;

    private void LateUpdate()
    {
        Quaternion rotValue = RotationInput.action.ReadValue<Quaternion>();
        head.rotation = rotValue * Quaternion.Euler(0, 180, 0);

        float yaw = rotValue.eulerAngles.y;
        body.rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 180, 0); ;
    }
}
