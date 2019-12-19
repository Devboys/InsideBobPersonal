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
    private LevelController levelController;

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        levelController = FindObjectOfType<LevelController>();
        
        levelMusic = RuntimeManager.CreateInstance(musicEventPath);
        levelMusic.start();

        // Randomize FMOD MusicIndex parameter on player re-spawn
        playerController.OnRespawnEvent += () =>
        {
            int musicIndex = UnityEngine.Random.Range(0, 9);
            levelMusic.setParameterByName("MusicIndex", musicIndex);
            
            Debug.Log(musicIndex); 
        };
            
        // Randomize FMOD MusicIndex parameter on level change
        levelController.onLevelChangeEvent += () =>
        {
            int musicIndex = UnityEngine.Random.Range(0, 9);
            levelMusic.setParameterByName("MusicIndex", musicIndex);
            
            Debug.Log(musicIndex); 
        };
    }

    private void Update()
    {
        // Call FMOD ReverbStop 2 parameter when player enters bullet time
        levelMusic.setParameterByName("ReverbStop 2", 1 - playerController.bulletTimePercentage);
        levelMusic.setParameterByName("MasterVol", musicVolume);

    }

    private void OnDisable()
    { 
        levelMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
}
