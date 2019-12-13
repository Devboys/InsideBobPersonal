using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PillHandler : MonoBehaviour
{
    public float floatAmplitude;
    public float floatFrequency;

    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.position = new Vector3(startPos.x, startPos.y + Mathf.Sin(Time.timeSinceLevelLoad * floatFrequency) * floatAmplitude);
    }
}