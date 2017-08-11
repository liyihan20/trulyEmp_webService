using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrulyEmpWebService.Models
{
    public class LoginUserModel
    {
        public string userName { get; set; }
        public string password { get; set; }
        public string imei { get; set; }
    }
    public class UserModel
    {
        public int userId { get; set; }
        public String userName { get; set; }
        public String cardNumber { get; set; }
        public String email { get; set; }
        public String sex { get; set; }
        public String idNumber { get; set; }
        public String salaryNumber { get; set; }
        public String phoneNumber { get; set; }
        public String shortPhoneNumber { get; set; }
        public String md5Password { get; set; }
    }
}