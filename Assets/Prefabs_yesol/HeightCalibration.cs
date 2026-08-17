using UnityEngine;
using Unity.XR.CoreUtils;

public class HeightCalibration : MonoBehaviour
{
    public XROrigin xrOrigin;
    public Transform avatarHead;
    public Transform avatarBody;
    public float avatarDefaultEyeHeight = 1.7f; // 아바타 기본 눈높이(모델링 기준값)

    // 사용자가 똑바로 서 있는 상태에서 버튼 누르면 호출
    public void CalibrateHeight()
    {
        float realHeight = xrOrigin.CameraInOriginSpaceHeight;
        // 또는: xrOrigin.Camera.transform.localPosition.y

        float scale = realHeight / avatarDefaultEyeHeight;

        // 방법 A: XR Origin 자체를 스케일 (가장 간단, 손 크기도 같이 스케일됨)
        // xrOrigin.transform.localScale = Vector3.one * scale;

        // 방법 B: 아바타만 스케일 (아바타 표현용, 손 컨트롤러 트래킹은 그대로)
        avatarHead.localScale = Vector3.one * scale;
        avatarBody.localScale = Vector3.one * scale;
    }
}