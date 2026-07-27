using Fusion;
using UnityEngine;

public class PedestalPart : NetworkBehaviour
{
    // Inspector 창에서 하위에 있는 특정 ParticleSystem을 직접 드래그해서 넣어줍니다.
    [Header("Child References")]
    [SerializeField] private ParticleSystem startParticleSystem;
    [SerializeField] private ParticleSystem completeParticleSystem;

    // 외부(손잡이 등)에서 접근할 수 있는 제어 메서드
    public void PlayStartParticle()
    {
        if (startParticleSystem != null)
        {
            startParticleSystem.Play();
        }
    }
    public void PlayCompleteParticle()
    {
        if (completeParticleSystem != null)
        {
            completeParticleSystem.Play();
        }
    }

    public void StopStartParticle()
    {
        if (startParticleSystem != null)
        {
            startParticleSystem.Stop();
        }
    }
    public void StopCompleteParticle()
    {
        if (completeParticleSystem != null)
        {
            completeParticleSystem.Stop();
        }
    }
}