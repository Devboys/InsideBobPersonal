using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelController : MonoBehaviour
{
    public AnimationCurve screenTransition;
    public Vector2 levelSize;

    private PlayerController player;
    private Camera mainCam;

    [HideInInspector]
    public Vector2Int levelIndex = Vector2Int.zero;

    private void Awake()
    {
        player = FindObjectOfType<PlayerController>();
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (player)
        {
            if (player.transform.position.x > levelSize.x / 2 + levelIndex.x * levelSize.x)
                MoveToLevel(Vector2Int.right);
            else if (player.transform.position.x < -levelSize.x / 2 + levelIndex.x * levelSize.x)
                MoveToLevel(Vector2Int.left);
            else if (player.transform.position.y > levelSize.y / 2 + levelIndex.y * levelSize.y)
                MoveToLevel(Vector2Int.up);
            else if (player.transform.position.y < -levelSize.y / 2 + levelIndex.y * levelSize.y)
                MoveToLevel(Vector2Int.down);
        }
    }

    private void MoveToLevel(Vector2Int dir)
    {
        StopAllCoroutines();
        levelIndex += dir;
        StartCoroutine(TransitionCamera());
    }

    private IEnumerator TransitionCamera()
    {
        float curTime = 0;
        float endTime = screenTransition.keys[screenTransition.length - 1].time;
        Vector3 startPos = mainCam.transform.position;
        Vector3 endPos = new Vector3(levelIndex.x * levelSize.x, levelIndex.y * levelSize.y, -10);
        while (curTime < endTime)
        {
            mainCam.transform.position = Vector3.LerpUnclamped(startPos, endPos, screenTransition.Evaluate(curTime));
            yield return null;
            curTime += Time.deltaTime;
        }
        mainCam.transform.position = endPos;
    }
}
