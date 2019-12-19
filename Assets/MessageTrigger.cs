using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MessageTrigger : MonoBehaviour
{
    [SerializeField]
    public LinkedMessage linkedMessage;
    public bool reuseable;
    
    [Header("-- FMOD Event")]
    [Space(20)]
    [EventRef]
    public string popUpMessPath;
    private EventInstance popUpMess;

    private bool used;

    private void Awake()
    {
        used = false;
    }

    private void Start()
    {
        // FMOD
        popUpMess = RuntimeManager.CreateInstance(popUpMessPath);
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!reuseable && used) return;
        if (other.gameObject.CompareTag("Player")) 
        {
            linkedMessage.ShowMessage();
            used = true;
            
            popUpMess.start(); // Play pop up message sound
        }    
    }
}
