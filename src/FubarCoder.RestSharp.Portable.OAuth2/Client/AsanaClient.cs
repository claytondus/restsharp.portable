﻿using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using RestSharp.Portable.OAuth2.Configuration;
using RestSharp.Portable.OAuth2.Infrastructure;
using RestSharp.Portable.OAuth2.Models;

namespace RestSharp.Portable.OAuth2.Client
{
    /// <summary>
    /// Asana authentication client.
    /// </summary>
    public class AsanaClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsanaClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public AsanaClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Defines URI of service which issues access code.
        /// </summary>
        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://app.asana.com", 
                    Resource = "/-/oauth_authorize"
                };
            }
        }

        /// <summary>
        /// Defines URI of service which issues access token.
        /// </summary>
        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://app.asana.com", 
                    Resource = "/-/oauth_token"
                };
            }
        }

        /// <summary>
        /// Defines URI of service which allows to obtain information about user which is currently logged in.
        /// </summary>
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://app.asana.com", 
                    Resource = "/api/1.0/users/me"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        /// <param name="args">
        /// </param>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.AddOrUpdateParameter("opt_fields", "id,name,photo.image_128x128,photo.image_60x60,photo.image_36x36,email");
            args.Client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(this, "Bearer");
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="response">The response which is received from the provider.</param>
        /// <returns>
        /// </returns>
        protected override UserInfo ParseUserInfo(IRestResponse response)
        {
            var info = JObject.Parse(response.Content);
            JToken dataExists;
            if (!info.TryGetValue("data", out dataExists))
                return new UserInfo();

            // const string avatarUriTemplate = "{0}?type={1}";
            var avatarSmallUri = info["data"]["photo"]["image_36x36"].Value<string>();
            var avatarNormalUri = info["data"]["photo"]["image_60x60"].Value<string>();
            var avatarLargeUri = info["data"]["photo"]["image_128x128"].Value<string>();
            var splitName = new List<string>(info["data"]["name"].Value<string>().Split(' '));
            var firstName = splitName.FirstOrDefault();
            splitName.RemoveAt(0);
            var lastName = splitName.Join(" ");

            return new UserInfo
            {
                Id = info["data"]["id"].Value<string>(), 
                FirstName = firstName, 
                LastName = lastName, 
                Email = info["data"]["email"].SafeGet(x => x.Value<string>()), 
                AvatarUri =
                {
                    Small = !string.IsNullOrWhiteSpace(avatarSmallUri) ? avatarSmallUri : string.Empty, 
                    Normal = !string.IsNullOrWhiteSpace(avatarNormalUri) ? avatarNormalUri : string.Empty, 
                    Large = !string.IsNullOrWhiteSpace(avatarLargeUri) ? avatarLargeUri : string.Empty
                }
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Asana"; }
        }
    }
}