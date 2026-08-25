using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class HelicopterController : NetworkBehaviour
{
    // 헬기 상태 정의
    public enum EHelicopterState
    {
        Inactive,      // 스폰 전 / 대기
        Approaching,   // 목표 지점으로 비행 중
        Hovering,      // 정지 상태 (사다리 내려옴, 플레이어 탑승 시간)
        Departing      // 탈출 상승 이탈
    }

    [Header("Networked States")]
    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public EHelicopterState CurrentState { get; set; } = EHelicopterState.Inactive;

    [Networked]
    public TickTimer EscapeTimer { get; set; }

    [Header("Flight Settings")]
    [SerializeField] private Transform startWaypoint;
    [SerializeField] private Transform hoverWaypoint;
    [SerializeField] private Transform escapeWaypoint;
    [SerializeField] private float flySpeed = 15f;
    [SerializeField] private float rotationSpeed = 3f;

    [Header("Ladder & Escape Settings")]
    [SerializeField] private GameObject helicopterObject;
    [SerializeField] private GameObject ropeLadderObject; // 사다리 프리팹/오브젝트
    [SerializeField] private float hoverDuration = 45f;    // 탑승 대기 시간 (초)

    [Header("Components")]
    [SerializeField] private AudioSource engineAudioSource;

    // 탈출 성공한 플레이어 목록 (State Authority에서 관리)
    private readonly HashSet<PlayerRef> _escapedPlayers = new HashSet<PlayerRef>();

    [Header("Ladder Rungs")]
    [SerializeField] private Transform[] ladderRungs; // 사다리 발판들, 인스펙터에서 순서대로 등록

    public Transform GetRungByIndex(int index)
    {
        if (ladderRungs == null || index < 0 || index >= ladderRungs.Length) return null;
        return ladderRungs[index];
    }

    public int GetRungIndex(Transform rung)
    {
        if (ladderRungs == null) return -1;
        for (int i = 0; i < ladderRungs.Length; i++)
        {
            if (ladderRungs[i] == rung) return i;
        }
        return -1;
    }

    public override void Spawned()
    {
        if(helicopterObject != null)
        {
            helicopterObject.SetActive(false);
        }

        if (ropeLadderObject != null)
        {
            ropeLadderObject.SetActive(false);
        }
    }

    // 헬기 호출 (서버/Host 권한으로 실행)
    public void CallHelicopter()
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentState != EHelicopterState.Inactive) return;

        transform.position = startWaypoint.position;
        transform.rotation = startWaypoint.rotation;
        CurrentState = EHelicopterState.Approaching;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        switch (CurrentState)
        {
            case EHelicopterState.Approaching:
                // 🌟 SetActive 제거 (OnStateChanged로 이동)
                MoveTowardsWaypoint(hoverWaypoint.position, () =>
                {
                    CurrentState = EHelicopterState.Hovering;
                    EscapeTimer = TickTimer.CreateFromSeconds(Runner, hoverDuration);
                });
                break;

            case EHelicopterState.Hovering:
                if (EscapeTimer.Expired(Runner))
                {
                    CurrentState = EHelicopterState.Departing;
                }
                break;

            case EHelicopterState.Departing:
                MoveTowardsWaypoint(escapeWaypoint.position, () =>
                {
                    OnHelicopterEscaped();
                    CurrentState = EHelicopterState.Inactive;
                });
                break;
        }
    }

    // 상태 변경 시 모든 클라이언트에서 실행 (Fusion 2 OnChangedRender)
    private void OnStateChanged()
    {
        switch (CurrentState)
        {
            case EHelicopterState.Approaching:
                if (helicopterObject != null)
                    helicopterObject.SetActive(true); // 🌟 이제 모든 클라이언트에서 실행됨

                if (engineAudioSource != null && !engineAudioSource.isPlaying)
                    engineAudioSource.Play();

                if (Object.HasStateAuthority)
                {
                    NotifyAllPlayersApproaching();
                }
                break;

            case EHelicopterState.Hovering:
                if (ropeLadderObject != null)
                    ropeLadderObject.SetActive(true);
                break;

            case EHelicopterState.Departing:
                // 탈출 연출 시작
                break;

            case EHelicopterState.Inactive:
                // 🌟 다음 라운드를 위해 여기서 끄는 것도 고려
                if (helicopterObject != null)
                    helicopterObject.SetActive(false);
                if (ropeLadderObject != null)
                    ropeLadderObject.SetActive(false);
                break;
        }
    }

    // 비행 이동 처리 메서드 (회전 없이 직선 이동만 수행)
    private void MoveTowardsWaypoint(Vector3 targetPosition, System.Action onArrived)
    {
        // 회전 계산(Quaternion.LookRotation 및 Slerp)을 모두 제거하고 Position만 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, flySpeed * Runner.DeltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            onArrived?.Invoke();
        }
    }

    // 플레이어가 사다리를 잡았을 때 호출하는 RPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RegisterEscapedPlayer(PlayerRef player, bool isBoarded)
    {
        if (isBoarded)
        {
            _escapedPlayers.Add(player);
            Debug.Log($"Player {player} registered for escape!");
        }
        else
        {
            _escapedPlayers.Remove(player);
        }
    }

    private void NotifyAllPlayersApproaching()
    {
        foreach (var playerRef in Runner.ActivePlayers)
        {
            var playerObject = Runner.GetPlayerObject(playerRef);
            if (playerObject != null)
            {
                var player = playerObject.GetComponent<Player>();
                if (player != null)
                {
                    player.NotifyHelicopterApproaching();
                }
            }
        }
    }

    [SerializeField] private NetworkGameOverManager gameOverManager; // 인스펙터에서 연결

    private void OnHelicopterEscaped()
    {
        Debug.Log($"Helicopter escaped with {_escapedPlayers.Count} players!");

        foreach (var playerRef in Runner.ActivePlayers)
        {
            var playerObject = Runner.GetPlayerObject(playerRef);
            if (playerObject == null) continue;

            var player = playerObject.GetComponent<Player>();
            if (player == null) continue;

            if (_escapedPlayers.Contains(playerRef))
            {
                player.IsRescued = true;
            }
            else
            {
                player.IsEscapeFailed = true;
            }
        }

        // 🌟 헬기가 최종적으로 떠났으니, 결과와 무관하게 게임 세션을 종료
        //    (이후 "전원 기절" 체크가 중복으로 게임오버를 띄우지 않도록)
        if (Object.HasStateAuthority && gameOverManager != null)
        {
            gameOverManager.EndGameSession();
        }
    }
}