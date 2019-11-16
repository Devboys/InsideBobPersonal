using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingCamera : MonoBehaviour
{
    private PlayerController pc;
    private Material mat;

    void Awake()
    {
        pc = FindObjectOfType<PlayerController>();
        mat = new Material(pc.shader);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (pc && pc.IsInBulletTime() > 0)
        {
            /*Material material = new Material(pc.shader)
            {
                mainTexture = GetComponent<Camera>().targetTexture
            };*/

            //draws the pixels from the source texture to the destination texture
            mat.SetFloat("_Percentage", pc.IsInBulletTime());
            Graphics.Blit(source, destination, mat);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
