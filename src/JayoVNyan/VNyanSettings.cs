using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace JayoVNyan
{
    public class VNyanSettings
    {
        public bool Enabled
        {
            get;
            set;
        }

        public string Address
        {
            get;
            set;
        }

        public VNyanSettings()
        {
            Enabled = false;
            Address = "ws://localhost:8069/vnyan";
        }
    }
}
