using Fusion;
using UnityEngine;

public class PedestalPart : NetworkBehaviour
{
    // Inspector â���� ������ �ִ� Ư�� ParticleSystem�� ���� �巡���ؼ� �־��ݴϴ�.
    [Header("Child References")]
    [SerializeField] private ParticleSystem startParticleSystem;
    [SerializeField] private ParticleSystem completeParticleSystem;

    // �ܺ�(������ ��)���� ������ �� �ִ� ���� �޼���
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

        gameObject.tag = "Torch";
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