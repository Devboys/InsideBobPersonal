using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class LinkedMessage : MonoBehaviour
{
    [SerializeField]
    public LinkedMessage nextMessage;
    [SerializeField]
    public KeyCode[] nextMessageKeys;
    [SerializeField]
    public string[] nextMessageAxis;

    public bool blockOtherInput;

    [HideInInspector]
    public List<LinkedMessage> prevMessages;
    
    [Header("-- FMOD Event")]
    [Space(20)]
    [EventRef]
    public string nextMessageSound;

    private PlayerController playerController;

    // Start is called before the first frame update
    void Awake()
    {
        prevMessages = new List<LinkedMessage>();
        playerController = FindObjectOfType<PlayerController>();
    }

    private void Start()
    {
        if (nextMessage) nextMessage.prevMessages.Add(this);
        HideNextMessage();
        LockInput();
    }

    // Update is called once per frame
    void Update()
    {
        foreach(KeyCode key in nextMessageKeys)
        {
            if (Input.GetKeyUp(key)) {
                ShowNextMessage();
            }
        }
        foreach (string axis in nextMessageAxis) {
            if (Input.GetAxisRaw(axis) != 0)
            {
                ShowNextMessage();
            }
        }
    }

    public void ShowMessage() {
        HidePrevMessage();
        HideNextMessage();
        gameObject.SetActive(true);
        LockInput();
    }

    void ShowNextMessage() {
        if (nextMessage) nextMessage.ShowMessage();
        HideMessage();
        
        RuntimeManager.PlayOneShot(nextMessageSound, transform.position); // Play button press confirmation sound
    }

    public void HideMessage() {
        if (gameObject.activeSelf)
        {
            UnlockInput();
            gameObject.SetActive(false);
        }
    }

    void HideNextMessage() {
        if (nextMessage)
        {
            nextMessage.HideMessage();
            nextMessage.HideNextMessage();
        }
    }

    void HidePrevMessage() {
        if (prevMessages.Count > 0)
        {
            foreach(LinkedMessage msg in prevMessages)
            {
                msg.HideMessage();
                msg.HidePrevMessage();
            }
        }
    }

    void LockInput() {
        if (blockOtherInput)
        {
            playerController.canMove = false;
        }
    }

    void UnlockInput() {
        if (!playerController.canMove) playerController.canMove = true;
    }

}
