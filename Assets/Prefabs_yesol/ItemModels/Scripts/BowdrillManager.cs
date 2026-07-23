using UnityEngine;

public class BowdrillManager : MonoBehaviour
{
    public ParticleSystem startFire;
    public ParticleSystem completeFire;
    public int count = 0;

    [SerializeField] private int startBurn = 10;
    [SerializeField] private int completeBurn = 50;
    private bool isStarted = false;
    private bool isCompleted = false;

    private void Awake()
    {
        startFire.Pause();
        completeFire.Pause();
    }
    private void Update()
    {
        if(!isStarted && count > startBurn)
        {
            startFire.Play();
            isStarted = true;
        }
        else if(!isCompleted && count > completeBurn)
        {
            completeFire.Play();
            isCompleted = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isCompleted && other.CompareTag("Bow"))
        {
            count++;
        }
    }
}
