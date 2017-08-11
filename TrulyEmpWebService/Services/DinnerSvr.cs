using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TrulyEmpWebService.Models;
using TrulyEmpWebService.Utils;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace TrulyEmpWebService.Services
{
    public class DinnerSvr
    {
        private CanteenDBDataContext db;

        public DinnerSvr()
        {
            db = new CanteenDBDataContext();
        }

        public DinnerInfoModels GetDinnerInfo(string cardNumber)
        {
            var infos = db.ljq20160323_002(cardNumber).ToList();
            if (infos.Count() < 1) {
                return new DinnerInfoModels()
                {
                    cardStatus = "不存在",
                    sumRemain = "0",
                    lastConsumeTime = "无"
                };
            }
            else {
                var info = infos.First();
                return new DinnerInfoModels()
                {
                    cardStatus = info.FStatus,
                    sumRemain = ((Decimal)info.MonBalance).ToString("0.0###"),
                    lastConsumeTime = info.DatLastConsumeTime == null ? "无" : ((DateTime)info.DatLastConsumeTime).ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
        }

        public string LockOrUnLock(string cardNumber, string lockCode)
        {
            try {
                db.ljq20161019(cardNumber, lockCode);
            }
            catch (Exception ex) {
                return "操作失败,原因：" + ex.InnerException.Message;
            }
            return "";
        }

        public SimpleResultModel GetDinnerCardBinding(string cardNumber)
        {
            var bindings = db.t_UserConfig.Where(t => t.FNumber == cardNumber);
            if (bindings.Count() < 1) {
                return new SimpleResultModel() { suc = false, msg = "没有绑卡信息" };
            }
            var binding = bindings.First();
            DinnerBindingModel model = new DinnerBindingModel()
            {
                status = binding.FStatus,
                payPassword = "之前设定密码",
                limit = ((decimal)binding.FQuota).ToString("0.#")
            };
            return new SimpleResultModel() { suc = true, extra = JsonConvert.SerializeObject(model) };
        }

        public string SaveDinnerCardBinding(string cardNumber, string canConsume, string payPassword, int maxLimit)
        {
            string phone = new UserSvr().GetUserPhone(cardNumber);
            if (string.IsNullOrEmpty(phone)) {
                return "保存设定需要先绑定手机长号，请在主界面点击头像设置手机长号" ;
            }
            else {
                string otherHaveMyPhone = new UserSvr().WhoElseHaveMyPhoneNumber(cardNumber, phone);
                if (!string.IsNullOrEmpty(otherHaveMyPhone)) {
                    return "你的手机长号与[" + otherHaveMyPhone + "]重复，保存失败。";
                }
            }
            var passwordRegx = new Regex(@"^\d{6}$");
            var hasBindingRecord = db.t_UserConfig.Where(t => t.FNumber == cardNumber);
            if (hasBindingRecord.Count() > 0) {
                var record = hasBindingRecord.First();
                if (!"之前设定密码".Equals(payPassword)) {
                    if (!passwordRegx.IsMatch(payPassword)) {
                        return "支付密码必须为6位数字";
                    }
                    else {
                        record.FPassword = MyUtils.getNormalMD5(payPassword);
                    }
                }
                record.FPhone = phone;
                record.FStatus = canConsume.Equals("1") ? "1" : "0";
                record.FQuota = maxLimit;
            }
            else {
                if (!passwordRegx.IsMatch(payPassword)) {
                    return "支付密码必须为6位数字";
                }
                t_UserConfig binding = new t_UserConfig();
                binding.FNumber = cardNumber;
                binding.FPassword = MyUtils.getNormalMD5(payPassword);
                binding.FPhone = phone;
                binding.FStatus = canConsume.Equals("1") ? "1" : "0";
                binding.FQuota = maxLimit;

                db.t_UserConfig.InsertOnSubmit(binding);
            }

            try {
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return "服务器错误，保存失败，请联系管理员:"+ex.Message;
            }

            return "";
        }

        public SimpleResultModel GetConsumeRecords(string cardNumber, string fromDate, string toDate)
        {
            if (DateTime.Parse(fromDate) < DateTime.Now.AddDays(-31)) {
                return new SimpleResultModel() { suc = false, msg = "哈哈~你以为改了手机时间就可以查一个月之前的消费记录吗？\nToo Young Too Simple" };
            }

            var records = db.ljq20160323_001(cardNumber, fromDate, toDate).ToList();
            if (records.Count() < 1) {
                return new SimpleResultModel() { suc = false, msg = "此时间段查询不到相关记录" };
            }
            List<ConsumeRecordModel> list = new List<ConsumeRecordModel>();
            foreach (var r in records.OrderByDescending(r => r.消费时间)) {
                list.Add(new ConsumeRecordModel()
                {
                    consumeTime = r.消费时间.ToString("yyyy-MM-dd HH:mm"),
                    consumeMoney = ((decimal)r.消费金额).ToString("0.0"),
                    diningType = r.餐别,
                    place = r.消费场所
                });
            }

            return new SimpleResultModel() { suc = true,msg="成功加载记录数："+list.Count(), extra = JsonConvert.SerializeObject(list) };
        }

        public SimpleResultModel GetRechargeRecords(string cardNumber, string fromDate, string toDate)
        {
            if (DateTime.Parse(fromDate) < DateTime.Now.AddDays(-31 * 6)) {
                return new SimpleResultModel() { suc = false, msg = "哈哈~你以为改了手机时间就可以查六个月之前的消费记录吗？\nToo Young Too Simple" };
            }
            var records = db.ljq20160323_003(cardNumber, fromDate, toDate).ToList();
            if (records.Count() < 1) {                
                return new SimpleResultModel() { suc = false, msg = "此时间段查询不到相关记录" };
            }
            List<RechargeRecordModel> list = new List<RechargeRecordModel>();
            foreach (var r in records.OrderByDescending(r => r.充值时间)) {
                list.Add(new RechargeRecordModel()
                {
                    beforeSum = ((decimal)r.充值前金额).ToString("0.0"),
                    afterSum = ((decimal)r.充值后金额).ToString("0.0"),
                    rechargeSum = ((decimal)r.充值金额).ToString("0.0"),
                    rechargeTime = r.充值时间.ToString("yyyy-MM-dd HH:mm"),
                    place = r.充值场所
                });
            }
            return new SimpleResultModel { suc = true, msg = "成功加载记录数:" + list.Count(), extra = JsonConvert.SerializeObject(list) };
        }

    }
}