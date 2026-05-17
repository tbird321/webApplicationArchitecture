using System;
using System.Collections.Generic;
using System.Text;
namespace WebApplicationArch.model.responses
{
    [Serializable]
    public class loginResponse
    {
        public Boolean loggedIn { get; set; }
        public string authToken { get; set; }
        public string errorMessage { get; set; }
    }
}
