using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayEndScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Resources.FindObjectsOfTypeAll<EndSceneHandler>()[0].PlayEndScene();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
