using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplicationArch.model
{
    [Serializable]
    public class userInfo
    {
        public string username { get; set; }
        public string passwordHash { get; set; }
        public string passwordSalt { get; set; }
    }

}
