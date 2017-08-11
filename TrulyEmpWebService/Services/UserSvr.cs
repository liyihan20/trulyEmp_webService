using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TrulyEmpWebService.Models;
using TrulyEmpWebService.Utils;

namespace TrulyEmpWebService.Services
{
    public class UserSvr:BaseSvr
    {
        
        public string UserLogin(string userName, string password)
        {
            int maxTimes = 5;
            DateTime sixMonthAgo = DateTime.Now.AddDays(-180);
            var users = db.ei_users.Where(u => u.card_number == userName).ToList();
            if (users.Count() < 1) {
                return "用户名不存在";
            }
            var user = users.First();
            if (user.forbit_flag == true) {
                ForbitUser(user);
                return "用户已被禁用";
            }
            var hrStatus = db.GetHREmpStatus(userName).ToList();
            if (hrStatus.Count() > 0 && hrStatus.First().state != "在职") {

                return "你在人事系统不是在职状态，已被禁用";
            }
            if (user.last_login_date < sixMonthAgo) {
                ForbitUser(user);
                return "用户超过6个月未登陆，已被禁用";
            }
            if (user.password == MyUtils.getMD5(password)) {
                user.last_login_date = DateTime.Now;
                user.fail_times = 0;
                db.SubmitChanges();
                return "";
            }
            else {
                int failTimes = user.fail_times ?? 0;
                failTimes++;
                if (failTimes >= maxTimes) {
                    ForbitUser(user);
                    return "连续输错密码达到"+maxTimes+"次，用户被禁用";
                }
                else {
                    user.fail_times = failTimes;
                    db.SubmitChanges();
                    return "已连续" + failTimes + "次密码错误，你还剩下" + (maxTimes - failTimes) + "次尝试机会。";
                }
            }
        }

        public string GetUserName(int userId)
        {
            return db.ei_users.Single(u => u.id == userId).name;
        }

        public int GetUserId(string userName)
        {
            return db.ei_users.Single(u => u.card_number == userName).id;
        }

        public UserModel GetUserInfo(string userName)
        {
            ei_users user = db.ei_users.Single(u => u.card_number == userName);
            return new UserModel()
            {
                userId = user.id,
                userName = user.name,
                cardNumber = user.card_number,
                email = user.email,
                idNumber = user.id_number,
                sex = user.sex,
                phoneNumber = user.phone,
                shortPhoneNumber = user.short_phone,
                salaryNumber = user.salary_no,
                md5Password=user.password
            };
        }

        public bool isCardNumberExisted(string cardNumber)
        {
            return db.ei_users.Where(u => u.card_number == cardNumber).Count() == 1;
        }

        private void ForbitUser(ei_users user)
        {
            user.forbit_flag = true;
            user.fail_times = 0;
            db.SubmitChanges();
        }

        public void ResetPassword(string cardNumber, string password)
        {
            password = MyUtils.getMD5(password);
            var user = db.ei_users.Single(u => u.card_number == cardNumber);
            user.password = password;
            db.SubmitChanges();
        }

        public SimpleResultModel RegisterStepFirst(string cardNumber, string userName, string idNumber)
        {
            SimpleResultModel result = new SimpleResultModel();
            result.suc = false;

            if (isCardNumberExisted(cardNumber)) {
                result.msg = "你的厂牌号已经注册过，请直接登陆";
            }
            else 
            if (db.ei_specialUsers.Where(u => u.card_no == cardNumber).Count() > 0) {
                result.msg = "你的用户处于特殊保护状态，如要注册请联系管理员";
            }
            else {
                var emps = db.GetHREmpInfo(cardNumber).ToList();
                if (emps.Count() < 1) {
                    result.msg = "你的厂牌在人事系统不存在";
                }
                else {
                    var emp = emps.First();
                    if (!userName.Equals(emp.emp_name)) {
                        result.msg = "姓名和厂牌不匹配";
                    }
                    else if (!emp.id_code.EndsWith(idNumber)) {
                        result.msg = "身份证号后六位不正确";
                    }
                    else if (string.IsNullOrEmpty(emp.email) && string.IsNullOrEmpty(emp.tp)) {
                        result.msg = "你在人事系统中没有登记邮箱或者手机号码，不能注册";
                    }
                    else {
                        result.suc = true;
                        result.msg = "第一步验证成功";
                        result.extra = JsonConvert.SerializeObject(new { email = emp.email, phone = emp.tp });
                    }
                }
            }

            return result;

        }

        public SimpleResultModel FinishRegister(string cardNumber)
        {
            if (db.ei_users.Where(u => u.card_number == cardNumber).Count() > 0) {
                return new SimpleResultModel() { suc = false, msg = "该用户已经注册，不能重复注册" };
            }

            try {
                var empInfo = db.GetHREmpInfo(cardNumber).First();                
                ei_users user = new ei_users()
                {
                    card_number = cardNumber,
                    name = empInfo.emp_name,
                    email = empInfo.email,
                    id_number = empInfo.id_code,
                    sex = empInfo.sex,
                    phone = empInfo.tp,
                    short_portrait = empInfo.zp == null ? null : MyUtils.MakeThumbnail(MyUtils.BytesToImage(empInfo.zp.ToArray())),
                    salary_no = empInfo.txm,
                    create_date = DateTime.Now,
                    fail_times = 0,
                    forbit_flag = false,
                    password = MyUtils.getMD5("000000")
                };
                db.ei_users.InsertOnSubmit(user);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return new SimpleResultModel() { suc = false, msg = "注册失败:" + ex.Message };
            }
            return new SimpleResultModel() { suc = true };
        }

        public string GetUserEmail(string cardNumber)
        {
            return db.ei_users.Single(u => u.card_number == cardNumber).email;
        }

        public string GetUserPhone(string cardNumber)
        {
            return db.ei_users.Single(u => u.card_number == cardNumber).phone;
        }

        public string GetLast6IdNumber(string cardNumber)
        {
            string idNumber = db.ei_users.Single(u => u.card_number == cardNumber).id_number;
            return idNumber.Substring(idNumber.Length - 6);
        }

        public bool ActivateUser(string cardNumber)
        {
            var users = db.ei_users.Where(u => u.card_number == cardNumber);
            if (users.Count() > 0) {
                var user = users.First();
                user.fail_times = 0;
                user.forbit_flag = false;
                user.last_login_date = DateTime.Now;
                db.SubmitChanges();
                return true;
            }
            return false;
        }

        public string UpdateUserInfo(int userId, string email, string phone, string shortPhone, string newPassword)
        {
            ei_users user = db.ei_users.Single(u => u.id == userId);
            if (!string.IsNullOrEmpty(email)) {
                if (db.ei_users.Where(u => u.email == email && u.id != userId && u.name != user.name).Count() > 0) {
                    return "此邮箱地址已被其他人注册";                    
                }
                user.email = email;
            }
            if (!string.IsNullOrEmpty(phone)) {
                if (db.ei_users.Where(u => u.phone == phone && u.id != userId && u.name != user.name).Count() > 0) {
                    return "此手机长号已被其他人注册";
                }
                user.phone = phone;
            }
            if (!string.IsNullOrEmpty(shortPhone)) {
                user.short_phone = shortPhone;
            }
            if (!string.IsNullOrEmpty(newPassword)) {
                string validateInfo = MyUtils.ValidatePassword(newPassword);
                if (!string.IsNullOrEmpty(validateInfo)) {
                    return validateInfo;
                }
                user.password = MyUtils.getMD5(newPassword);
            }
            db.SubmitChanges();
            return "";
        }

        public string WhoElseHaveMyPhoneNumber(string cardNumber, string phone)
        {
            string names = String.Join(",", (from u in db.ei_users
                                             from ou in db.ei_users
                                             where u.name != ou.name
                                             && u.card_number == cardNumber
                                             && u.phone == ou.phone
                                             select ou.name).ToArray());
            return names;

        }

    }
}