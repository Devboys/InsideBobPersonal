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

    Timer indexSwitchTimer = new Timer();
    public float indexSwitchCooldown = 1;

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        levelController = FindObjectOfType<LevelController>();
        
        levelMusic = RuntimeManager.CreateInstance(musicEventPath);
        levelMusic.start();

        // Randomize FMOD MusicIndex parameter on player re-spawn
        playerController.OnRespawnEvent += () =>
        {
            if (indexSwitchTimer.IsFinished)
            {
                int musicIndex = UnityEngine.Random.Range(0, 9);
                levelMusic.setParameterByName("MusicIndex", musicIndex);

                indexSwitchTimer.StartTimer(indexSwitchCooldown);

                Debug.Log(musicIndex);
            }
        };
            
        // Randomize FMOD MusicIndex parameter on level change
        levelController.onLevelChangeEvent += () =>
        {
            if (indexSwitchTimer.IsFinished)
            {
                int musicIndex = UnityEngine.Random.Range(0, 9);
                levelMusic.setParameterByName("MusicIndex", musicIndex);

                indexSwitchTimer.StartTimer(indexSwitchCooldown);

                Debug.Log(musicIndex);
            }
        };
    }

    private void Update()
    {
        // Call FMOD ReverbStop 2 parameter when player enters bullet time
        levelMusic.setParameterByName("ReverbStop 2", 1 - playerController.bulletTimePercentage);
        levelMusic.setParameterByName("MasterVol", musicVolume);

        indexSwitchTimer.TickTimer(Time.deltaTime);

    }

    private void OnDisable()
    { 
        levelMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }


    private class Timer
    {
        private float initTime = 1; //initialized as 1 to prevent div by 0
        private float timer = 0;
        bool finishedLastCheck;

        public bool IsFinished
        {
            get { return timer <= 0; }
        }

        public float AsFraction()
        {
            if (timer < 0) return 0;

            return 1 - timer / initTime;
        }

        public bool HasJustFinished()
        {
            bool result = finishedLastCheck == IsFinished;
            finishedLastCheck = IsFinished;

            return result;
        }

        public void StartTimer(float startTime) { timer = this.initTime = startTime; }
        public void TickTimer(float amount) { timer -= amount; }
        public void EndTimer() { timer = 0; }
    }
}
