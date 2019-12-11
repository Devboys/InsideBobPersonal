using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenController : MonoBehaviour
{

    public KeyCode[] startKeys;

    // Update is called once per frame
    void Update()
    {
        foreach(KeyCode code in startKeys)
        {
            if (Input.GetKeyUp(code))
            {
                //gameObject.SetActive(false);
                SceneManager.LoadScene("Scenes/Main");
            }
        }
    }
}
