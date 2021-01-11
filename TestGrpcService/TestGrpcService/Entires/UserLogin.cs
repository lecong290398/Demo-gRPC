using System;
using System.Collections.Generic;

#nullable disable

namespace TestGrpcService.Entires
{
    public partial class UserLogin
    {
        public string ID { get; set; }
        public string Fullname { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public DateTime? CreateDate { get; set; }
        public string ByCreate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string ByModify { get; set; }
    }
}
