using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;

public class TitleScreenController : MonoBehaviour
{

    public KeyCode[] startKeys;
    
    [Space(20)]
    [EventRef]
    public string nextMess;

    // Update is called once per frame
    void Update()
    {
        foreach(KeyCode code in startKeys)
        {
            if (Input.GetKeyUp(code))
            {
                //gameObject.SetActive(false);
                SceneManager.LoadScene("Scenes/Main");
                
                RuntimeManager.PlayOneShot(nextMess, transform.position); // Play UI next message sound
            }
        }
    }
}
