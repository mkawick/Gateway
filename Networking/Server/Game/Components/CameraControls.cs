using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DebugHelpers;

namespace Server.Game
{
    public class CameraControls : MonoBehaviour
    {
        // Use this for initialization
        IEnumerator Start()
        {
            yield return null;
            /*
            // Open/close console.
            if (CrossPlatformInputManager.GetButtonDown(InputMapping.GetInputString(InputMapping.Key.ToggleConsole)))
            {
                bool activate = !DebugUI.ConsoleActive;

                DebugUI.ActivateConsole(activate);
            }
            */

            DebugUI.SetCurrentGameCamera(Camera.main);
            DebugUI.EnableFlyCamera(true);
            Cursor.lockState = CursorLockMode.Locked;
        }
        
    }

}