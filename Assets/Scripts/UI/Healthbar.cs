using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    public Image slider;
    private PlayerController player;
    private Image sr;
    private void Awake()
    {
        player = FindObjectOfType<PlayerController>();
        sr = GetComponent<Image>();
    }

    private void Update()
    {
        bool isActive = player.health < player.maxHP;
        sr.enabled = isActive;
        slider.gameObject.SetActive(isActive);
        slider.fillAmount = player.health / player.maxHP;
    }
}
