using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using TMPro;

public class ButtonsManager : MonoBehaviour
{
    /**
    * This class manages the UI interface of the applications.
    * It should handle the button states and trigger the appropriate actions
    * in the ApplicationLogic class when the buttons are pressed.
    *
    * By itself this MonoBehaviour contains the whole application. Meaning you can import 
    * an instance of it in an empty scene, set the button references in the inspector, and it should work.
    */
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;
    [SerializeField] private Button button4;

    private ApplicationLogic appLogic = new ApplicationLogic();

    public void Start()
    {
        if (button1 == null || button2 == null || button3 == null || button4 == null )
        {
            Debug.LogError("One or more button references are missing. Please set them in the inspector.");
            return;
        }

        DisableButton(button1);
        DisableButton(button2);
        DisableButton(button3);
        DisableButton(button4);

        /** 
         * The _ is there to indicate that these functions are run asynchronously
         * but are never awaited.
         */
        button1.onClick.AddListener(() => _ = OnButtonPressed(1));
        button2.onClick.AddListener(() => _ = OnButtonPressed(2));
        button3.onClick.AddListener(() => _ = OnButtonPressed(3));
        button4.onClick.AddListener(() => _ = OnButtonPressed(4));

        /**
         * In the flow of the application, the first thing to be done is to establish
         * the connection with the server, so we only enable the first button at the start.
         */
        EnableButton(button1);
    }

    private async Task OnButtonPressed(int buttonNumber)
    {
        Debug.Log($"Button {buttonNumber} pressed!");
        
        switch(buttonNumber)
        {
            case 1:
                await HandleButton1();
                break;
            case 2:
                await HandleButton2();
                break;
            case 3:
                await HandleButton3();
                break;
            case 4:
                await HandleButton4();
                break;
        }
    }

    private async Task HandleButton1() 
    {
        DisableButton(button1);

        try
        {
            await appLogic.BuildConnections();

            SetButton(button1, "Connected", Color.green);
            EnableButton(button2);
            // EnableButton(button3); // Not implemented yet
            EnableButton(button4);

            Debug.Log("Connections established successfully.");
        }
        catch (Exception ex)
        {
            SetButton(button1, "Try Again", Color.red);
            EnableButton(button1);

            Debug.LogError($"Failed to connect: {ex.Message}");
        }
    }

    private async Task HandleButton2()
    {
        DisableButton(button2);

        if (!appLogic.IsCameraStreaming)
        {
            try {
                await appLogic.BeginCameraStream();

                SetButton(button2, "Stop Stream", Color.red);
                Debug.Log("Camera stream started successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start stream: {ex.Message}");
            }
        }
        else
        {
            try {
                await appLogic.EndCameraStream();

                SetButton(button2, "Start Stream", Color.white);
                Debug.Log("Camera stream stopped successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to stop stream: {ex.Message}");
            }
        }

        EnableButton(button2);
    }

    private async Task HandleButton3()
    {
        DisableButton(button3);

        if (!appLogic.IsPoseStreaming)
        {
            try {
                await appLogic.BeginPoseStream();

                SetButton(button3, "Stop Stream", Color.red);
                Debug.Log("Pose stream started successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start stream: {ex.Message}");
            }
        }
        else
        {
            try {
                await appLogic.EndPoseStream();

                SetButton(button3, "Start Stream", Color.white);
                Debug.Log("Pose stream stopped successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to stop stream: {ex.Message}");
            }
        }

        EnableButton(button3);
    }

    private async Task HandleButton4()
    {
        DisableButton(button4);

        try {
            await appLogic.CleanUp();

            SetButton(button1, "Connect", Color.white);
            SetButton(button2, "Stream Cameras", Color.white);
            SetButton(button3, "Stream Pose", Color.white);
            SetButton(button4, "Disconnect", Color.white);

            EnableButton(button1);
            DisableButton(button2);
            DisableButton(button3);
            DisableButton(button4);

            Debug.Log("Clean up completed successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to clean up: {ex.Message}");

            EnableButton(button4);
        }       
    }


    // Utility functions used to help with button handling
    private void SetButton(Button button, string text, Color color)
    {
        button.GetComponentInChildren<TMP_Text>().text = text;
        button.image.color = color;
    }

    private void DisableButton(Button button)
    {
        var transparencyLevel = 0.25f;
        button.interactable = false;
        button.image.color = new Color(button.image.color.r, button.image.color.g, button.image.color.b, transparencyLevel);
    }

    private void EnableButton(Button button)
    {
        var transparencyLevel = 1f;
        button.interactable = true;
        button.image.color = new Color(button.image.color.r, button.image.color.g, button.image.color.b, transparencyLevel);
    }
}
