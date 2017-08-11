using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrulyEmpWebService.Models
{
    public class DinnerInfoModels
    {
        public string cardStatus { get; set; }
        public string sumRemain { get; set; }
        public string lastConsumeTime { get; set; }
    }

    public class DinnerBindingModel
    {
        public string status { get; set; }
        public string payPassword { get; set; }
        public string limit { get; set; }
    }

    public class ConsumeRecordModel
    {
        public string consumeTime { get; set; }
        public string diningType { get; set; }
        public string consumeMoney { get; set; }
        public string place { get; set; }
    }

    public class RechargeRecordModel
    {
        public string rechargeSum { get; set; }
        public string rechargeTime { get; set; }
        public string place { get; set; }
        public string beforeSum { get; set; }
        public string afterSum { get; set; }
    }
}