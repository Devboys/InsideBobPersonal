using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageSystem : MonoBehaviour
{

    private Timer showTimer;

    private Text textMesh;

    void Awake()
    {
        textMesh = GetComponentInChildren<Text>();
        showTimer = new Timer();
    }

    // Update is called once per frame
    void Update()
    {
        TickTimers();
        if (showTimer.IsFinished)
        {
            Hide();
        }
    }

    private void TickTimers()
    {
        showTimer.TickTimer(Time.deltaTime);
    }

    public void SetText(string text, float fadeTime) {
        textMesh.text = text;
        showTimer.StartTimer(fadeTime);
        Show();
    }

    public void Show() {
        //gameObject.SetActive(true);
    }

    public void Hide()
    {
        //gameObject.SetActive(false);
    }


    private class Timer
    {
        private float initTime = 1; //initialized as 1 to prevent div by 0
        private float timer = 0;
        bool finishedLastCheck;

        public bool IsFinished
        {
            get { return timer <= 0; }
        }

        public float AsFraction()
        {
            if (timer < 0) return 0;

            return 1 - timer / initTime;
        }

        public bool HasJustFinished()
        {
            bool result = finishedLastCheck == IsFinished;
            finishedLastCheck = IsFinished;

            return result;
        }

        public void StartTimer(float startTime) { timer = this.initTime = startTime; }
        public void TickTimer(float amount) { timer -= amount; }
        public void EndTimer() { timer = 0; }
    }

}
