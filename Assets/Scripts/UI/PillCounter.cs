using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class PillCounter : MonoBehaviour
{

    public GameObject pillImage;
    
    [Header("-- FMOD Event")]
    [Space(20)]
    [EventRef]
    public string pillPickupPath;
    private EventInstance pillPickup;

    private Text _text;
    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = FindObjectOfType<PlayerController>();
        _text = GetComponent<Text>();

        // FMOD
        pillPickup = RuntimeManager.CreateInstance(pillPickupPath);
        
        //subscribe to pickup event
        _playerController.OnPillPickup += () => {
            
            pillImage.GetComponent<Animator>().SetTrigger("pickup");
            if (!_playerController.playerHasDied)
            {
                pillPickup.start(); // Play pill pickup sound
            }
        };
    }
    

    private void Update()
    {
        _text.text = _playerController.totalPillsPickedUp.ToString();
    }
}
