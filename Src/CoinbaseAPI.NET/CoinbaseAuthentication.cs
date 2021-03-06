﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;

    using Utilities;

    /// <summary>
    /// This exception type is thrown when Coinbase authentication fails - this may mean that invalid parameters were supplied,
    /// the connection timed out, or the user has revoked access to the application.
    /// </summary>
    public class CoinbaseAuthenticationException : Exception
    {
        public HttpValueCollection Parameters { get; private set; }
        public HttpStatusCode Code { get; private set; }

        /// <summary>
        /// Constructed using the parameters that lead to the failure and the status code that resulted.
        /// </summary>
        /// <param name="parameters">The parameters that lead to the error.</param>
        /// <param name="code">The HTTP Status Code returned.</param>
        internal CoinbaseAuthenticationException(HttpValueCollection parameters, HttpStatusCode code)
        {
            Parameters = parameters;
            Code = code;
        }

        /// <summary>
        /// Lists the causes of the exception.
        /// </summary>
        /// <returns>The exception in human readable form.</returns>
        public override string ToString()
        {
            return String.Format("Coinbase authorization failed with the HTTP status code ({0}) and with these parameters: [{1}]", 
                Code, 
                String.Join(",", from parameter in Parameters select "{" + parameter.Key + "," + parameter.Value + "}"));
        }
    }

    /// <summary>
    /// This static class has methods for performing authentication with Coinbase.
    /// </summary>
    public static class CoinbaseAuthentication
    {
        /// <summary>
        /// This private method accesses oauth/token, but takes HTTP parameters as an input.
        /// 
        /// This is a helper to implement the two different authentication methods.
        /// </summary>
        /// <param name="parameters">The URI parameters to sent to oauth/token</param>
        /// <returns>A task for the authentication response.</returns>
        private static async Task<AuthResponse> GetTokensAsync(HttpValueCollection parameters)
        {
            var uri = new UriBuilder("https://coinbase.com/oauth/token")
            {
                Query = parameters.ToString()
            }.Uri;

            using (var client = new HttpClient())
            {
                var httpResponse = await client.PostAsync(uri, new StringContent("")).ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new CoinbaseAuthenticationException(parameters, httpResponse.StatusCode);
                }
                
                return JsonConvert.DeserializeObject<AuthResponse>(await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
        }

        /// <summary>
        /// Creates an HttpValueCollection with the common parameters for each auth method - namely the client keys.
        /// </summary>
        /// <param name="clientId">The id of the application.</param>
        /// <param name="clientSecret">The application's secret.</param>
        /// <returns>An HttpValueCollection populated with these values.</returns>
        private static HttpValueCollection GetBasicParameters(string clientId, string clientSecret)
        {
            var parameters = new HttpValueCollection();
            parameters.Add("client_id", clientId);
            parameters.Add("client_secret", clientSecret);
            return parameters;
        }

        /// <summary>
        /// This authentication method requests the user's OAuth tokens for the first time - this requires providing a
        /// code which is generated by the application's OAuth login.
        /// 
        /// For more information on this process see https://coinbase.com/docs/api/authentication#oauth2
        /// </summary>
        /// <param name="clientId">The application's id.</param>
        /// <param name="clientSecret">The application's secret.</param>
        /// <param name="redirectUri">The redirect uri for the request - should match the uri listed for the application.</param>
        /// <param name="code">The authorization code created through the authorization callback.</param>
        /// <returns>A Task for an AuthResponse.</returns>
        public static Task<AuthResponse> RequestTokensAsync(string clientId, string clientSecret, string redirectUri, string code)
        {
            var parameters = GetBasicParameters(clientId, clientSecret);
            parameters.Add("grant_type", "authorization_code");
            parameters.Add("code", code);
            parameters.Add("redirect_uri", redirectUri);

            return GetTokensAsync(parameters);
        }

        /// <summary>
        /// This authentication method attempts to refresh the user's OAuth tokens using their refresh token.
        /// 
        /// For more information on this process see https://coinbase.com/docs/api/authentication#oauth2
        /// </summary>
        /// <param name="clientId">The application's id.</param>
        /// <param name="clientSecret">The application's secret.</param>
        /// <param name="refreshToken">The user's current refresh token.</param>
        /// <returns>A Task for an AuthResponse.</returns>
        public static Task<AuthResponse> RefreshTokensAsync(string clientId, string clientSecret, string refreshToken)
        {
            var parameters = GetBasicParameters(clientId, clientSecret);
            parameters.Add("grant_type", "refresh_token");
            parameters.Add("refresh_token", refreshToken);

            return GetTokensAsync(parameters);
        }
    }
}
