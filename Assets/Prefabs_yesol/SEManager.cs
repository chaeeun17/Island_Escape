using UnityEngine;
using System.Collections.Generic;

public class SEManager : MonoBehaviour
{
    private static SEManager instance;

    [Header("Audio Clip")]
    [SerializeField] private AudioClip SE_craft;
    [SerializeField] private AudioClip SE_waterDrop;
    [SerializeField] private AudioClip SE_waterSplash;
    [SerializeField] private AudioClip SE_place;

    [SerializeField] private List<AudioClip> SE_fish;

    private AudioSource audioSource;
    private float volume = 1f;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        audioSource = GetComponent<AudioSource>();
        volume = audioSource.volume;
    }

    public static SEManager Instance
    {
        get
        {
            return instance;
        }
    }

    public void PlayCraft()
    {
        audioSource.PlayOneShot(SE_craft);
    }
    public void PlayWaterDrop()
    {
        audioSource.volume = volume * 0.7f;
        audioSource.PlayOneShot(SE_waterDrop);
    }
    public void PlayWaterSplash()
    {
        audioSource.volume = volume;
        audioSource.PlayOneShot(SE_waterSplash);
    }
    public void PlayPlace()
    {
        audioSource.volume = volume;
        audioSource.PlayOneShot(SE_place);
    }

    public void PlayFish()
    {
        audioSource.volume = volume * 0.7f;
        AudioClip fish = SE_fish[Random.Range(0, SE_fish.Count)];
        audioSource.PlayOneShot(fish);
    }
}
