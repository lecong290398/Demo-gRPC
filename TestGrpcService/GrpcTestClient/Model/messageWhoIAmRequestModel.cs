using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GrpcTestClient.Model
{
    public class messageWhoIAmRequestModel
    {
        public string name { get; set; }

        public string Who { get; set; }

        public string Live { get; set; }
    }
}
