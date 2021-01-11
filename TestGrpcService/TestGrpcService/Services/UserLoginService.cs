using Grpc.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestGrpcService.Logic;
using TestGrpcService.Model;
using TestGrpcService.Protos;
using static TestGrpcService.Model.gRPC_TokenDto;

namespace TestGrpcService.Services
{
    public class UserLoginService : UserLogin.UserLoginBase
    {
        private readonly ILogger<UserLoginService> _logger;
        private readonly LoginLogic _logicLogin;
        private readonly LogicBase _logicBase;
        private readonly IConfiguration _config;
        private readonly IMemoryCache  _cache;

        public UserLoginService(ILogger<UserLoginService> logger, LoginLogic logicLogin, IConfiguration config, LogicBase logicBase, IMemoryCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logicLogin = logicLogin ?? throw new ArgumentNullException(nameof(logicLogin));
            _logicBase = logicBase ?? throw new ArgumentNullException(nameof(logicBase));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async override Task<messageLoginRely> LoginSrv(messageLoginRequest request, ServerCallContext context)
        {
            var result = await _logicLogin.Login(request);
            if (!string.IsNullOrEmpty(result.Id))
            {
                var token = await GenerateTokenAsync(request.Username, request.Device, new Entires.UserLogin
                {
                    ID = result.Id,
                    UserName = result.Username,
                    Fullname = result.Fullname
                }).ConfigureAwait(false);

                result.Tokens = new messageTokenRely
                {
                    AccessToken = token.accessToken,
                    ExpiresRefreshToken = ((DateTimeOffset)token.expiresRefreshToken).ToUnixTimeSeconds(),
                    RefreshToken = token.refreshToken
                };
            }
           
            return result;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async override Task<messageLogoutRely> LogoutSrv(messageLogoutRequest request, ServerCallContext context)
        {
            try
            {
                var userId = request.Username;
                var device = request.Device;

                var keyAccessToken = CachingHelpers.GetKeyAccessToken(userId, device);
                _cache.Remove(keyAccessToken);

                var keyRefressToken = CachingHelpers.GetKeyRefreshToken(userId, device);
                 _cache.Remove(keyRefressToken);
                var result = new messageLogoutRely { Username = request.Username, IsLogout = true };
                return result;
            }
            catch (Exception ex)
            {
                return new messageLogoutRely { Username = request.Username, IsLogout = false };
                throw ex;
            }

        }

        public async override Task<messageRefreshRequest> RefreshTokenSrv(messageTokenRely request, ServerCallContext context)
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Token-Invalid"), "Access");
            }

            var userId = GetClaimsUserId(principal.Claims);
            var device = GetClaimsDevice(principal.Claims);

            var valueRefreshToken = GetCacheRefreshTokenAsync( _cache, userId, device);

            if (valueRefreshToken != request.RefreshToken)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Token-Invalid"), "Refresh");
            }

            var token = await GenerateTokenAsync(userId, device, principal.Claims).ConfigureAwait(false);


            //Mapping
            var model = new messageRefreshRequest
            {
                AccessToken = token.accessToken,
                RefreshToken = token.refreshToken
            };

            return model;
        }


        #region TOKEN


        private (string tokenId, string token, DateTime expires) GenerateJwtToken(in string username, in string device, Entires.UserLogin user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, username), // Cho jwt token
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username), // cho cookie
                new Claim(gRPCClaimTypes.Id, user.ID),
                new Claim(gRPCClaimTypes.Device, $"{device}")
            };


            return GenerateJwtToken(claims);
        }
        private (string tokenId, string token, DateTime expires) GenerateJwtToken(IEnumerable<Claim> listClaim)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var time = _config.GetValue<int>("JwtExpireMinutes");

            var expires = _logicBase.GetDateTimeFromServer().AddMinutes(_config.GetValue<int>("JwtExpireMinutes"));

            var token = new JwtSecurityToken(
                _config["JwtIssuer"],
                _config["JwtIssuer"],
                listClaim,
                expires: expires,
                signingCredentials: creds
            );

            return (token.Id, new JwtSecurityTokenHandler().WriteToken(token), token.ValidTo);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = GetTokenValidationParameters(_config, false);

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (!(securityToken is JwtSecurityToken jwtSecurityToken)
                || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                //throw new SecurityTokenException("Invalid token");
                return null;
            }

            return principal;
        }

        public static string GetCacheRefreshTokenAsync(IMemoryCache cache, in string userId, in string device)
        {
            var key = CachingHelpers.GetKeyRefreshToken(userId, device);
            var asd = cache.Get<string>(key);
            return cache.Get<string>(key);
        }

        public static string GetCacheAccessTokenAsync(IMemoryCache cache, in string userId, in string device)
        {
            var key = CachingHelpers.GetKeyAccessToken(userId, device);
            return cache.Get<string>(key);
        }

        public Task<TokenDto> GenerateTokenAsync(string username, string device, Entires.UserLogin user)
        {
            var (accessTokenId, accessToken, expiresAccessToken) = GenerateJwtToken(username, device, user);

            return GenerateTokenAsync(
               accessTokenId, accessToken, expiresAccessToken,
               user.ID, device);
        }
        public Task<TokenDto> GenerateTokenAsync(string userId, string device, IEnumerable<Claim> listClaims)
        {
            var (accessTokenId, accessToken, expiresAccessToken) = GenerateJwtToken(listClaims);

            return GenerateTokenAsync(
                accessTokenId, accessToken, expiresAccessToken,
                userId, device);
        }
        public async Task<TokenDto> GenerateTokenAsync(
            string accessTokenId, string accessToken, DateTime expiresAccessToken,
            string userId, string device)
        {
            try
            {
                var keyAccess = CachingHelpers.GetKeyAccessToken(userId, device);

                 _cache.Set(keyAccess, accessTokenId, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expiresAccessToken

                });

                var randomNumber = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomNumber);
                var refreshToken = Convert.ToBase64String(randomNumber);

                var expiresRefreshToken = _logicBase.GetDateTimeFromServer().AddDays(_config.GetValue<int>("JwtRefreshExpireDays"));
                var keyRefresh = CachingHelpers.GetKeyRefreshToken(userId, device);

                 _cache.Set(keyRefresh, refreshToken, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expiresRefreshToken

                });

                return new TokenDto { accessToken = accessToken, refreshToken = refreshToken, expiresRefreshToken = expiresRefreshToken };
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public static TokenValidationParameters GetTokenValidationParameters(IConfiguration config, bool validateLifetime)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = config["JwtIssuer"],

                ValidateAudience = true,
                ValidAudience = config["JwtIssuer"],

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtKey"])),

                ValidateLifetime = validateLifetime
            };

            return tokenValidationParameters;
        }

        public static string GetClaimsUserId(IEnumerable<Claim> listClaims)
        {
            var UserId = "";
            UserId = listClaims.FirstOrDefault(h => h.Type == gRPCClaimTypes.Id)?.Value;
            return UserId;
        }
        public static string GetClaimsDevice(IEnumerable<Claim> listClaims)
        {
            return listClaims.FirstOrDefault(h => h.Type == gRPCClaimTypes.Device)?.Value;
        }

        #endregion
    }
}
