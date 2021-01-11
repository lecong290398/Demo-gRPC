using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestGrpcService.Context;
using TestGrpcService.Protos;

namespace TestGrpcService.Logic
{
    public class LoginLogic : LogicBase
    {
        public LoginLogic(DemoGRPCContext context, IConfiguration config) : base(context, config)
        {
           
        }

        public async Task<messageLoginRely> Login(messageLoginRequest input)
        {
            try
            {
                var result = await _context.UserLogins.FirstOrDefaultAsync(c => c.UserName == input.Username && c.Password == input.Password).ConfigureAwait(false);
                if (result != null)
                {

                    var model = new messageLoginRely
                    {
                        Fullname = result.Fullname,
                        Id = result.ID,
                        Username = result.UserName
                    };
                    return model;
                }
                else
                {
                    return new messageLoginRely();
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
