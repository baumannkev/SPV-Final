using System;
using System.Windows.Forms;
using System.Web;

namespace SPV_Project_Add_In
{
    /// <summary>
    /// A form that handles user sign-in using Firebase OAuth.
    /// </summary>
    public partial class SignInForm : Form
    {
        // This property will hold the authenticated user's UID (or access token).
        public string AuthenticatedUserId { get; private set; }

        // Firebase OAuth URL for user authentication.
        // Ensure your Firebase project and OAuth settings (including the redirect URI) are configured correctly.
        private readonly string firebaseOAuthUrl =
            "https://accounts.google.com/o/oauth2/v2/auth?" +
            "client_id=433099206626-0gikv5vrouehbtk8q5pt55ca7amb1vf4.apps.googleusercontent.com" +
            "&redirect_uri=http://localhost:8080" +
            "&response_type=token" +
            "&scope=email%20profile";

        /// <summary>
        /// Initializes the sign-in form and sets up event handlers.
        /// </summary>
        public SignInForm()
        {
            InitializeComponent();
            this.Load += SignInForm_Load;
        }

        /// <summary>
        /// Handles the form load event. Navigates to the Firebase OAuth sign-in page.
        /// </summary>
        private void SignInForm_Load(object sender, EventArgs e)
        {
            // Set up an event handler to capture when navigation completes.
            webBrowser1.Navigated += WebBrowser1_Navigated;
            // Navigate to the Firebase OAuth sign-in page.
            webBrowser1.Navigate(firebaseOAuthUrl);
        }

        /// <summary>
        /// Handles the navigation event of the web browser control.
        /// Checks if the URL contains the access token or UID.
        /// </summary>
        private void WebBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            // Check if the URL contains the access token.
            if (e.Url.AbsoluteUri.Contains("access_token"))
            {
                var query = HttpUtility.ParseQueryString(e.Url.Query);
                string accessToken = query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Store the access token and close the form with a success result.
                    this.AuthenticatedUserId = accessToken;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            // Note: If Firebase uses URL fragments (after a '#' symbol), handle them by parsing e.Url.Fragment.
        }

        private WebBrowser webBrowser1;
    }
}
