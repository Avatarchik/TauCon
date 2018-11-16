using UnityEngine;
using UnityEngine.UI;

namespace TauConsole
{

    [AddComponentMenu("Scripts/TauCon/TauConToggle")]
    /// <summary>
    /// This script must be attached to a separate (always active) GameObject
    /// </summary>
    public class TauConToggle : MonoBehaviour
    {

        [Header("Toggle Button")]
        // Set a 'Console' Axes in Project Settings > Input
        public string toggleCommand = string.Empty;

        [Header("UI Objects")]
        public GameObject tauConCanvas;
        public InputField inputField;

        private GameObject tauConGameObject;

        /// <summary>
        /// Called once in the lifetime of a script, after all Awake functions on all objects in a scene are called.
        /// </summary>
        private void Start()
        {
            tauConCanvas.SetActive(false);
        }

        /// <summary>
        /// Called every frame, but update interval times will vary depending on FPS.
        /// </summary>
        void Update()
        {

            // If the toggle input field is empty
            if (toggleCommand == string.Empty)
            {
                // Don't do anything
                return;
            }

            // On toggle
            if (Input.GetButtonDown(toggleCommand))
            {
                // Toggle console
                tauConCanvas.SetActive(!tauConCanvas.activeSelf);
                // If the console is active
                // Remove any added characters from the toggleCommand string
                // TODO: Get the value of Input button "Console" positive button and pass it here...might not be possible with default InputManager in Unity
                if (tauConCanvas.activeSelf)
                {
                    if (inputField.text.Contains("`"))
                    {
                        inputField.text = inputField.text.Replace("`", "");
                    }
                }


                // Then use utility method CaretToEnd IF reselectOnSubmit == false
                // Moves the caret to the end of the input
                if (!TauCon.Instance.reselectOnSubmit)
                {
                    StartCoroutine(TauCon.CaretToEnd(inputField));
                }
            }
        }
    }
}
