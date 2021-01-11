using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TestGrpcService.Model
{
    public class gRPC_TokenDto
    {
        public class RefreshDto
        {
            [Required]
            public string AccessToken { get; set; }

            [Required]
            public string RefreshToken { get; set; }
        }

        public class TokenDto
        {
            public string accessToken { get; set; }

            public string refreshToken { get; set; }

            public DateTime expiresRefreshToken { get; set; }
        }
    }
}
