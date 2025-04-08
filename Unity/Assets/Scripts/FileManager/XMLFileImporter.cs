using System.Xml;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.IO;
using UnityEditor;
using TMPro;

public class FileImporter : MonoBehaviour
{
    public Button importButton;
    public XMLTaskLoader xmltaskloader;
    
    [DllImport("__Internal")]
    private static extern void OpenFileDialog(string gameObjectName, string callbackMethod);

    /// <summary>
    /// Initializes the FileImporter, checks for required components, and sets up the import button listener.
    /// </summary>
    void Start()
    {
        if (GetComponent<FileImporter>() == null)
        {
            Debug.LogError("FileImporter component is not attached to " + gameObject.name);
        }
        else
        {
            Debug.Log("FileImporter component attached");
        }
        // Check if the GameObject is active in the scene
        if (gameObject.activeInHierarchy)
        {
            Debug.Log(gameObject.name + " is active in the scene.");
        }
        else
        {
            Debug.LogError(gameObject.name + " is NOT active in the scene.");
        }
        gameObject.name = "UIManager"; // Assign the correct name here
        importButton.onClick.AddListener(PickFile);
    }
    
    /// <summary>
    /// Handles file picking logic based on the platform (WebGL or Editor/Standalone).
    /// </summary>
    public void PickFile()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL: Use the JavaScript file picker
                OpenFileInWebGL();
    #else
            // Unity Editor or Standalone: Use native file picker
            OpenFileInEditor();
#endif
        }

    /// <summary>
    /// Opens the file picker in WebGL using a JavaScript function.
    /// </summary>
    void OpenFileInWebGL()
    {
        Debug.Log("GameObject Name: " + gameObject.name);
        OpenFileDialog(gameObject.name, "OnFileSelected");
    }
    
    /// <summary>
    /// Callback method for when a file is selected in WebGL. Processes the file content.
    /// </summary>
    public void OnFileSelected(string fileContent)
    {
        // Debug.Log("File content received: " + fileContent);
        if (xmltaskloader != null)
        {
            Debug.Log("xmltaskloader found, processing file...");
            xmltaskloader.LoadTasksFromXML(fileContent);
        }
        else
        {
            Debug.LogError("xmltaskloader is missing or null.");
        }
    }
    
    /// <summary>
    /// Opens the file picker in the Unity Editor or Standalone builds and processes the selected file.
    /// </summary>
    void OpenFileInEditor()
    {
        // Use UnityEditor or System.Windows.Forms for file picking in the Editor
#if UNITY_EDITOR
        string filePath = UnityEditor.EditorUtility.OpenFilePanel("Select an XML file", "", "xml");
        if (!string.IsNullOrEmpty(filePath))
        {
            // Read the file content
            string fileContent = File.ReadAllText(filePath);
            // Debug.Log("File content read: " + fileContent);
        
            // Load the tasks from the XML string
            xmltaskloader.LoadTasksFromXML(fileContent);
        }
#endif
    }
}
