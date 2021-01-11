using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestGrpcService.Model
{
    public class CachingHelpers
    {
        public static string GetKeyAccessToken(in string userId, in string device)
        {
            var upperUserId = $"{userId}".ToUpperInvariant().Trim();
            var upperDevice = $"{device}".ToUpperInvariant().Trim();
            return $"USER:{upperUserId}:{upperDevice}:TOKEN:ACCESS";
        }

        public static string GetKeyRefreshToken(in string userId, in string device)
        {
            var upperUserId = $"{userId}".ToUpperInvariant().Trim();
            var upperDevice = $"{device}".ToUpperInvariant().Trim();
            return $"USER:{upperUserId}:{upperDevice}:TOKEN:REFRESH";
        }
    }
}
