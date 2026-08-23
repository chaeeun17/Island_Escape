using Fusion;
using UnityEngine;

public class NetworkGameOverManager : NetworkBehaviour
{
    [Header("Check Settings")]
    [SerializeField] private float checkInterval = 1f;

    [Networked] private NetworkBool IsGameOver { get; set; }

    private float _checkTimer = 0f;
    private bool _uiShown = false;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (IsGameOver) return; // 🌟 이미 어떤 방식으로든 게임이 끝났으면 더 이상 체크 안 함

        _checkTimer += Runner.DeltaTime;
        if (_checkTimer < checkInterval) return;
        _checkTimer = 0f;

        if (AreAllPlayersFainted())
        {
            IsGameOver = true;
            RPC_BroadcastGameOver();
        }
    }

    private bool AreAllPlayersFainted()
    {
        bool anyPlayerFound = false;

        foreach (var playerRef in Runner.ActivePlayers)
        {
            var playerObject = Runner.GetPlayerObject(playerRef);
            if (playerObject == null) continue;

            var stats = playerObject.GetComponent<NetworkPlayerStats>();
            if (stats == null) continue;

            anyPlayerFound = true;

            if (!stats.IsPlayerFainted())
            {
                return false;
            }
        }

        return anyPlayerFound;
    }

    // 🌟 헬기 탈출 결과 등, 외부 이벤트로 세션을 최종 종료할 때 호출
    //    (StateAuthority에서만 호출되어야 함)
    public void EndGameSession()
    {
        if (!Object.HasStateAuthority) return;
        IsGameOver = true; // 이 플래그로 이후 "전원 기절" 체크가 영구히 중단됨
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastGameOver()
    {
        if (_uiShown) return;
        _uiShown = true;

        var localStats = NetworkPlayerStats.LocalInstance;
        if (localStats != null)
        {
            var uiManager = localStats.GetComponent<NetworkUIManager>();
            if (uiManager != null)
            {
                uiManager.ShowFailUI();
            }
        }

        Debug.Log("💀 모든 플레이어가 기절했습니다. 게임 종료.");
    }
}