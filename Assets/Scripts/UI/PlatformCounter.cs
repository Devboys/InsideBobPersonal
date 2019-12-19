using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;


public class PlatformCounter : MonoBehaviour
{
    private Text _text;
    
    private PlayerController _playerController;

    public GameObject padImage;
    
    [Header("-- FMOD Event")]
    [Space(20)]
    [EventRef]
    public string padPickupPath;
    private EventInstance padPickup;

    private void Awake()
    {
        _playerController = FindObjectOfType<PlayerController>();
        _text = GetComponent<Text>();
        
        // FMOD
        padPickup = RuntimeManager.CreateInstance(padPickupPath);

        //subscribe to pickup event
        _playerController.OnPadPickup += () => {

            padImage.GetComponent<Animator>().SetTrigger("pickup");

            padPickup.start(); // Play band-aid pickup sound

        };
    }

    private void Update()
    {
        _text.text = _playerController.numPadsAllowed.ToString();
    }
}
