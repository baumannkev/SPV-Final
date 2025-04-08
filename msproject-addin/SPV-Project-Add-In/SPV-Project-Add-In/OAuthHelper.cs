using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace SPV_Project_Add_In
{
    /// <summary>
    /// Helper class for handling OAuth 2.0 authentication flow.
    /// </summary>
    public class OAuthHelper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Initiates the OAuth 2.0 flow to retrieve an access token.
        /// </summary>
        /// <returns>The access token as a string.</returns>
        public async Task<string> GetAccessTokenAsync()
        {
            // OAuth client credentials for a Web application.
            string clientId = "433099206626-unetmatjt151oj74pc56k5789t9kdaim.apps.googleusercontent.com";
            string clientSecret = "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";
            string redirectUri = "http://localhost:8080/";
            string state = "xyz";

            // Generate PKCE code verifier and challenge.
            string codeVerifier = GenerateCodeVerifier();
            string codeChallenge = GenerateCodeChallenge(codeVerifier);

            // Construct the authorization URL.
            string authUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                             "client_id=" + clientId +
                             "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                             "&response_type=code" +
                             "&scope=" + Uri.EscapeDataString("openid email profile") +
                             "&state=" + state +
                             "&code_challenge=" + codeChallenge +
                             "&code_challenge_method=S256";

            // Open the authorization URL in the default browser.
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // Start an HTTP listener to capture the redirect with the authorization code.
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(redirectUri);
            listener.Start();

            HttpListenerContext context = await listener.GetContextAsync();

            // Respond to the browser to indicate the process is complete.
            string responseHtml = @"<html><head>
            <script type='text/javascript'>
                if(window.location.search.length === 0 && window.location.hash.length > 0){
                    window.location = window.location.href.replace('#','?');
                }
            </script>
                        </head><body>Click here if you are not redirected in 11 seconds: <a href='https://spv-app-c59a1.firebaseapp.com/app'>SPV App</a></body></html>";
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            context.Response.OutputStream.Close();
            listener.Stop();

            // Extract the authorization code from the redirect URL.
            string code = HttpUtility.ParseQueryString(context.Request.Url.Query)["code"];
            if (string.IsNullOrEmpty(code))
                throw new Exception("Authorization code not found.");

            // Exchange the authorization code for an access token.
            TokenResponse tokenResponse = await ExchangeCodeForTokenAsync(clientId, clientSecret, code, codeVerifier, redirectUri);
            return tokenResponse.access_token;
        }

        /// <summary>
        /// Exchanges the authorization code for an access token.
        /// </summary>
        private async Task<TokenResponse> ExchangeCodeForTokenAsync(string clientId, string clientSecret, string code, string codeVerifier, string redirectUri)
        {
            var values = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "code", code },
                { "code_verifier", codeVerifier },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var content = new FormUrlEncodedContent(values);
            HttpResponseMessage response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            if (!response.IsSuccessStatusCode)
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception("Token endpoint error: " + errorResponse);
            }
            string responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TokenResponse>(responseString);
        }

        /// <summary>
        /// Generates a random code verifier for PKCE.
        /// </summary>
        private string GenerateCodeVerifier()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }
        }

        /// <summary>
        /// Generates a code challenge from the code verifier using SHA256.
        /// </summary>
        private string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.ASCII.GetBytes(codeVerifier);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }
        }
    }

    /// <summary>
    /// Represents the response from the token endpoint.
    /// </summary>
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string access_token { get; set; }

        [JsonProperty("expires_in")]
        public int expires_in { get; set; }

        [JsonProperty("token_type")]
        public string token_type { get; set; }

        [JsonProperty("refresh_token")]
        public string refresh_token { get; set; }
    }
}
