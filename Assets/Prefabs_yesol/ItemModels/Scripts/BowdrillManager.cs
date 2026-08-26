using Fusion;
using UnityEngine;

public class BowdrillManager : NetworkBehaviour
{
    [SerializeField] private HandlePart handlePart;
    [SerializeField] private HandleEndPart handleEndPart;

    [Header("Settings")]
    [SerializeField] private int startBurn = 10;
    [SerializeField] private int completeBurn = 50;

    [Networked] public int count { get; set; }

    [Networked] private bool isStarted { get; set; }
    [Networked] private bool isCompleted { get; set; }

    private ChangeDetector _changeDetector;

    private void Awake()
    {
        /*
        if (handlePart != null)
        {
            handlePart.TriggerStartParticleEffect(false);
            handlePart.TriggerCompleteParticleEffect(false);
        }
        */
    }

    public override void Spawned()
    {
        // Fusion 객체가 네트워크에 생성될 때 ChangeDetector 등록
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    // 로직 처리 (서버/호스트에서 실행)
    public override void FixedUpdateNetwork()
    {
        // StateAuthority(서버/호스트)만 로직을 계산하도록 제한
        if (!HasStateAuthority) return;

        if (!isStarted && count > startBurn)
        {
            isStarted = true;
        }

        if (!isCompleted && count > completeBurn)
        {
            isCompleted = true;
        }
    }

    // 시각적 연출/네트워크 데이터 변화 감지 (모든 클라이언트에서 실행)
    public override void Render()
    {
        // [Networked] 변수 값이 변경되었는지 감지
        foreach (var changeName in _changeDetector.DetectChanges(this))
        {
            if (changeName == nameof(isStarted) && isStarted)
            {
                if (handlePart != null) handlePart.TriggerStartParticleEffect(true);
            }
            else if (changeName == nameof(isCompleted) && isCompleted)
            {
                if (handlePart != null) handlePart.TriggerCompleteParticleEffect(true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 서버/호스트에서만 트리거 카운트를 올리도록 제어 (중복 카운트 방지)
        if (!HasStateAuthority) return;

        if (!isCompleted && other.CompareTag("Bow"))
        {
            count++;
        }
    }
}
