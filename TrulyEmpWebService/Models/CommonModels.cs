using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrulyEmpWebService.Models
{
    public class SimpleResultModel
    {
        public bool suc { get; set; }
        public string msg { get; set; }
        public string extra { get; set; }
    }

    public class ParamsModel
    {
        public int arg0 { get; set; }
        public string arg1 { get; set; }
        public string arg2 { get; set; }
        public string arg3 { get; set; }
        public string arg4 { get; set; }
        public string arg5 { get; set; }
    }

    public class UpdateModel
    {
        public int versionCode { get; set; }
        public string apkUrl { get; set; }
        public bool forceUpdate { get; set; }
    }

}