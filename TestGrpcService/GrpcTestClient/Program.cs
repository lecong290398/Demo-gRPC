using Grpc.Net.Client;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json;
using GrpcTestClient.Model;
using System.Collections.Generic;
using Grpc.Core;

namespace GrpcTestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // The port number(5001) must match the port of the gRPC server.
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new UserLogin.UserLoginClient(channel);
            var rely = await client.LoginSrvAsync(new messageLoginRequest { Username = "admin", Password = "123",Device = "Web" });
            Console.WriteLine("Đăng nhập thành công:____" + rely.Fullname);

            var headers = new Metadata();
            headers.Add("Authorization", $"Bearer {rely.Tokens.AccessToken}");
            var relyLogout = await client.LogoutSrvAsync(new messageLogoutRequest { Device = "Web", Username = rely.Id }, headers);
            Console.WriteLine("Logout thành công..." + relyLogout.Username);


            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
