using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TrulyEmpWebService.Models;
using TrulyEmpWebService.Utils;
using Newtonsoft.Json;

namespace TrulyEmpWebService.Services
{
    public class DormSvr:BaseSvr
    {
        public DormInfoModel GetDormInfo(string cardNumber)
        {
            DormInfoModel model = new DormInfoModel();

            //住宿状态
            var dormInfo = db.GetEmpDormInfo(cardNumber).ToList();
            if (dormInfo.Count() > 0) {
                model.livingStatus = "在住";
                model.areaName = dormInfo.First().area;
                model.dormNumber = dormInfo.First().dorm_number;
                model.inDate = ((DateTime)dormInfo.First().in_date).ToString("yyyy-MM-dd");
            }
            else {
                model.livingStatus = "未住宿";
                model.areaName = "无";
                model.dormNumber = "无";
                model.inDate = "无";
            }

            var yearMonthArr = db.GetDormChargeMonth().Select(d=>d.year_month.Substring(0,4)+"-"+d.year_month.Substring(4)).ToArray();
            model.feeMonths = string.Join(",", yearMonthArr);

            return model;
        }

        public SimpleResultModel UserIsInDorm(string salaryNo)
        {
            var result = db.ValidateDormStatus(salaryNo).ToList();
            if (result.Count() > 0) {
                return new SimpleResultModel() { suc = result.First().suc == 1 ? true : false, msg = result.First().msg };
            }
            return new SimpleResultModel() { suc = false, msg = "查询失败" };
        }

        public SimpleResultModel GetDormFee(string salaryNo, string yearMonth)
        {
            var fees = db.GetDormFeeByMonth(yearMonth.Replace("-", ""), salaryNo).ToList();
            if (fees.Count() < 1) {
                return new SimpleResultModel() { suc = false, msg = "查询不到相关信息" };
            }
            DormFeeModel model = new DormFeeModel();
            model.yearMonth = yearMonth;
            foreach (var fee in fees) {
                model.dormNumber += "  " + fee.dorm_number;
                model.rent += "  " + fee.rent;
                model.management += "  " + fee.management;
                model.elec += "  " + fee.electricity;
                model.water += "  " + fee.water;
                model.fine += "  " + fee.fine;
                model.repair += "  " + fee.repair;
                model.others += "  " + fee.others;
                model.comment += "  " + fee.comment;
            }
            model.total = fees.Sum(f => f.total).ToString()+"(元)";
            
            return new SimpleResultModel() { suc = true, extra = JsonConvert.SerializeObject(model) };
        }

    }
}