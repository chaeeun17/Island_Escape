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
    [SerializeField] private CharacterController playerCharacterController;

    private float baseCCHeight;
    private float baseCCRadius;
    private Vector3 baseCCCenter;
    private float baseCCSkinWidth;

    private Transform cachedMouth;
    private float baseMouthLocalY; // scale=1 기준 mouth의 로컬 Y (localPosition은 scale 영향 안 받음)

    [SerializeField] private LayerMask ownAvatarLayer; // 인스펙터에서 "OwnAvatar" 레이어로 지정 (혹은 int layer 하나로)
    [SerializeField] private string ownAvatarLayerName = "OwnAvatar";


    public override void Spawned()
    {
        GameObject xrOriginObject = GameObject.FindWithTag("XROrigin");
        if (xrOriginObject != null)
        {
            xrOrigin = xrOriginObject.GetComponent<XROrigin>();

            if (HasInputAuthority)
            {
                playerCharacterController = xrOriginObject.GetComponentInChildren<CharacterController>();
                if (playerCharacterController != null)
                {
                    baseCCHeight = playerCharacterController.height;
                    baseCCRadius = playerCharacterController.radius;
                    baseCCCenter = playerCharacterController.center;
                    baseCCSkinWidth = playerCharacterController.skinWidth;
                }
            }
        }

        if (avatarHeadRoot != null)
        {
            cachedMouth = avatarHeadRoot.Find("MouthZone");
            if (cachedMouth != null)
                baseMouthLocalY = cachedMouth.localPosition.y; // transform.localPosition은 부모 scale과 무관한 authoring 값
        }

        ApplyVisualScale(AvatarScale);

        if (HasInputAuthority)
        {
            CalibrateHeight();
            HideOwnAvatarFromSelf();
        }
    }
    [Header("Own Avatar Hiding")]
    [SerializeField] private string[] hideTargetNames; // 예: "Head_Mesh", "Body_Mesh" 등 숨길 자식 오브젝트 이름들

    private void HideOwnAvatarFromSelf()
    {
        int ownLayer = LayerMask.NameToLayer(ownAvatarLayerName);
        if (ownLayer < 0)
        {
            Debug.LogWarning($"레이어 '{ownAvatarLayerName}'가 존재하지 않습니다. Project Settings에서 추가하세요.");
            return;
        }

        if (hideTargetNames == null || hideTargetNames.Length == 0)
        {
            Debug.LogWarning("hideTargetNames가 비어있습니다. 숨길 자식 오브젝트 이름을 인스펙터에서 지정하세요.");
            return;
        }

        foreach (string targetName in hideTargetNames)
        {
            SetLayerByName(avatarHeadRoot, targetName, ownLayer);
            SetLayerByName(avatarBodyRoot, targetName, ownLayer);
        }

        // 로컬 카메라의 culling mask에서 해당 레이어 제외
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            xrOrigin.Camera.cullingMask &= ~(1 << ownLayer);
        }
    }

    // root 이하에서 이름이 targetName과 일치하는 자식(자기 자신 포함)만 찾아서 레이어 변경
    private void SetLayerByName(Transform root, string targetName, int layer)
    {
        if (root == null) return;

        if (root.name == targetName)
        {
            root.gameObject.layer = layer;
            // 이 오브젝트 자체는 찾았지만, 그 아래 자식들도 같이 숨겨야 한다면 재귀 유지
            foreach (Transform child in root)
                SetLayerRecursively(child, layer);
            return; // 찾았으면 더 깊이 이름 매칭할 필요 없음 (원하면 return 제거하고 계속 탐색 가능)
        }

        foreach (Transform child in root)
            SetLayerByName(child, targetName, layer);
    }

    private void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }

    // 다른 스크립트(Player)가 참조할 수 있도록 공개
    public float GetHeadYCorrection()
    {
        // mouth가 scale배만큼 pivot에서 벌어진 만큼, 반대로 pivot을 밀어서 상쇄
        return -baseMouthLocalY * AvatarScale;
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
            avatarLeftHandVisual.localScale = new Vector3(-0.1f, 0.1f, 0.1f) * dampedHandScale;
        if (avatarRightHandVisual != null)
            avatarRightHandVisual.localScale = new Vector3(0.1f, 0.1f, 0.1f) * dampedHandScale;

        // 🌟 CharacterController는 로컬 플레이어만
        if (HasInputAuthority && playerCharacterController != null)
        {
            playerCharacterController.height = baseCCHeight * scale;
            playerCharacterController.radius = Mathf.Max(baseCCRadius * scale, 0.05f); // 너무 얇아지지 않게 clamp
            playerCharacterController.center = baseCCCenter * scale;
            playerCharacterController.skinWidth = baseCCSkinWidth * scale; // radius 대비 비율 유지
        }

        // Transform mouth = avatarHeadRoot.Find("MouthZone");
        // xrOrigin.transform.position = new Vector3(avatarHeadRoot.position.x, mouth.position.y, avatarHeadRoot.position.z);
    }
}