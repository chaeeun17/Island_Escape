using UnityEngine;
using System.Collections;
using Fusion;

public class NetworkTent : NetworkBehaviour
{
    [Networked] private NetworkBool isPlayerSleeping { get; set; }

    [Header("페이드 연출 시간 설정")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float sleepDelay = 5.0f;

    [Header("퇴장 위치")]
    [SerializeField] private Transform exitPoint;

    private void OnTriggerEnter(Collider other)
    {
        // 1. ReviveRange 무시
        if (other.gameObject.name.Contains("ReviveRange") || 
             other.gameObject.name.Contains("Sphere")||
            (other.transform.parent != null && other.transform.parent.name.Contains("ReviveRange")))
        {
            return;
        }

        // 2. 네트워크 유효성 검사
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning) return;

        // 3. 이미 자는 중인지 확인
        bool sleepingState = false;
        try { sleepingState = isPlayerSleeping; }
        catch (System.InvalidOperationException) { return; }

        if (sleepingState) return;

        // 4. 플레이어 컴포넌트 검사
        Player enteringPlayer = other.GetComponentInParent<Player>();
        if (enteringPlayer == null || enteringPlayer.Object == null || !enteringPlayer.Object.IsValid) return;

        // 5. 내 로컬 플레이어일 때만 수면 요청
        if (enteringPlayer.HasInputAuthority)
        {
            RPC_RequestSleep(enteringPlayer.Object.InputAuthority);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSleep(PlayerRef sleepingPlayerRef)
    {
        if (isPlayerSleeping) return;

        isPlayerSleeping = true;
        RPC_BroadcastSleepSequence(sleepingPlayerRef);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastSleepSequence(PlayerRef sleepingPlayerRef)
    {
        foreach (var player in FindObjectsOfType<Player>())
        {
            if (player.Object != null && player.Object.InputAuthority == sleepingPlayerRef)
            {
                StartCoroutine(SleepRoutine(player));
                break;
            }
        }
    }

    private IEnumerator SleepRoutine(Player sleepingPlayer)
    {
        bool isMyPlayer = sleepingPlayer.HasInputAuthority;

        if (isMyPlayer)
        {
            Debug.Log("[텐트] 수면 시퀀스 시작...");
            sleepingPlayer.SetInputEnabled(false);

            // 1. 암전
            if (FadeManager.Instance != null)
            {
                yield return FadeManager.Instance.FadeOut(fadeDuration);
            }

            // 2. 스탯 회복
            if (NetworkPlayerStats.LocalInstance != null)
            {
                NetworkPlayerStats.LocalInstance.RestoreStamina(10);
            }

            // 3. 자는 시간 대기
            yield return new WaitForSeconds(sleepDelay);

            // 4. 깨어날 위치로 플레이어 이동
            if (exitPoint != null)
            {
                sleepingPlayer.transform.position = exitPoint.position;
                sleepingPlayer.transform.rotation = exitPoint.rotation;
            }

            sleepingPlayer.SetInputEnabled(true);

            // 🌟 1회용이므로 텐트 네트워크 디스폰 요청
            RPC_RequestDespawn();

            // 5. 화면 밝아짐
            if (FadeManager.Instance != null)
            {
                yield return FadeManager.Instance.FadeIn(fadeDuration);
            }
        }
        else
        {
            SetPlayerVisibility(sleepingPlayer, false);

            yield return new WaitForSeconds(fadeDuration + sleepDelay);

            if (exitPoint != null)
            {
                sleepingPlayer.transform.position = exitPoint.position;
                sleepingPlayer.transform.rotation = exitPoint.rotation;
            }

            SetPlayerVisibility(sleepingPlayer, true);
        }
    }

    private void SetPlayerVisibility(Player player, bool isVisible)
    {
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.enabled = isVisible;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDespawn()
    {
        if (Runner != null && Runner.IsRunning && Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }
}