using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingCamera : MonoBehaviour
{
    private PlayerController pc;

    void Awake()
    {
        pc = FindObjectOfType<PlayerController>();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (pc.IsInBulletTime())
        {
            Material material = new Material(pc.shader)
            {
                mainTexture = GetComponent<Camera>().targetTexture
            };

            //draws the pixels from the source texture to the destination texture
            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
