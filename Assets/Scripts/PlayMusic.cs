using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayMusic : MonoBehaviour
{
    [SerializeField] AudioSource musicAudio;

    void OnEnable()
    {
        musicAudio.Play();
    }

    void OnDisable()
    {
        musicAudio.Stop();
    }
}
