using UnityEngine;
using System.Collections;
using Fusion;

[RequireComponent(typeof(Collider))]
public class NetworkTent : NetworkBehaviour
{
    [Networked] private NetworkBool isPlayerSleeping { get; set; }

    [Header("페이드 연출 시간 설정")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float sleepDelay = 5.0f;

    [Header("퇴장 위치")]
    [SerializeField] private Transform exitPoint;

    private void Awake()
    {
        // 1. 트리거 콜라이더 보장
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // 2. 🌟 클라이언트/호스트 모두에서 충돌을 감지하도록 Kinematic Rigidbody 필수 부착
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. ReviveRange 무시
        string otherName = other.gameObject.name.ToLower();
        string parentName = other.transform.parent != null ? other.transform.parent.name.ToLower() : "";
        if (otherName.Contains("reviverange") || otherName.Contains("sphere") || parentName.Contains("reviverange"))
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

        // 4. 플레이어 탐색 (XR Origin 카메라 리그 또는 아바타 바디)
        Player enteringPlayer = other.GetComponentInParent<Player>();
        if (enteringPlayer == null && NetworkPlayerStats.LocalInstance != null)
        {
            // XR Origin 하위의 로컬 카메라/컨트롤러가 닿았을 경우 대비
            enteringPlayer = NetworkPlayerStats.LocalInstance.GetComponent<Player>();
        }

        if (enteringPlayer == null || enteringPlayer.Object == null || !enteringPlayer.Object.IsValid) return;

        // 5. 🌟 클라이언트(내 캐릭터) 또는 서버(호스트)에서 감지 시 수면 요청
        if (enteringPlayer.HasInputAuthority || Runner.IsServer)
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
        Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player != null && player.Object != null && player.Object.InputAuthority == sleepingPlayerRef)
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
            Debug.Log("⛺ [텐트] 로컬 플레이어 수면 시작");
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

            // 텐트 네트워크 디스폰 요청
            RPC_RequestDespawn();

            // 5. 화면 밝아짐
            if (FadeManager.Instance != null)
            {
                yield return FadeManager.Instance.FadeIn(fadeDuration);
            }
        }
        else
        {
            // 다른 플레이어 화면에서 숨기기
            SetPlayerVisibility(sleepingPlayer, false);

            yield return new WaitForSeconds(fadeDuration + sleepDelay);

            if (exitPoint != null)
            {
                sleepingPlayer.transform.position = exitPoint.position;
                sleepingPlayer.transform.rotation = exitPoint.rotation;
            }

            // 다른 플레이어 화면에서 정상 복구
            SetPlayerVisibility(sleepingPlayer, true);
        }
    }

    private void SetPlayerVisibility(Player player, bool isVisible)
    {
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            string objName = rend.gameObject.name.ToLower();
            string parentName = rend.transform.parent != null ? rend.transform.parent.name.ToLower() : "";

            // ReviveRange 계열 렌더러 무조건 끔
            if (objName.Contains("reviverange") || objName.Contains("revive") || parentName.Contains("reviverange"))
            {
                rend.enabled = false;
                continue;
            }

            // Body(Blueberry) 하위 Hair 렌더러 무조건 끔
            if (objName == "hair" && (parentName.Contains("blueberry") || parentName.Contains("body")))
            {
                rend.enabled = false;
                continue;
            }

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