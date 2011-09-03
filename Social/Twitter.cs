﻿namespace RoliSoft.TVShowTracker.Social
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Authentication;

    using Hammock;
    using Hammock.Authentication.OAuth;
    using Hammock.Web;

    /// <summary>
    /// Provides support for posting status updates to Twitter.
    /// </summary>
    public static class Twitter
    {
        /// <summary>
        /// The consumer key of the application.
        /// </summary>
        public static string ConsumerKey = "4e8qhi2hiCQXO84kijKBg";

        /// <summary>
        /// The consumer secret of the application.
        /// </summary>
        public static string ConsumerSecret = "AoUTWWKkVnHALa00M1TEoSzvIHaaWN18MKvqqX2Tiic";

        /// <summary>
        /// The default status format.
        /// </summary>
        public static string DefaultStatusFormat = "Watching $show S$seasonE$episode - $title";

        private static string _tempAuthToken;
        private static string _tempAuthTokenSecret;
        private static RestClient _restClient;

        /// <summary>
        /// Initializes the <see cref="Twitter"/> class.
        /// </summary>
        static Twitter()
        {
            ServicePointManager.Expect100Continue = false;

            _restClient = new RestClient
                {
                    QueryHandling        = QueryHandling.AppendToParameters,
                    DecompressionMethods = DecompressionMethods.GZip,
                    UserAgent            = Signature.Software + "/" + Signature.Version,
                    FollowRedirects      = true,
                };
        }

        /// <summary>
        /// Checks whether the software has authorization to use Twitter.
        /// </summary>
        /// <returns></returns>
        public static bool OAuthTokensAvailable()
        {
            return Settings.Get("Twitter OAuth", new List<string>()).Count == 4;
        }

        /// <summary>
        /// Generates an URL which will be opened in the users web browser to authorize the application.
        /// </summary>
        /// <returns>
        /// Authorization URL.
        /// </returns>
        public static string GenerateAuthorizationLink()
        {
            _restClient.Authority   = "https://api.twitter.com/oauth";
            _restClient.Credentials = new OAuthCredentials
                {
                    Type              = OAuthType.RequestToken,
                    SignatureMethod   = OAuthSignatureMethod.HmacSha1,
                    ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                    ConsumerKey       = ConsumerKey,
                    ConsumerSecret    = ConsumerSecret,
                    CallbackUrl       = "oob",
                };

            var response = _restClient.Request(new RestRequest { Path = "/request_token" });
            var parsed   = Utils.ParseQueryString(response.Content);

            if (!parsed.TryGetValue("oauth_token", out _tempAuthToken)
             || !parsed.TryGetValue("oauth_token_secret", out _tempAuthTokenSecret))
            {
                throw new Exception("Invalid response from server. (No tokens were returned.)");
            }

            return "https://api.twitter.com/oauth/authorize?oauth_token=" + _tempAuthToken;
        }

        /// <summary>
        /// Finishes the authorization by using the user-specified PIN and saves the token to the settings.
        /// </summary>
        /// <param name="pin">The PIN.</param>
        public static void FinishAuthorizationWithPin(string pin)
        {
            _restClient.Authority   = "https://api.twitter.com/oauth";
            _restClient.Credentials = new OAuthCredentials
                {
                    Type              = OAuthType.AccessToken,
                    SignatureMethod   = OAuthSignatureMethod.HmacSha1,
                    ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                    ConsumerKey       = ConsumerKey,
                    ConsumerSecret    = ConsumerSecret,
                    Token             = _tempAuthToken,
                    TokenSecret       = _tempAuthTokenSecret,
                    Verifier          = pin
                };

            var response = _restClient.Request(new RestRequest { Path = "/access_token" });
            var parsed   = Utils.ParseQueryString(response.Content);

            if (!parsed.ContainsKey("oauth_token")
             || !parsed.ContainsKey("oauth_token_secret"))
            {
                throw new Exception("Invalid response from server. (No tokens were returned.)");
            }

            Settings.Set("Twitter OAuth", new List<string>
                {
                    parsed["user_id"],
                    parsed["screen_name"],
                    parsed["oauth_token"],
                    parsed["oauth_token_secret"]
                });
        }

        /// <summary>
        /// Posts the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Server response.
        /// </returns>
        public static void PostMessage(string message)
        {
            var oauth = Settings.Get<List<string>>("Twitter OAuth");
            if (oauth == null || oauth.Count != 4)
            {
                throw new InvalidCredentialException();
            }

            _restClient.Authority   = "http://api.twitter.com";
            _restClient.Credentials = new OAuthCredentials
                {
                    Type              = OAuthType.ProtectedResource,
                    SignatureMethod   = OAuthSignatureMethod.HmacSha1,
                    ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                    ConsumerKey       = ConsumerKey,
                    ConsumerSecret    = ConsumerSecret,
                    Token             = oauth[2],
                    TokenSecret       = oauth[3],
                };

            var request = new RestRequest
                {
                    Path   = "/statuses/update.json",
                    Method = WebMethod.Post
                };

            request.AddParameter("status", message);

            _restClient.Request(request);
        }
    }
}