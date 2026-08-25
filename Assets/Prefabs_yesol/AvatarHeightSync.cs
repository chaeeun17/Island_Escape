using Fusion;
using UnityEngine;
using Unity.XR.CoreUtils;

public class AvatarHeightSync : NetworkBehaviour
{
    // 아바타 시각적 크기 - 네트워크로 동기화되는 값
    [Networked, OnChangedRender(nameof(OnAvatarScaleChanged))]
    public float AvatarScale { get; set; } = 1f;

    [SerializeField] private Transform avatarHeadRoot; // 아바타 메쉬 루트 (XR Origin 아님!)
    [SerializeField] private Transform avatarBodyRoot; // 아바타 메쉬 루트 (XR Origin 아님!)
    [SerializeField] private XROrigin xrOrigin;           // 로컬 트래킹용 (Input Authority만 사용)
    [SerializeField] private float avatarDefaultEyeHeight = 1.7f;

    public override void Spawned()
    {
        GameObject xrOriginObject = GameObject.FindWithTag("XROrigin");
        if (xrOriginObject != null)
        {
            xrOrigin = xrOriginObject.GetComponent<XROrigin>();
        }

        // 🌟 모든 피어에서, 이 시점의 네트워크 값을 명시적으로 즉시 반영
        //    (OnChangedRender가 "최초 수신"에는 발동하지 않을 수 있으므로 안전장치)
        ApplyVisualScale(AvatarScale);

        if (HasInputAuthority)
        {
            CalibrateHeight();
        }
    }

    // 로컬 사용자가 캘리브레이션 버튼을 눌렀을 때 호출
    public void CalibrateHeight()
    {
        if (!HasInputAuthority) return; // 내 아바타에 대해서만 실행

        float realHeight = xrOrigin.CameraInOriginSpaceHeight;
        float scale = realHeight / avatarDefaultEyeHeight;

        RPC_RequestSetAvatarScale(scale);
    }

    // Host 모드 기준: 클라이언트 -> StateAuthority(호스트)로 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSetAvatarScale(float scale)
    {
        AvatarScale = scale; // StateAuthority가 값을 바꾸면 자동으로 모든 클라이언트에 리플리케이션됨
    }

    // [Networked] 값이 바뀔 때만 호출됨 (매 프레임 X, 효율적)
    private void OnAvatarScaleChanged()
    {
        ApplyVisualScale(AvatarScale);
    }

    [SerializeField] private Transform avatarLeftHandVisual;  // 🌟 손 메쉬 전용 (트래킹 Transform의 자식)
    [SerializeField] private Transform avatarRightHandVisual; // 🌟 손 메쉬 전용

    [Header("Hand Scale Damping")]
    [SerializeField, Range(0f, 1f)] private float handGrowDamping = 0.5f;   // scale > 1일 때 (커질 때) 감쇠율
    [SerializeField, Range(0f, 1f)] private float handShrinkDamping = 1f;   // scale < 1일 때 (작아질 때) 감쇠율 - 기본 1.0이면 실제 비율 그대로

    private void ApplyVisualScale(float scale)
    {
        if (avatarHeadRoot != null)
            avatarHeadRoot.localScale = Vector3.one * scale;
        if (avatarBodyRoot != null)
            avatarBodyRoot.localScale = Vector3.one * scale;

        float dampedHandScale;
        if (scale >= 1f)
        {
            // 🌟 1보다 커지는 방향은 damping을 강하게 걸어서 실제 scale보다 작게
            dampedHandScale = 1f + (scale - 1f) * handGrowDamping;
        }
        else
        {
            // 작아지는 방향은 별도 damping (기본값 1.0 = 그대로 반영)
            dampedHandScale = 1f - (1f - scale) * handShrinkDamping;
        }

        if (avatarLeftHandVisual != null)
            avatarLeftHandVisual.localScale = new Vector3 (-0.1f,0.1f,0.1f) * dampedHandScale;
        if (avatarRightHandVisual != null)
            avatarRightHandVisual.localScale = new Vector3 (0.1f,0.1f,0.1f) * dampedHandScale;
    }
}