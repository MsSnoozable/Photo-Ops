using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMusicSelector : MonoBehaviour
{
    [SerializeField] AudioClip[] Music;
    [SerializeField] AudioSource AudioManager;

    // Start is called before the first frame update
    void Start()
    {
        //pick a random song
        AudioManager.clip = Music[Random.Range(0, Music.Length)];
        AudioManager.Play();
    }
}
