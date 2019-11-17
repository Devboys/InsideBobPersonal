using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FMOD_LevelMusic : MonoBehaviour
{
    public PlayerController playerController;
    
    [EventRef] 
    public string musicPath;
 
    private EventInstance levelMusic;

    private void Start()
    {
        levelMusic = RuntimeManager.CreateInstance(musicPath);
        levelMusic.start();
    }

    private void Update()
    {
        levelMusic.setParameterByName("ReverbStop", playerController.bulletTimePercentage);
    }

    private void OnDisable()
    { 
        levelMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
}
