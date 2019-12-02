using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkedMessage : MonoBehaviour
{
    [SerializeField]
    public LinkedMessage nextMessage;

    [HideInInspector]
    public List<LinkedMessage> prevMessages;

    // Start is called before the first frame update
    void Awake()
    {
        prevMessages = new List<LinkedMessage>();
    }

    private void Start()
    {
        if (nextMessage) nextMessage.prevMessages.Add(this);
        HideNextMessage();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.E)) {
            ShowNextMessage();
        }
    }

    public void ShowMessage() {
        gameObject.SetActive(true);
        HidePrevMessage();
        HideNextMessage();
    }

    void ShowNextMessage() {
        nextMessage.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    void HideNextMessage() {
        if (nextMessage)
        {
            nextMessage.gameObject.SetActive(false);
            nextMessage.HideNextMessage();
        }
    }

    void HidePrevMessage() {
        if (prevMessages.Count > 0)
        {
            foreach(LinkedMessage msg in prevMessages)
            {
                msg.gameObject.SetActive(false);
                msg.HidePrevMessage();
            }
        }
    }

}
