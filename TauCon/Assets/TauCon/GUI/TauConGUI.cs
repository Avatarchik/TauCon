using UnityEngine;
using UnityEngine.UI;

namespace TauConsole
{

    [AddComponentMenu("Scripts/TauCon/TauConGUI")]
    /// <summary>
    /// This script must be attached to the main TauCon Canvas
    /// </summary>
    /// <remarks>
    /// Default UI Element Name: TauCon
    /// </remarks>
    public class TauConGUI : MonoBehaviour
    {

        [Header("UI Components")]
        public Canvas tauConCanvas;
        public GameObject versionPanel;
        public Text versionText;
        public GameObject outputPanel;
        public ScrollRect outputLogScrollRect;
        public RectTransform outputViewport;
        public RectTransform outputContent;
        public Text outputLogText;
        public InputField inputField;
        public Text inputText;
        public Text inputPlaceholderText;
        public Scrollbar scrollbar;
        public RectTransform scrollbarHandle;

        [Header("Caret Display")]
        public float caretBlinkRate = 1.5f;
        public int caretWidth = 10;
        public bool caretCustomColor = true;

        [Header("Fonts")]
        public Font versionFont;
        public Font outputFont;
        public Font inputFont;
        public Font placeholderFont;

        [Header("Font Sizes")]
        public int versionFontSize;
        public int outputFontSize;
        public int inputFontSize;
        public int placeholderFontSize;

        [Header("GUI Colors")]
        public Color32 versionPanelBackgroundRGBA = new Color32(46, 46, 46, 255);
        public Color32 versionTextRGBA = new Color32(131, 212, 179, 255);
        public Color32 outputPanelBackgroundRGBA = new Color32(58, 58, 58, 255);
        public Color32 inputFieldBackgroundRGBA = new Color32(73, 73, 73, 255);
        public Color32 inputTextRGBA = new Color32(188, 186, 184, 255);
        public Color32 inputPlaceholderTextRGBA = new Color32(164, 164, 164, 255);
        public Color32 inputCaretColorRGBA = new Color32(131, 212, 179, 255);
        public Color32 inputSelectionColorRGBA = new Color32(131, 212, 179, 125);
        public Color32 scrollbarBackgroundRGBA = new Color32(85, 85, 85, 255);
        public Color32 scrollbarHandleRGBA = new Color32(131, 212, 179, 255);

        #pragma warning disable
        private float outputContentHeight;
        #pragma warning enable

        private Vector2 outputContentReset = new Vector2(0f, 0f);

        /// <summary>
        /// Called once in the lifetime of a script, after all Awake functions on all objects in a scene are called.
        /// </summary>
        private void Start()
        {
            TauCon.OnOutput += OnOutput;
            // Set the TauConGUIInput component script var 'consoleGUI' to 'this'
            inputField.GetComponent<TauConGUIInput>().tauConGUI = this;

            // Initialize outputContent private vars
            outputContentHeight = outputContent.rect.height;

            // Initialize GUI colours
            InitConsoleGUI();

            // Set the version text (the text at the top of the console)
            // By default this will pull the Application Version from:
            // Edit > Project Settings > Player > Version, under Mac App Store Settings (it is a shared value)
            versionText.text = "TauCon // v" + Application.version;

            // Rebuild the UI to reflect caret/selection color changes
            RebuildOutputUI(outputContent, outputViewport, scrollbar, inputField);
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            StartCoroutine(TauCon.CaretToEnd(inputField));
        }

        /// <summary>
        /// Called when text is to be appended to the output log.
        /// </summary>
        /// <param name="line">The line to append to the output log.</param>
        private void OnOutput(string line)
        {
            if (outputLogText.text.Length > TauCon.Instance.maxOutputLength)
            {
                outputLogText.text = outputLogText.text.Substring((outputLogText.text.Length - TauCon.Instance.maxOutputLength), TauCon.Instance.maxOutputLength);
            }

            outputLogText.text += '\n' + line;
            RebuildOutputUI(outputContent, outputViewport, scrollbar, inputField);
        }

        /// <summary>
        /// Called when a submit event has announced.
        /// </summary>
        public void OnInput()
        {
            // Get the value of the input field
            string command = inputField.text;
            // If there's no command, return
            if (string.IsNullOrEmpty(command))
            {
                return;
            }

            // Otherwise continue...
            // Send command to console & eval
            TauCon.Eval(command);

            // If clearOnSubmit is enabled
            if (TauCon.Instance.clearOnSubmit)
            {
                // Clear the input field
                inputField.text = string.Empty;
            }
            // If reselectOnSubmit is enabled
            if (TauCon.Instance.reselectOnSubmit)
            {
                // Start a coroutine to place the cursor at the end of the text in the input
                StartCoroutine(TauCon.CaretToEnd(inputField));
            }
            // And then rebuild the UI elements that need to be rebuilt to show changes
            RebuildOutputUI(outputContent, outputViewport, scrollbar, inputField);
        }

        /// <summary>
        /// Rebuilds the output UI to account for log output (resizes the outputContentScrollRect height)
        /// <para>RectTransform content</para>
        /// <para>RectTransform parent</para>
        /// <para>Scrollbar scrollbar</para>
        /// </summary>
        private void RebuildOutputUI(RectTransform content, RectTransform parent, Scrollbar scrollbar, InputField inputField)
        {
            // Rebuild content RT
            content.GetComponent<RectTransform>().anchoredPosition = parent.position;
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(content.rect.width, outputContentHeight = outputLogText.preferredHeight);
            content.anchorMin = outputContentReset;
            content.anchorMax = outputContentReset;
            content.pivot = outputContentReset;
            content.transform.SetParent(parent);

            // Rebuild scrollbar
            scrollbar.Rebuild(CanvasUpdate.Prelayout);

            // Rebuild InputField
            inputField.Rebuild(CanvasUpdate.PreRender);
        }

        /// <summary>
        /// Sets all GUI image color values and settings.
        /// </summary>
        private void InitConsoleGUI()
        {
            // Colors
            versionPanel.GetComponent<Image>().color = versionPanelBackgroundRGBA;
            versionText.color = versionTextRGBA;
            outputPanel.GetComponent<Image>().color = outputPanelBackgroundRGBA;
            outputLogScrollRect.GetComponent<Image>().color = outputPanelBackgroundRGBA;
            inputField.GetComponent<Image>().color = inputFieldBackgroundRGBA;
            inputText.color = inputTextRGBA;
            inputPlaceholderText.color = inputPlaceholderTextRGBA;
            inputField.selectionColor = inputSelectionColorRGBA;
            inputField.caretColor = inputCaretColorRGBA;
            scrollbar.GetComponent<Image>().color = scrollbarBackgroundRGBA;
            scrollbarHandle.GetComponent<Image>().color = scrollbarHandleRGBA;

            // Options
            inputField.caretBlinkRate = caretBlinkRate;
            inputField.caretWidth = caretWidth;
            inputField.customCaretColor = caretCustomColor;
        }
    }
}
