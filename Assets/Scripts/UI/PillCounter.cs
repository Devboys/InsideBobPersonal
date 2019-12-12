using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class PillCounter : MonoBehaviour
{

    public GameObject pillImage;

    private Text _text;
    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = FindObjectOfType<PlayerController>();
        _text = GetComponent<Text>();

        //subscribe to pickup event
        _playerController.OnPillPickup += () => {
            
            pillImage.GetComponent<Animator>().SetTrigger("pickup"); 
            
            //TODO: SOUND - Play pickup sound.

        };
    }

    private void Update()
    {
        _text.text = _playerController.totalPillsPickedUp.ToString();
    }
}
