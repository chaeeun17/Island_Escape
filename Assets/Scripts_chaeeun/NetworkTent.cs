using UnityEngine;
using System.Collections;
using Fusion;

public class NetworkTent : NetworkBehaviour, IInteractable
{
    // 현재 텐트가 사용 중인지 네트워크 상에 동기화
    [Networked] private NetworkBool isPlayerSleeping { get; set; }

    [Header("페이드 연출 시간 설정")]
    [SerializeField] private float fadeDuration = 1.0f; // 화면이 닫히고 열리는 시간(초)
    [SerializeField] private float sleepDelay = 5.0f;    // 암전된 상태로 유지될 시간(자는 시간)

    [Header("퇴장 위치")]
    [SerializeField] private Transform exitPoint; // 잠에서 깨어난 후 플레이어가 서 있을 위치

    public string GetInteractText()
    {
        if (isPlayerSleeping)
        {
            return "누군가 사용하는 중...";
        }
        else
        {
            return "잠자기";
        }
    }

    public void Interact()
    {
        if (isPlayerSleeping) return;

        if (NetworkPlayerStats.LocalInstance != null)
        {
            Player localPlayer = NetworkPlayerStats.LocalInstance.GetComponent<Player>();
            if (localPlayer != null)
            {
                RPC_RequestSleep(localPlayer.Object.InputAuthority);
            }
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
            if (player.Object.InputAuthority == sleepingPlayerRef)
            {
                StartCoroutine(SleepRoutine(player));
                break;
            }
        }
    }

    private IEnumerator SleepRoutine(Player sleepingPlayer)
    {
        bool isMyPlayer = sleepingPlayer.HasInputAuthority; // "나 자신"인가?

        if (isMyPlayer)
        {
            // ==========================================
            // 👁️ [내 시점] 나에게는 내 캐릭터를 끌 필요가 전혀 없음!
            // ==========================================
            Debug.Log("[텐트] 수면 시퀀스 시작...");

            // 🛑 [입력 차단] 페이드 아웃 시작 직전 이동 및 잡기 정지
            sleepingPlayer.SetInputEnabled(false);

            // 1. 화면 암전 (Fade Out)
            if (FadeManager.Instance != null)
            {
                yield return FadeManager.Instance.FadeOut(fadeDuration);
            }

            // 2. 스탯 정산
            if (NetworkPlayerStats.LocalInstance != null)
            {
                NetworkPlayerStats.LocalInstance.RestoreStamina(10);
            }

            // 3. 수면 시간 대기 (어두운 화면 유지)
            yield return new WaitForSeconds(sleepDelay);

            // 4. 깨어날 위치로 내 위치 이동
            if (exitPoint != null)
            {
                sleepingPlayer.transform.position = exitPoint.position;
                sleepingPlayer.transform.rotation = exitPoint.rotation;
            }

            sleepingPlayer.SetInputEnabled(true);
        
            RPC_RequestDespawn();

            // 5. 화면 다시 밝아지기 (Fade In)
            if (FadeManager.Instance != null)
            {
                yield return FadeManager.Instance.FadeIn(fadeDuration);
            }

            
        }
        else
        {
            // ==========================================
            // 👥 [상대방 시점] 상대방 화면에서만 내 아바타 숨기기!
            // ==========================================
            Debug.Log($"[텐트] {sleepingPlayer.name} 님이 텐트 속으로 들어갔습니다.");

            // 1. 상대방 화면에서만 잠자는 플레이어 아바타(메쉬) 숨김
            SetPlayerVisibility(sleepingPlayer, false);

            // 2. 수면 + 페이드 시간 동안 대기
            yield return new WaitForSeconds(fadeDuration + sleepDelay);

            // 3. 퇴장 위치로 플레이어 모델링 이동
            if (exitPoint != null)
            {
                sleepingPlayer.transform.position = exitPoint.position;
                sleepingPlayer.transform.rotation = exitPoint.rotation;
            }

            // 4. 상대방 화면에 내 아바타 다시 뿅! 등장
            SetPlayerVisibility(sleepingPlayer, true);
            Debug.Log($"[텐트] {sleepingPlayer.name} 님이 텐트에서 나왔습니다.");
        }
    }

    // 🌟 [핵심 지원 함수] GameObject는 둔 채, 메쉬(Renderer)만 켜고 끄는 함수
    private void SetPlayerVisibility(Player player, bool isVisible)
    {
        // 플레이어 캐릭터 자식들에 붙은 모든 MeshRenderer(몸통, 손, 얼굴 등)를 켜거나 끕니다.
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            // VR 카메라는 끄면 안 되므로, 혹시 카메라나 UI 가이드가 포함되어 있지 않은 렌더러만 토글합니다.
            rend.enabled = isVisible;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDespawn()
    {
        if (Runner != null && Runner.IsRunning)
        {
            Runner.Despawn(Object);
        }
    }
}