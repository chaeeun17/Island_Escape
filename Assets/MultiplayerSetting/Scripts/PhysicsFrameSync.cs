using UnityEngine;
using UnityEngine.Rendering;

// 이 프로젝트는 클라이언트 물리 시뮬레이션(Physics.Simulate)을 쓰지 않으므로,
// NetworkRigidbody3D.Render() 등이 LateUpdate 이후/이전 애매한 시점에 transform을 옮겨도
// 콜라이더(Raycast/Trigger 판정용)가 자동으로 따라가지 않는다.
// 개별 스크립트의 LateUpdate 실행 순서에 기대는 대신, 매 프레임 렌더링 직전(모든 Update/LateUpdate/
// Fusion Render 콜백이 끝난 시점)에 한 번 더 SyncTransforms를 호출해 "보이는 위치"와
// "raycast/trigger가 판정하는 위치"가 항상 일치하도록 보장한다.
// 이 프로젝트는 URP(PC_RPAsset)를 사용하므로 Built-in 전용인 Camera.onPreCull 대신
// RenderPipelineManager.beginContextRendering(SRP 공용 훅)을 사용한다.
internal static class PhysicsFrameSync
{
    private static int _lastSyncFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        RenderPipelineManager.beginContextRendering -= HandleBeginContextRendering;
        RenderPipelineManager.beginContextRendering += HandleBeginContextRendering;
        Camera.onPreCull -= HandlePreCull; // Built-in 파이프라인으로 전환되는 경우를 대비한 폴백
        Camera.onPreCull += HandlePreCull;
    }

    private static void HandleBeginContextRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
    {
        Sync();
    }

    private static void HandlePreCull(Camera cam)
    {
        Sync();
    }

    private static void Sync()
    {
        if (Time.frameCount == _lastSyncFrame) return;
        _lastSyncFrame = Time.frameCount;
        Physics.SyncTransforms();
    }
}
