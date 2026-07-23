using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    [Header("Target settings")]
    public Transform targetCamera; // VR 메인 카메라 Transform
    public float distance = 1.5f;   // 카메라(유저)로부터 떨어질 거리
    public float followSpeed = 5.0f;// 따라오는 속도 (낮을수록 부드럽게 지연되며 따라옵니다)

    [Header("Options")]
    public bool keepUpright = true; // UI가 위아래로 기울어지지 않고 수직을 유지하게 할지 여부

    private void Start()
    {
        // 지정된 카메라가 없다면 Main Camera를 자동으로 찾아 할당합니다.
        if (targetCamera == null)
        {
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogError("메인 카메라를 찾을 수 없습니다. 인스펙터에서 카메라를 직접 할당해주세요.");
            }
        }

        // 게임 시작 시 혹은 활성화 시, 멀리서 날아오지 않고 즉시 시야 앞으로 텔레포트 시킵니다.
        SnapToCameraFront();
    }

    private void OnEnable()
    {
        // 패널이 다시 켜질 때 즉시 플레이어 앞으로 이동시킵니다.
        SnapToCameraFront();
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // 1. 목표 위치 계산 (카메라 앞 방향 벡터 * 거리)
        Vector3 targetPosition = targetCamera.position + (targetCamera.forward * distance);

        // 2. 현재 위치에서 목표 위치로 부드럽게 보간 이동 (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        // 3. UI가 항상 카메라를 바라보도록 회전 (Slerp)
        Vector3 directionToCamera = transform.position - targetCamera.position;

        if (keepUpright)
        {
            // Y축 회전만 남겨 UI가 위아래로 꺾여 보이지 않게 방지합니다.
            directionToCamera.y = 0;
        }

        if (directionToCamera != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
    }

    // 창이 처음 열릴 때 플레이어 정면에 즉시 위치시키는 메서드
    public void SnapToCameraFront()
    {
        if (targetCamera == null) return;

        Vector3 targetPosition = targetCamera.position + (targetCamera.forward * distance);
        transform.position = targetPosition;

        Vector3 directionToCamera = transform.position - targetCamera.position;
        if (keepUpright) directionToCamera.y = 0;

        if (directionToCamera != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }
}