﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RestSharp.Portable.Authenticators
{
    /// <summary>
    /// The OAuth 2 authenticator using the authorization request header field.
    /// </summary>
    /// <remarks>
    /// Based on http://tools.ietf.org/html/draft-ietf-oauth-v2-10#section-5.1.1
    /// </remarks>
    public class OAuth2AuthorizationRequestHeaderAuthenticator : OAuth2Authenticator
    {
        private readonly string _tokenType;
        private bool _authFailed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2AuthorizationRequestHeaderAuthenticator"/> class.
        /// </summary>
        /// <param name="client">The OAuth2 client</param>
        public OAuth2AuthorizationRequestHeaderAuthenticator(OAuth2.OAuth2Client client)
            : this(client, string.IsNullOrEmpty(client.TokenType) ? "OAuth" : client.TokenType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2AuthorizationRequestHeaderAuthenticator"/> class.
        /// </summary>
        /// <param name="client">The OAuth2 client</param>
        /// <param name="tokenType">
        /// The token type.
        /// </param>
        public OAuth2AuthorizationRequestHeaderAuthenticator(OAuth2.OAuth2Client client, string tokenType)
            : base(client)
        {
            _tokenType = tokenType;
        }

        /// <summary>
        /// Modifies the request to ensure that the authentication requirements are met.
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="header">Authentication/Authorization header</param>
        /// <param name="credential">Credential to be used for the authentication</param>
        /// <returns>The task the authentication is performed on</returns>
        public override async Task PreAuthenticate(IRestClient client, IRestRequest request, AuthHeader header, NetworkCredential credential)
        {
            // Only add the Authorization parameter if it hasn't been added and the authorization didn't fail previously
            var authParam = request.Parameters.LastOrDefault(p => p.Type == ParameterType.HttpHeader && p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
            if (!_authFailed && authParam != null)
                return;

            // When the authorization failed or when the Authorization header is missing, we're just adding it (again) with the
            // new AccessToken.
            _authFailed = false;
            var authValue = string.Format("{0} {1}", _tokenType, await Client.GetCurrentToken());
            if (authParam == null)
            {
                request.AddParameter("Authorization", authValue, ParameterType.HttpHeader);
            }
            else
            {
                authParam.Value = authValue;
            }
        }

        /// <summary>
        /// Will be called in response to an authentication request.
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="response">Response of the failed request</param>
        /// <param name="header">Authentication/Authorization header</param>
        /// <param name="credential">Credential to be used for the authentication</param>
        /// <returns>The task the authentication is performed on, which returns true when the authentication succeeded</returns>
        public override async Task<bool> Authenticate(
            IRestClient client,
            IRestRequest request,
            HttpResponseMessage response,
            AuthHeader header,
            NetworkCredential credential)
        {
            if (string.IsNullOrEmpty(Client.RefreshToken))
                return false;

            // Set this variable only if we have a refresh token
            _authFailed = true;
            await Client.GetCurrentToken(forceUpdate: true);

            return true;
        }
    }
}
