using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplicationArch.model.responses
{
    [Serializable]
    public class defaultResponse
    {
        public Boolean status { get; set; }
        public string errorMessage { get; set; }
    }
}
