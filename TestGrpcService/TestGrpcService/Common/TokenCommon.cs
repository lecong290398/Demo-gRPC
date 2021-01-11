using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestGrpcService.Entires;
using TestGrpcService.Logic;
using TestGrpcService.Model;
using static TestGrpcService.Model.gRPC_TokenDto;

namespace TestGrpcService.Common
{
    public class TokenCommon
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly LogicBase _logicBase;

        //Contructer
        public TokenCommon(IConfiguration configuration, IMemoryCache cache,
           LogicBase logicBase)
        {
            _configuration = configuration;
            _cache = cache;
            _logicBase = logicBase;
        }

        private (string tokenId, string token, DateTime expires) GenerateJwtToken(in string username, in string device, UserLogin user)
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var time = _configuration.GetValue<int>("JwtExpireMinutes");

            var expires = _logicBase.GetDateTimeFromServer().AddMinutes(_configuration.GetValue<int>("JwtExpireMinutes"));

            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtIssuer"],
                listClaim,
                expires: expires,
                signingCredentials: creds
            );

            return (token.Id, new JwtSecurityTokenHandler().WriteToken(token), token.ValidTo);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = GetTokenValidationParameters(_configuration, false);

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

        public Task<TokenDto> GenerateTokenAsync(string username, string device, UserLogin user)
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

                var expiresRefreshToken = _logicBase.GetDateTimeFromServer().AddDays(_configuration.GetValue<int>("JwtRefreshExpireDays"));
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
            //UserId = listClaims.FirstOrDefault(h => h.Type == gRPCClaimTypes.Id)?.Value;
            return UserId;
        }
        public static string GetClaimsDevice(IEnumerable<Claim> listClaims)
        {
            return listClaims.FirstOrDefault(h => h.Type == gRPCClaimTypes.Device)?.Value;
        }

    }
}
