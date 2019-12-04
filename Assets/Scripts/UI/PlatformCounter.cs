using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class PlatformCounter : MonoBehaviour
{
    private Text _text;
    
    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = FindObjectOfType<PlayerController>();
        _text = GetComponent<Text>();
    }

    private void Update()
    {
        _text.text = _playerController.numPadsAllowed.ToString();
    }
}
