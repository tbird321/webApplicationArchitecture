using System;
using System.Collections.Generic;
using System.Text;
namespace WebApplicationArch.model.requests
{
    [Serializable]
    public class loginRequest
    {
        public string username { get; set; }
        public string password { get; set; }
    }
    
}
