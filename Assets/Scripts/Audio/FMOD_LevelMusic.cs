using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.Serialization;

public class FMOD_LevelMusic : MonoBehaviour
{
    [EventRef] 
    public string musicEventPath;
    
    [Range(0, 1)]
    public float musicVolume = 1;
 
    private EventInstance levelMusic;
    private PlayerController playerController;

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        
        levelMusic = RuntimeManager.CreateInstance(musicEventPath);
        levelMusic.start();
    }

    private void Update()
    {
        levelMusic.setParameterByName("ReverbStop", 1 - playerController.bulletTimePercentage);
        levelMusic.setParameterByName("MasterVol", musicVolume);

    }

    private void OnDisable()
    { 
        levelMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
}
