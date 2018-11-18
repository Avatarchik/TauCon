﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TauConsole
{

    [AddComponentMenu("Scripts/TauCon/TauCon")]
    /// <summary>
    /// This script must be attached to the main TauCon Canvas
    /// Default UI Element Name: TauCon
    /// </summary>
    public class TauCon : MonoBehaviour
    {

        [Header("UI Components")]
        public Canvas tauConCanvas;
        public GameObject versionPanel;
        public Text versionText;
        public GameObject outputPanel;
        public ScrollRect outputLogScrollRect;
        public Text outputLogText;
        public InputField inputField;
        public Text inputText;
        public Text inputPlaceholderText;
        public Scrollbar scrollbar;
        public RectTransform scrollbarHandle;
        public static Text _outputLogText;

        [Header("Console Options")]
        public char commandSymbol = '$';
        public int maxOutputLength = 1500;
        public bool clearOnSubmit = true;
        public bool reselectOnSubmit = false;
        public bool outputUnityLog = false;
        public bool outputStackTrace = false;
        public bool allowEmptyOutput = false;
        public bool newlineOnOutput = true;
        public int characterLimit = 65;

        [Header("Log Colors")]
        public Color32 commandColorRGBA = new Color32(131, 212, 179, 255);
        public Color32 logColorRGBA = new Color32(188, 186, 184, 255);
        public Color32 assertColorRGBA = new Color32(214, 200, 255, 255);
        public Color32 warningColorRGBA = new Color32(254, 206, 168, 255);
        public Color32 errorColorRGBA = new Color32(255, 132, 123, 255);
        public Color32 exceptionColorRGBA = new Color32(232, 74, 95, 255);
        public Color32 paramColorRGBA = new Color32(163, 222, 131, 255);
        public Color32 helpColorRGBA = new Color32(157, 229, 255, 255);
        public Color32 helpListColorRGBA = new Color32(213, 253, 255, 255);

        // Vars to store hex of RGBA
        public static string commandColor;
        public static string logColor;
        public static string assertColor;
        public static string warningColor;
        public static string errorColor;
        public static string exceptionColor;
        public static string paramColor;
        public static string helpColor;
        public static string helpListColor;

        private static Color32 initialInputSelectionColor;
        private static Color32 initialCaretColor;
        private static int currentIndex;

        public static TauCon Instance;
        public static Dictionary<string, TauConCommand> Commands = new Dictionary<string, TauConCommand>();
        public static List<string> History = new List<string>();
        public delegate void ConsoleListener(string line);
        public static event ConsoleListener OnOutput;

        /// <summary>Used to color text in the logger by wrapping text in color tags
        /// <para>string text</para>
        /// <para>[string color(hex) = null]</para>
        /// </summary>
        public static string Colorify(string text, string color = null)
        {
            if (color == null)
            {
                return "<color=#" + logColor + ">" + text + "</color>";
            }
            else
            {
                return "<color=#" + color + ">" + text + "</color>";
            }
        }

        static TauCon() { }

        #region Unity Callbacks

        /// <summary>
        /// Called once in the lifetime of a script, before any Start functions are called.
        /// </summary>
        private void Awake()
        {
            // If a console instance doesn't already exist
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            // Init Commands Dictionary
            Commands = new Dictionary<string, TauConCommand>();
        }

        /// <summary>
        /// Called once in the lifetime of a script, after all Awake functions on all objects in a scene are called.
        /// </summary>
        private void Start()
        {
            _outputLogText = outputLogText;

            // If Unity log output is enabled
            if (outputUnityLog)
            {
                Application.logMessageReceived += new Application.LogCallback(this.HandleUnityLog);
            }

            // Init current index for History
            currentIndex = 0;

            InitConsoleOptions();
            InitLogColors();
            InitDefaultCommands();

            // Initialize OutputLog as empty string (remove test text from Editor)
            outputLogText.text = string.Empty;
            Debug.Log("Output has been cleared.");

            // Initialize console as inactive
            tauConCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called every frame, but update interval times will vary depending on FPS.
        /// </summary>
        private void Update()
        {
            // Check for active console and 'return' event for command input
            if (tauConCanvas.gameObject.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    // Only parse text if there is text
                    if (inputText.text != "")
                    {

                        // Clear the console input field
                        inputText = null;
                    }
                    // And focus the console input field again
                    inputField.ActivateInputField();
                }
            }

            // Check for up/down arrow for History
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                FetchHistory(KeyCode.UpArrow);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                FetchHistory(KeyCode.DownArrow);
            }

            // FIXME: Fix to NOT allow scrolling the outputContent with click+drag
            // Prevent any mouse/touch scroll/drag interaction
            // scrollRect.OnBeginDrag(null);
            // scrollRect.OnDrag(null);
            // scrollRect.OnEndDrag(null);
            // if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) {
            // 	if (EventSystem.current.currentSelectedGameObject.name == "LogView") {
            // 		return;
            // 	}
            // }
        }

        #endregion

        #region Logging

        private static string LOGERROR;
        private static string LOGWARNING;
        private static string LOGDEFAULT;
        private static string LOGEXCEPTION;
        private static string LOGASSERT;
        private static string LOGINVALIDCMD;
        private static string LOGCMDNOTFOUND;

        /// <summary>
        /// Set all default log messages and their colors.
        /// </summary>
        private static void InitDefaultLogMessages()
        {
            LOGINVALIDCMD = Colorify("Command invalid: ", exceptionColor);
            LOGCMDNOTFOUND = Colorify("Command unrecognized: ", exceptionColor);
            LOGERROR = Colorify("Error: ", errorColor);
            LOGWARNING = Colorify("Warning: ", warningColor);
            LOGDEFAULT = Colorify("Log: ", logColor);
            LOGEXCEPTION = Colorify("Exception: ", exceptionColor);
            LOGASSERT = Colorify("Assert: ", assertColor);
        }

        /// <summary>
        /// Initialize all log colors in Hex from the given RGBA colors set by default or in the editor.
        /// </summary>
        private void InitLogColors()
        {
            commandColor = ColorUtility.ToHtmlStringRGBA(commandColorRGBA);
            logColor = ColorUtility.ToHtmlStringRGBA(logColorRGBA);
            assertColor = ColorUtility.ToHtmlStringRGBA(assertColorRGBA);
            warningColor = ColorUtility.ToHtmlStringRGBA(warningColorRGBA);
            errorColor = ColorUtility.ToHtmlStringRGBA(errorColorRGBA);
            exceptionColor = ColorUtility.ToHtmlStringRGBA(exceptionColorRGBA);
            paramColor = ColorUtility.ToHtmlStringRGBA(paramColorRGBA);
            helpColor = ColorUtility.ToHtmlStringRGBA(helpColorRGBA);
            helpListColor = ColorUtility.ToHtmlStringRGBA(helpListColorRGBA);
        }

        /// <summary>
        /// Initialize all console options.
        /// </summary>
        private void InitConsoleOptions()
        {

            inputField.characterLimit = characterLimit;
            initialInputSelectionColor = inputField.selectionColor;
            initialCaretColor = inputField.caretColor;
        }

        private void HandleUnityLog(string logString, string trace, LogType logType)
        {
            string output = String.Empty;

            switch (logType)
            {
                case LogType.Error:
                    output += LOGERROR;
                    break;
                case LogType.Assert:
                    output += LOGASSERT;
                    break;
                case LogType.Warning:
                    output += LOGWARNING;
                    break;
                case LogType.Log:
                    output += LOGDEFAULT;
                    break;
                case LogType.Exception:
                    output += LOGEXCEPTION;
                    break;
                default:
                    return;
            }

            output += logString + (Instance.outputStackTrace ? "\n" + Colorify(trace, logColor) : String.Empty);
            Print(output);
        }

        #endregion

        #region Adding & Removing ConsoleCommands

        /// <summary>
        /// Removes a command from the Commands Dictionary.
        /// </summary>
        /// <param name="command">The command to remove from the Commands Dictionary.</param>
        /// <returns>Return true or false based on if the Commands list contains the given command argument.</returns>
        public bool RemoveCommand(string command)
        {
            if (Commands.ContainsKey(command))
            {
                Commands.Remove(command);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add a command from the Commands Dictionary.
        /// </summary>
        /// <param name="name">The capitalized name of the command.</param>
        /// <param name="command">The command string used to invoke the command.</param>
        /// <param name="description">A short description of the command.</param>
        /// <param name="method">The method to call when the command is invoked.</param>
        /// <param name="helpText">The help text for the command.</param>
        /// <returns></returns>
        public static bool AddCommand(string name, string command, string description, Func<string, string> method, string helpText = null)
        {
            if (string.IsNullOrEmpty(command))
            {
                Debug.LogError("Could not add command to console { " + command + " }, command is empty");
                return false;
            }

            if (Commands.ContainsKey(command))
            {
                Debug.LogError("Could not add command to console { " + command + " }, command already exists");
                return false;
            }

            // Add the command to the dictionary
            TauConCommand consoleCommand = new TauConCommand(name, command, description, method, helpText);
            Commands.Add(command, consoleCommand);

            SortCommands();

            return true;
        }

        #endregion

        #region Command Eval

        /// <summary>
        /// Evaluate given string (execute command)
        /// <returns> Direct output of the method that is called</returns>
        public static string Eval(string command)
        {

            string output = string.Empty;

            // Print command
            Print(Instance.commandSymbol + " " + Colorify(command, commandColor));

            if (string.IsNullOrEmpty(command))
            {
                Debug.Log("Invalid Cmd");
                output = LOGINVALIDCMD;
                return Print(output);
            }

            command.ToLower();

            string[] parsedCmd = command.Split(' ');

            // Raw command without args
            string rawCmd = parsedCmd[0];

            // Remove any extra spaces and store for History use
            string trimmedCmd = string.Join(" ", parsedCmd).Trim();

            // Check to see if our History array does NOT contain the evaluated cmd
            if (!History.Contains(trimmedCmd))
            {
                // If it does not contain it, prepend it
                History.Insert(0, trimmedCmd);
            }
            else
            {
                // If it does contain it, remove it from the array and prepend it
                History.Remove(trimmedCmd);
                History.Insert(0, trimmedCmd);
            }

            // DEV NOTE:
            // The reason I have decided to add this check *after* the History logic
            // is so that the History acts exactly like bash. Even if the past command
            // or argument is invalid it will add it regardless. 
            // This is a personal preference thing which I may modify in the future.

            // If the command is not found in the Commands Dictionary
            if (!Commands.ContainsKey(rawCmd))
            {
                Debug.Log("Command not found");
                output = LOGCMDNOTFOUND + rawCmd;
                return Print(output);
            }

            // Call the ExtractParameters method and store the parameters in a variable
            string parameters = ExtractParameters(command, rawCmd);
            // Combine command and parameters
            output = Commands[rawCmd].method(parameters);

            // Newline check
            if (Instance.newlineOnOutput)
            {
                output += "\n";
            }

            currentIndex = -1;
            // Return the output (print to the output log)
            return Print(output);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Extract the command and any parameters given.
        /// </summary>
        /// <param name="command">The command used.</param>
        /// <param name="rawCmd">The raw command without parameters.</param>
        /// <returns></returns>
        private static string ExtractParameters(string command, string rawCmd)
        {
            string parameters = (command.Length > rawCmd.Length) ? command.Substring(rawCmd.Length + 1, command.Length - (rawCmd.Length + 1)) : string.Empty;
            return parameters.Trim();
        }

        /// <summary>
        /// Sort the commands alphabetically in the dictionary (for help list)
        /// </summary>
        private static void SortCommands()
        {
            Commands = Commands.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Moves caret to given pos
        /// This sets the colors to transparent for 1 frame to overcome a quirk in Unity's UI.
        /// </summary>
        /// <param name="inputField">The input field used.</param>
        /// <returns>null</returns>
        public static IEnumerator CaretToEnd(InputField inputField)
        {
            // Focus the given input field
            inputField.ActivateInputField();
            // Hide the selection color
            inputField.selectionColor = new Color32(0, 0, 0, 0);
            inputField.caretColor = new Color32(0, 0, 0, 0);
            // Wait for 1 frame
            yield return null;
            // Move the input field caret to the end of the text
            inputField.caretPosition = inputField.text.Length;
            // Reset selection color to initial color
            inputField.selectionColor = initialInputSelectionColor;
            inputField.caretColor = initialCaretColor;
            inputField.Rebuild(CanvasUpdate.PreRender);
        }

        #endregion

        #region Configuration File & Parsing

        // TODO: Write this parse config method at a later date
        /// <summary>
        /// Parse the console.cfg file 
        /// </summary>
        public static void ParseConfig()
        {
            // code HEYAAAAA!!!
        }

        #endregion

        #region Printing & Output to Listeners

        private static string SendOutputToListeners(string output)
        {
            if (OnOutput != null) { OnOutput(output); }
            return output;
        }

        /// <summary>
        /// Send text to listeners and return text.
        /// </summary>
        /// <param name="text">The string of text to send.</param>
        /// <returns>Returns either an empty string if text is empty or the text given.</returns>
        public static string Print(string text)
        {
            if (text == null)
            {
                return String.Empty;
            }

            // If allowEmptyOutput is false, do not send to listeners
            if (TauCon.Instance.allowEmptyOutput && text == string.Empty)
            {
                return String.Empty;
            }

            SendOutputToListeners(text);
            return text;
        }

        /// <summary>
        /// Overrides Monobehaviour's "print" method with <see cref="Print(string)"/> method.
        /// </summary>
        /// <param name="text">The text given.</param>
        /// <returns>Returns the result of calling <see cref="Print(string)"/> with the given text.</returns>
        public static string print(string text)
        {
            return Print(text);
        }

        #endregion

        #region Built-in Console Commands

        /// <summary>
        /// Initialize all default commands 
        /// </summary>
        private void InitDefaultCommands()
        {
            AddCommand("Help", "help", "Show help on how to use the console.", CommandHelp.GetHelp);
            AddCommand("Quit", "quit", "Quits the application immediately.", CommandQuit.QuitApplication);
            AddCommand("Clear", "clear", "Clears the output log of all text.", CommandClear.ClearLog);
            AddCommand("Volume", "volume", "Change volume, value between 0 and 1",
                CommandVolume.ChangeVolume, "Usage: { "
                + Colorify("volume [value between 0 and 1]", paramColor)
                + " } \nExample: " + Colorify("volume 0.5", paramColor)
                + " sets the volume to 50%\nHint: Input { "
                + Colorify("volume", paramColor)
                + " } to output the current volume value.");
        }

        #endregion

        #region Command History

        /// <summary>
        /// Populate InputField based on command history
        /// </summary>
        /// <param name="dir">KeyCode</param>
        private void FetchHistory(KeyCode dir)
        {
            Debug.Log("before: " + currentIndex);
            //Debug.Log(History.ElementAt(currentIndex+1));
            //Debug.Log(History.ToString());
            switch(dir)
            {
                case KeyCode.UpArrow:
                    if (currentIndex < 0)
                    {
                        currentIndex += 1;
                        inputField.text = History[currentIndex];
                        break;
                    }
                    else if (currentIndex == History.Count - 1)
                    {
                        inputField.text = History[History.Count - 1];
                        break;
                    }
                    else
                    {
                        currentIndex += 1;
                        inputField.text = History.ElementAt(currentIndex);
                        break;
                    }
                case KeyCode.DownArrow:
                    if (currentIndex <= 0)
                    {
                        currentIndex = -1;
                        inputField.text = "";
                        StartCoroutine(CaretToEnd(inputField));
                        break;
                    }
                    else
                    {
                        currentIndex -= 1;
                        inputField.text = History.ElementAt(currentIndex);
                        StartCoroutine(CaretToEnd(inputField));
                        break;
                    }
            }
            Debug.Log("after: " + currentIndex);
        } 

        #endregion
    }
}
