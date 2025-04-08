using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SPV_Project_Add_In
{
    /// <summary>
    /// Helper class for Firebase authentication-related operations.
    /// </summary>
    public class FirebaseAuthHelper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Exchanges a Google access token for Firebase credentials.
        /// </summary>
        /// <param name="googleAccessToken">The Google access token to exchange.</param>
        /// <returns>A tuple containing the Firebase UID, ID token, and email.</returns>
        /// <exception cref="System.Exception">Thrown when the token exchange fails.</exception>
        public async Task<(string firebaseUID, string idToken, string email)> ExchangeAccessTokenForFirebaseUID(string googleAccessToken)
        {
            // Firebase API key for authentication.
            string firebaseApiKey = "AIzaSyDTRmg89HOBmJHvxDjjGAsCFCv_F1tLl60";
            string signInUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={firebaseApiKey}";

            // Payload for the API request.
            var payload = new
            {
                postBody = $"access_token={googleAccessToken}&providerId=google.com",
                requestUri = "http://localhost",
                returnSecureToken = true,
                returnIdpCredential = true
            };

            // Serialize the payload to JSON.
            string jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Send the POST request to Firebase.
            HttpResponseMessage response = await httpClient.PostAsync(signInUrl, content);
            string responseString = await response.Content.ReadAsStringAsync();

            // Handle unsuccessful responses.
            if (!response.IsSuccessStatusCode)
            {
                throw new System.Exception("Error exchanging token: " + responseString);
            }

            // Parse the response to extract Firebase credentials.
            JObject obj = JObject.Parse(responseString);
            string firebaseUID = obj.Value<string>("localId");
            string idToken = obj.Value<string>("idToken");
            string email = obj.Value<string>("email");

            return (firebaseUID, idToken, email);
        }
    }
}
