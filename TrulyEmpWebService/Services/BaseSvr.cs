using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TrulyEmpWebService.Models;

namespace TrulyEmpWebService.Services
{
    public class BaseSvr
    {
        protected EmpDBDataContext db = new EmpDBDataContext();

        public void WriteEventLog(string tag,string doWhat)
        {            
            db.ei_event_log_android.InsertOnSubmit(new ei_event_log_android()
            {
                do_what = doWhat,
                op_date = DateTime.Now,
                model = tag
            });
            db.SubmitChanges();
        }

    }
}