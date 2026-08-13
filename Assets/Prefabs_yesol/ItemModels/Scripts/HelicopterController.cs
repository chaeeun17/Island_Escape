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
    [SerializeField] private GameObject ropeLadderObject; // 사다리 프리팹/오브젝트
    [SerializeField] private float hoverDuration = 45f;    // 탑승 대기 시간 (초)

    [Header("Components")]
    [SerializeField] private AudioSource engineAudioSource;

    // 탈출 성공한 플레이어 목록 (State Authority에서 관리)
    private readonly HashSet<PlayerRef> _escapedPlayers = new HashSet<PlayerRef>();

    public override void Spawned()
    {
        // 사다리는 초기 상태에서 비활성화
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
        // 상태 업데이트 로직은 State Authority(Host)에서만 수행
        if (!Object.HasStateAuthority) return;

        switch (CurrentState)
        {
            case EHelicopterState.Approaching:
                MoveTowardsWaypoint(hoverWaypoint.position, () =>
                {
                    // 목표 지점 도착 시 호버링 상태로 전환 및 타이머 시작
                    CurrentState = EHelicopterState.Hovering;
                    EscapeTimer = TickTimer.CreateFromSeconds(Runner, hoverDuration);
                });
                break;

            case EHelicopterState.Hovering:
                // 남은 시간이 끝나면 이탈 상태로 전환
                if (EscapeTimer.Expired(Runner))
                {
                    CurrentState = EHelicopterState.Departing;
                }
                break;

            case EHelicopterState.Departing:
                MoveTowardsWaypoint(escapeWaypoint.position, () =>
                {
                    // 탈출 완료 후 세션 마무리 처리
                    OnHelicopterEscaped();
                    CurrentState = EHelicopterState.Inactive;
                });
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

    // 상태 변경 시 모든 클라이언트에서 실행 (Fusion 2 OnChangedRender)
    private void OnStateChanged()
    {
        switch (CurrentState)
        {
            case EHelicopterState.Approaching:
                if (engineAudioSource != null && !engineAudioSource.isPlaying)
                    engineAudioSource.Play();
                break;

            case EHelicopterState.Hovering:
                if (ropeLadderObject != null)
                    ropeLadderObject.SetActive(true); // 사다리 투하 연출/활성화
                break;

            case EHelicopterState.Departing:
                // 탈출 연출 시작 (예: VR 카메라 Fade Out 준비)
                break;
        }
    }

    private void OnHelicopterEscaped()
    {
        Debug.Log($"Helicopter escaped with {_escapedPlayers.Count} players!");
        // TODO: UI Fade Out 처리 및 승리 결과창 출력 후 세션 종료 로직 구현
    }
}