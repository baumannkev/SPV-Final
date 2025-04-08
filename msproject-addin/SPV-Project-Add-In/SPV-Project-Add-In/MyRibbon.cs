using Google.Cloud.Firestore;
using Microsoft.Office.Interop.MSProject;
using Microsoft.Office.Tools.Ribbon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Application = Microsoft.Office.Interop.MSProject.Application;
using Exception = System.Exception;
using Formatting = Newtonsoft.Json.Formatting;
using MsTask = Microsoft.Office.Interop.MSProject.Task;
using Task = System.Threading.Tasks.Task;

namespace SPV_Project_Add_In
{
    /// <summary>
    /// Handles the ribbon UI and core functionality for exporting MS Project tasks to Firestore.
    /// </summary>
    public partial class ExportToSPV
    {
        private FirestoreDb db; // Firestore database instance.
        private static readonly HttpClient client = new HttpClient();

        // Project and user identifiers.
        private static string currentProjectId = null;
        private static string currentUserId = null;
        private static string currentIdToken = null; // Firebase ID token
        private static string currentUserEmail = null; // User email from Firebase

        // A simple hash of the last task export.
        private static string lastTasksHash = string.Empty;
        // Timer to poll for task changes.
        private Timer taskPollTimer;

        /// <summary>
        /// Handles the ribbon load event. Initializes the add-in and starts the task polling timer.
        /// </summary>
        private void Ribbon1_Load(object sender, RibbonUIEventArgs e)
        {
            try
            {
                // Inform the user that the add‑in is loaded (optional)
                // You can remove this if you prefer no pop-up at load.
                MessageBox.Show("SPV Add-In loaded. Click the Export button to sign in and export data.");

                // Start a timer to poll for task changes every 10 seconds.
                taskPollTimer = new Timer();
                taskPollTimer.Interval = 10000;
                taskPollTimer.Tick += TaskPollTimer_Tick;
                taskPollTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during Ribbon load: " + ex.Message);
            }
        }

        /// <summary>
        /// Authenticates the user via OAuth and exchanges the Google access token for Firebase credentials.
        /// </summary>
        private async Task AuthenticateUser()
        {
            try
            {
                OAuthHelper oauthHelper = new OAuthHelper();
                string googleAccessToken = await oauthHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(googleAccessToken))
                {
                    // You can log this error instead of showing a pop-up.
                    return;
                }

                FirebaseAuthHelper firebaseAuth = new FirebaseAuthHelper();
                var (firebaseUID, idToken, email) = await firebaseAuth.ExchangeAccessTokenForFirebaseUID(googleAccessToken);
                if (!string.IsNullOrEmpty(firebaseUID))
                {
                    currentUserId = firebaseUID;
                    currentIdToken = idToken;
                    currentUserEmail = email;
                    InitializeFirestore();
                }
                else
                {
                    // Log error or handle silently.
                    return;
                }
            }
            catch (Exception ex)
            {
                // Log error instead of showing a pop-up.
            }
        }

        /// <summary>
        /// Initializes the Firestore database connection using the Firebase ID token.
        /// </summary>
        private void InitializeFirestore()
        {
            try
            {
                string projectId = "spv-app-c59a1";
                var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken(currentIdToken);
                FirestoreDbBuilder builder = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    Credential = credential
                };
                db = builder.Build();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Firestore initialization error: " + ex.Message);
            }
        }

        /// <summary>
        /// Handles the Export button click event. Authenticates the user, extracts tasks, and sends them to Firestore.
        /// </summary>
        private async void btnExportToSPV_Click(object sender, RibbonControlEventArgs e)
        {
            try
            {
                // Authenticate if necessary.
                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentIdToken))
                {
                    await AuthenticateUser();
                    if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentIdToken))
                    {
                        MessageBox.Show("Authentication is required to export data.");
                        return;
                    }
                }

                // Ensure Firestore is initialized.
                if (db == null)
                {
                    InitializeFirestore();
                    if (db == null)
                    {
                        MessageBox.Show("Firestore is not initialized. Cannot export data.");
                        return;
                    }
                }

                var taskList = ExtractTasks();
                if (taskList != null)
                {
                    await Task.Run(async () => await SendDataToFirestore(taskList, currentProjectId));
                    lastTasksHash = ComputeHash(ConvertToJson(taskList));
                }

                // Automatically redirect the user without a pop-up confirmation.
                Process.Start(new ProcessStartInfo("https://spv-app-c59a1.web.app/app")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting data: " + ex.Message);
            }
        }

        /// <summary>
        /// Timer tick handler that checks for task changes and exports them if changes are detected.
        /// </summary>
        private void TaskPollTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(lastTasksHash))
                    return;

                var taskList = ExtractTasks();
                if (taskList != null)
                {
                    string json = ConvertToJson(taskList);
                    string currentHash = ComputeHash(json);
                    if (currentHash != lastTasksHash)
                    {
                        lastTasksHash = currentHash;
                        Task.Run(async () =>
                        {
                            try
                            {
                                await SendDataToFirestore(taskList, currentProjectId);
                            }
                            catch (Exception ex)
                            {
                                // Optionally log error without interrupting the user.
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in task polling: " + ex.Message);
            }
        }

        /// <summary>
        /// Computes an MD5 hash for the input string. Used to detect changes in task data.
        /// </summary>
        private string ComputeHash(string input)
        {
            try
            {
                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("X2"));
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error computing hash: " + ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts tasks from the active MS Project and converts them into a list of TaskData objects.
        /// </summary>
        private List<TaskData> ExtractTasks()
        {
            try
            {
                Application projectApp = Globals.ThisAddIn.Application;
                if (projectApp.ActiveProject == null)
                {
                    MessageBox.Show("No project is open.");
                    return null;
                }
                Project project = projectApp.ActiveProject;
                string newTitle = project.Title;
                if (string.IsNullOrEmpty(newTitle))
                    newTitle = "Unknown Project";
                if (currentProjectId == null || currentProjectId != newTitle)
                    currentProjectId = newTitle;

                var extractedTasks = project.Tasks
                    .Cast<MsTask>()
                    .Where(task => task != null)
                    .Select(task =>
                    {
                        double durationDays = 0;
                        try
                        {
                            // Convert from minutes to hours, then to work days (8 hours per day)
                            durationDays = ((double)task.Duration / 60) / 8;
                        }
                        catch (Exception)
                        {
                            double.TryParse(task.Duration.ToString(), out durationDays);
                        }
                        return new TaskData
                        {   
                            UID = task.UniqueID.ToString(),
                            Name = task.Name,
                            Critical = task.Critical ? "1" : "0",
                            Start = DateTime.SpecifyKind(task.Start, DateTimeKind.Utc),
                            Finish = DateTime.SpecifyKind(task.Finish, DateTimeKind.Utc),
                            Duration = durationDays,
                            OutlineLevel = task.OutlineLevel,
                            WBS = task.WBS,
                            PredecessorUIDs = task.PredecessorTasks != null
                                ? task.PredecessorTasks.Cast<MsTask>().Select(pred => pred.UniqueID.ToString()).ToList()
                                : new List<string>()
                        };
                    })
                    .ToList();

                // Debug output for task durations
                //foreach (var task in extractedTasks)
                //{
                //    Debug.WriteLine($"Task: {task.Name} - Duration: {task.Duration}");
                //}

                return extractedTasks;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error extracting tasks: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Converts a list of tasks to a JSON string for debugging or export purposes.
        /// </summary>
        private string ConvertToJson(List<TaskData> tasks)
        {
            try
            {
                return JsonConvert.SerializeObject(new { tasks }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error converting tasks to JSON: " + ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Sends the extracted task data to Firestore under the authenticated user's account.
        /// </summary>
        private async Task SendDataToFirestore(List<TaskData> tasks, string projectId)
        {
            try
            {
                string userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId) || userId == "default_user")
                {
                    MessageBox.Show("No authenticated user id found. Please sign in.");
                    return;
                }
                DocumentReference projectDocRef = db.Collection("users")
                    .Document(userId)
                    .Collection("projects")
                    .Document(projectId);
                await projectDocRef.SetAsync(new { CreatedAt = DateTime.UtcNow, ownerId = userId }, SetOptions.MergeAll);
                CollectionReference tasksRef = projectDocRef.Collection("tasks");
                foreach (var task in tasks)
                {
                    DocumentReference docRef = tasksRef.Document(task.UID);
                    await docRef.SetAsync(task, SetOptions.MergeAll);
                }
                // Logging export success without a pop-up to block redirection.
                Debug.WriteLine($"Tasks successfully exported to Firestore under user: {currentUserEmail}, project: {projectId}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Firestore error: " + ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the current authenticated user ID or returns a default value if not authenticated.
        /// </summary>
        private string GetCurrentUserId()
        {
            return string.IsNullOrEmpty(currentUserId) ? "default_user" : currentUserId;
        }
    }

    /// <summary>
    /// Represents a task in MS Project with properties for Firestore serialization.
    /// </summary>
    [FirestoreData]
    public class TaskData
    {
        [FirestoreProperty]
        public string UID { get; set; }
        [FirestoreProperty]
        public string Critical { get; set; }
        [FirestoreProperty]
        public string Name { get; set; }

        private DateTime _start;
        private DateTime _finish;

        [FirestoreProperty]
        public DateTime Start
        {
            get => _start;
            set => _start = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        [FirestoreProperty]
        public DateTime Finish
        {
            get => _finish;
            set => _finish = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private double _duration;

        [FirestoreProperty]
        public double Duration
        {
            get => _duration;
            set => _duration = value;
        }

        [FirestoreProperty]
        public string WBS { get; set; }
        [FirestoreProperty]
        public int OutlineLevel { get; set; }
        [FirestoreProperty]
        public List<string> PredecessorUIDs { get; set; } = new List<string>();
    }
}
