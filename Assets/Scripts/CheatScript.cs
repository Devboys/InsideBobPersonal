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
            SceneManager.LoadScene(0);
        }
    }
}
