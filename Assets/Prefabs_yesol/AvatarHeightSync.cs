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
        // 스폰 시점에 현재 값으로 즉시 반영 (늦게 들어온 클라이언트 대비)
        GameObject xrOriginObject = GameObject.FindWithTag("XROrigin");
        xrOrigin = xrOriginObject.GetComponent<XROrigin>();

        // ApplyVisualScale(AvatarScale);
        CalibrateHeight();
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

    private void ApplyVisualScale(float scale)
    {
        if (avatarHeadRoot != null)
            avatarHeadRoot.localScale = Vector3.one * scale;

        if (avatarBodyRoot != null)
            avatarBodyRoot.localScale = Vector3.one * scale;
    }
}