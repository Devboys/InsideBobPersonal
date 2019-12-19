using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndSceneHandler : MonoBehaviour
{

    Animator animator;

    // Start is called before the first frame update
    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        gameObject.SetActive(false);
    }

    private void Start()
    {
        PlayEndScene();
    }

    public void PlayEndScene()
    {
        GameObject.FindGameObjectWithTag("UI").SetActive(false);
        gameObject.SetActive(true);
        animator.SetTrigger("Play");
    }

    void GoToTitle()
    {
        SceneManager.LoadScene("Scenes/TitleScene");
    }

}
