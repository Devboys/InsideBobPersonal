using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BobpolosionController : MonoBehaviour
{
    private ParticleSystem ps;

    public bool playParticleImmediatly = true;
    public bool isOneshot = true;

    private bool toBeDestroyed = false;

    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        if (playParticleImmediatly)
        {
            ps.Play();
            toBeDestroyed = isOneshot;
        }
    }

    public void PlayEffect()
    {
        if(ps != null)
        {
            ps.Play();
            toBeDestroyed = isOneshot;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (ps.isStopped && toBeDestroyed) Destroy(gameObject);
    }
}
