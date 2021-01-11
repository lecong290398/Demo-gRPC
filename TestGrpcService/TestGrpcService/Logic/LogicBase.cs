using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TestGrpcService.Context;

namespace TestGrpcService.Logic
{
    public class LogicBase
    {
        protected readonly DemoGRPCContext _context;
        public readonly IConfiguration _config;

        public LogicBase(DemoGRPCContext context, IConfiguration config)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public  DateTime GetDateTimeFromServer()
        {
            return Convert.ToDateTime(DateTime.Now);
        }
    }
}
