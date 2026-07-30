using Fusion;
using UnityEngine;

public class HandlePart : NetworkBehaviour
{
    [Networked] private NetworkId pedestalPartId { get; set; }

    // BasePart 스크립트 참조 가져오기
    private PedestalPart TargetPedestalPart
    {
        get
        {
            if (pedestalPartId.IsValid && Runner.TryFindObject(pedestalPartId, out var netObj))
            {
                return netObj.GetComponent<PedestalPart>();
            }
            return null;
        }
    }

    public void InitPedestalReference(NetworkObject pedestalNetObj)
    {
        if (HasStateAuthority)
        {
            pedestalPartId = pedestalNetObj.Id;
            TriggerStartParticleEffect(false);
            TriggerCompleteParticleEffect(false);
        }
    }

    // 받침대 하위 파티클 제어 호출
    public void TriggerStartParticleEffect(bool play)
    {
        var basePart = TargetPedestalPart;
        if (basePart != null)
        {
            if (play) basePart.PlayStartParticle();
            else basePart.StopStartParticle();
        }
    }

    public void TriggerCompleteParticleEffect(bool play)
    {
        var basePart = TargetPedestalPart;
        if (basePart != null)
        {
            if (play) basePart.PlayCompleteParticle();
            else basePart.StopCompleteParticle();
        }
    }
}