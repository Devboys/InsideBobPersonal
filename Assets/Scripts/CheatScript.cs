using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheatScript : MonoBehaviour
{
    private PlayerController player;
    private ControllerInput controllerInput;

    private void Awake()
    {
        player = FindObjectOfType<PlayerController>();
        controllerInput = player.GetComponent<ControllerInput>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            controllerInput.enabled = false;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            controllerInput.enabled = true;
            controllerInput.useBulletTimeButton = true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            controllerInput.enabled = true;
            controllerInput.useBulletTimeButton = false;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
