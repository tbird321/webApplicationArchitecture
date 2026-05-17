using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplicationArch.model
{
    [Serializable]
    public class userList
    {
        public userList()
        {
            users = new List<userInfo>();
        }
        public List<userInfo> users { get; set; }
    }
}
