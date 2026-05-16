using APS_Automation_Server.Models;
using Autodesk.Authentication;
using Autodesk.Authentication.Model;
using Microsoft.AspNetCore.Authentication.OAuth;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using System.Collections.Generic;

namespace APS_Automation_Server.Services
{
    public record TokenResponse(string token_type, string access_token, DateTime expires_in);

    public record ThreeLeggedTokenRespons(string public_token, string internal_Token, string refresh_Token, DateTime expires_at);

    public class ApsAuthService
    {

        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _callbackUri;
        private TokenResponse? _internalTokenCache;
        private TokenResponse? _publicTokenCache;



        private readonly List<Scopes> InternalTokenScopes=new List<Scopes>
        {
            Scopes.DataRead,
            Scopes.AccountRead,
            Scopes.AccountWrite,
            Scopes.UserProfileRead,
            Scopes.ViewablesRead,
            Scopes.BucketCreate,
            Scopes.BucketRead,
            Scopes.DataWrite,
            Scopes.DataCreate
        };

        private readonly List<Scopes> PublicTokenScopes = new List<Scopes> { Scopes.ViewablesRead };

        public ApsAuthService(string clientId, string clientSecret, string callbackUri)
        {

            _clientId = clientId;
            _clientSecret = clientSecret;
            _callbackUri = callbackUri;
        }

        public string GetAuthorizationURL()
        {
            var authenticationClient = new AuthenticationClient();
            return authenticationClient.Authorize(_clientId, ResponseType.Code, _callbackUri, InternalTokenScopes);
        }
        private async Task<TokenResponse> GetToken(List<Scopes> scopes)
        {
            var authenticationClient = new AuthenticationClient();
            var auth = await authenticationClient.GetTwoLeggedTokenAsync(_clientId, _clientSecret, scopes);
            return new TokenResponse(auth.TokenType, auth.AccessToken, DateTime.UtcNow.AddSeconds((double)auth.ExpiresIn));
        }

        public async Task<ThreeLeggedTokenRespons> GenerateThreeLeggedTokens(string code)
        {
            var authenticationClient = new AuthenticationClient();
            var internalAuth = await authenticationClient.GetThreeLeggedTokenAsync(_clientId, code, _callbackUri, clientSecret: _clientSecret);
            var publicAuth = await authenticationClient.RefreshTokenAsync(internalAuth.RefreshToken, _clientId, clientSecret: _clientSecret, scopes: PublicTokenScopes);
            return new ThreeLeggedTokenRespons
            (
                 publicAuth.AccessToken,
                 internalAuth.AccessToken,
                 publicAuth.RefreshToken,
                 DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
            );
        }

        public async Task<ThreeLeggedTokenRespons> RefreshTokens(ThreeLeggedTokenRespons tokens)
        {
            var authenticationClient = new AuthenticationClient();

            var auth = await authenticationClient.RefreshTokenAsync(
                tokens.refresh_Token,
                _clientId,
                clientSecret: _clientSecret
            );

            return new ThreeLeggedTokenRespons
            (
                auth.AccessToken,
                auth.AccessToken,
                auth.RefreshToken,
                DateTime.UtcNow.AddSeconds((double)auth.ExpiresIn)
            );
        }

        public async Task<UserInfo> GetUserProfile(ThreeLeggedTokenRespons tokens)
        {
            var authenticationClient = new AuthenticationClient();
            UserInfo userInfo = await authenticationClient.GetUserInfoAsync(tokens.internal_Token);
            return userInfo;
        }

        public async Task<TokenResponse> GetPublicToken()
        {
            if (_publicTokenCache == null || _publicTokenCache.expires_in < DateTime.UtcNow)
                _publicTokenCache = await GetToken(PublicTokenScopes);
            return _publicTokenCache;
        }
        public async Task<TokenResponse> GetInternalToken()
        {
            if (_internalTokenCache == null || _internalTokenCache.expires_in < DateTime.UtcNow)
                _internalTokenCache = await GetToken(InternalTokenScopes);
            return _internalTokenCache;
        }

        public async Task<ThreeLeggedTokenRespons> PrepareTokens(HttpRequest request, HttpResponse response, ApsAuthService aps)
        {
         
            if (!request.Cookies.ContainsKey("internal_token"))
            {
                return null;
            }
            var tokens = new ThreeLeggedTokenRespons
            (
                request.Cookies["public_token"],
                request.Cookies["internal_token"],
                request.Cookies["refresh_token"],
                DateTime.Parse(request.Cookies["expires_at"])
            );
            if (tokens.expires_at < DateTime.Now.ToUniversalTime())
            {
                tokens = await aps.RefreshTokens(tokens);
                response.Cookies.Append("public_token", tokens.public_token);
                response.Cookies.Append("internal_token", tokens.internal_Token);
                response.Cookies.Append("refresh_token", tokens.refresh_Token);
                response.Cookies.Append("expires_at", tokens.expires_at.ToString());
            }
            
            return tokens;
        }


    }
}
