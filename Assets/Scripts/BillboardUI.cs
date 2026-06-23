using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // 씬에 있는 메인 카메라의 위치(Transform)를 찾아 기억합니다.
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // 1. UI가 카메라를 정면으로 바라보도록 회전값을 정렬합니다.
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                             mainCameraTransform.rotation * Vector3.up);
            
            // 2. (선택) 픽셀 아트 감성이나 깔끔한 가독성을 위해 완전히 평면으로 정렬하고 싶다면 아래 코드를 써도 됩니다.
            // transform.rotation = mainCameraTransform.rotation;
        }
    }
}