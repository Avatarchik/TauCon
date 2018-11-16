using UnityEngine;
using UnityEngine.UI;

namespace TauConsole
{

    [AddComponentMenu("Scripts/TauCon/TauConGUIInput")]
    /// <summary>
    /// This script must be attached to the InputField on the console
    /// </summary>
    /// <remarks>
    /// Default UI Element Name: InputField
    /// </remarks>
    public class TauConGUIInput : MonoBehaviour
    {

        [HideInInspector]
        public TauConGUI tauConGUI;
        private InputField inputField;

        /// <summary>
        /// Called once in the lifetime of a script, after all Awake functions on all objects in a scene are called.
        /// </summary>
        private void Start()
        {
            inputField = GetComponent<InputField>();
            inputField.onEndEdit.AddListener(OnEndEdit);
        }

        /// <summary>
        /// A method to act on the onEndEdit event for an InputField in Unity, checks for "Submit" event and calls tauConGUI.OnInput()
        /// </summary>
        /// <param name="line"></param>
        private void OnEndEdit(string line)
        {
            if (Input.GetButtonDown("Submit"))
            {
                tauConGUI.OnInput();
            }
        }
    }

}
