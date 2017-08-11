using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using cn.jpush.api;
using cn.jpush.api.push.mode;
using cn.jpush.api.push.notification;
using cn.jpush.api.common;
using cn.jpush.api.common.resp;
using TrulyEmpWebService.Models;
using System.Text.RegularExpressions;
using System.Drawing;

namespace TrulyEmpWebService.Utils
{
    public class MyUtils
    {

        //DES的密钥
        private static string DES_key = "19871219";
        private static string DES_iv = "19890610";

        //AES设计有三个密钥长度:128,192,256位
        private static string AES_128_key = "1987121919890610";
        //private static string AES_192_key = "198712191989061019871219";
        //private static string AES_256_key = "19871219198906101987121919890610";

        public static string getNormalMD5(string str)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = Encoding.Default.GetBytes(str);
            byte[] result = md5.ComputeHash(data);
            String ret = "";
            for (int i = 0; i < result.Length; i++) {
                ret += result[i].ToString("x").PadLeft(2, '0');
            }
            return ret;
        }

        //自定义MD5加密
        public static string getMD5(string str)
        {
            if (str.Length > 2) {
                str = "Who" + str.Substring(2) + "Are" + str.Substring(0, 2) + "You";
            }
            else {
                str = "Who" + str + "Are" + str + "You";
            }
            return getNormalMD5(str);
        }

        //DES加密
        public static string DESEncrypt(string sourceString)
        {
            try {
                byte[] btKey = Encoding.UTF8.GetBytes(DES_key);

                byte[] btIV = Encoding.UTF8.GetBytes(DES_iv);

                DESCryptoServiceProvider des = new DESCryptoServiceProvider();

                using (MemoryStream ms = new MemoryStream()) {
                    byte[] inData = Encoding.UTF8.GetBytes(sourceString);
                    try {
                        using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(btKey, btIV), CryptoStreamMode.Write)) {
                            cs.Write(inData, 0, inData.Length);

                            cs.FlushFinalBlock();
                        }

                        return Convert.ToBase64String(ms.ToArray());
                    }
                    catch {
                        return sourceString;
                    }
                }
            }
            catch { }

            return "DES加密出错";
        }

        //DES解密
        public static string DESDecrypt(string encryptedString)
        {
            byte[] btKey = Encoding.UTF8.GetBytes(DES_key);

            byte[] btIV = Encoding.UTF8.GetBytes(DES_iv);

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            using (MemoryStream ms = new MemoryStream()) {
                byte[] inData = Convert.FromBase64String(encryptedString);
                try {
                    using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(btKey, btIV), CryptoStreamMode.Write)) {
                        cs.Write(inData, 0, inData.Length);

                        cs.FlushFinalBlock();
                    }

                    return Encoding.UTF8.GetString(ms.ToArray());
                }
                catch {
                    return encryptedString;
                }
            }
        }

        //AES加密
        public static string AESEncrypt(string toEncrypt)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(AES_128_key);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        //AES解密
        public static string AESDecrypt(string toDecrypt)
        {
            try {
                byte[] keyArray = UTF8Encoding.UTF8.GetBytes(AES_128_key);
                byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateDecryptor();

                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return UTF8Encoding.UTF8.GetString(resultArray);
            }
            catch (Exception ex) {
                return ex.ToString();
            }

        }

        public static string ValidatePassword(string str)
        {
            string alph = @"[A-Za-z]+";
            string num = @"\d+";
            //string cha = @"[\-`=\\\[\];',\./~!@#\$%\^&\*\(\)_\+\|\{\}:""<>\?]+";
            if (!new Regex(alph).IsMatch(str)) {
                return "新密码必须包含英文字母，保存失败。英文字母有：A~Z，a~z";
            }
            if (!new Regex(num).IsMatch(str)) {
                return "新密码必须包含阿拉伯数字，保存失败。数字有：0~9";
            }
            //if (!new Regex(cha).IsMatch(str)) {
            //    return @"新密码必须包含特殊字符，保存失败。特殊字符有：-`=\[];',./~!@#$%^&*()_+|{}:""<>?";
            //}
            return "";
        }

        //生成随机数列
        public static string CreateValidateNumber(int length)
        {
            //去掉数字0和字母o，因为不容易区分
            string Vchar = "1,2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,i,j,k,l,m,n,p" +
            ",q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,I,J,K,L,M,N,P,Q" +
            ",R,S,T,U,V,W,X,Y,Z";

            string[] VcArray = Vchar.Split(new Char[] { ',' });//拆分成数组
            string num = "";

            int temp = -1;//记录上次随机数值，尽量避避免生产几个一样的随机数

            Random rand = new Random();
            //采用一个简单的算法以保证生成随机数的不同
            for (int i = 1; i < length + 1; i++) {
                if (temp != -1) {
                    rand = new Random(i * temp * unchecked((int)DateTime.Now.Ticks));
                }

                int t = rand.Next(VcArray.Length - 1);
                if (temp != -1 && temp == t) {
                    return CreateValidateNumber(length);

                }
                temp = t;
                num += VcArray[t];
            }
            return num;
        }

        //将保存在数据库的图片二进制转化为Image格式
        public static Image BytesToImage(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream(buffer)) {
                Image image = System.Drawing.Image.FromStream(ms);
                return image;
            }
        }

        //生成缩略图
        public static byte[] MakeThumbnail(Image originalImage, int width = 0, int height = 128, string mode = "H")
        {
            int towidth = width;
            int toheight = height;

            int x = 0;
            int y = 0;
            int ow = originalImage.Width;
            int oh = originalImage.Height;

            switch (mode) {
                case "HW"://指定高宽缩放（可能变形）                 
                    break;
                case "W"://指定宽，高按比例                     
                    toheight = originalImage.Height * width / originalImage.Width;
                    break;
                case "H"://指定高，宽按比例 
                    towidth = originalImage.Width * height / originalImage.Height;
                    break;
                case "Cut"://指定高宽裁减（不变形）                 
                    if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight) {
                        oh = originalImage.Height;
                        ow = originalImage.Height * towidth / toheight;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else {
                        ow = originalImage.Width;
                        oh = originalImage.Width * height / towidth;
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }

            //新建一个bmp图片 
            Image bitmap = new System.Drawing.Bitmap(towidth, toheight);

            //新建一个画板 
            Graphics g = System.Drawing.Graphics.FromImage(bitmap);

            //设置高质量插值法 
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

            //设置高质量,低速度呈现平滑程度 
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            //清空画布并以透明背景色填充 
            g.Clear(Color.Transparent);

            //在指定位置并且按指定大小绘制原图片的指定部分 
            g.DrawImage(originalImage, new Rectangle(0, 0, towidth, toheight),
                new Rectangle(x, y, ow, oh),
                GraphicsUnit.Pixel);

            try {
                byte[] bytes;
                using (MemoryStream ms = new MemoryStream()) {
                    bitmap.Save(ms, originalImage.RawFormat);
                    bytes = ms.ToArray();
                }
                return bytes;
            }
            catch (System.Exception e) {
                throw e;
            }
            finally {
                originalImage.Dispose();
                g.Dispose();
            }
        }

    }

    public class JpushHelper
    {
        private static string TITLE = "员工查询系统";
        private static string APP_KEY = "aa99e673ed652d8c9109abb7";
        private static string MASTER_SECRET = "b956f7edcfa8663306b88499";
        private static JPushClient client = new JPushClient(APP_KEY, MASTER_SECRET);                

        public static SimpleResultModel SendToAll(string msg)
        {            
            PushPayload pushPayload = new PushPayload();
            pushPayload.platform = Platform.android();
            pushPayload.audience = Audience.all();
            pushPayload.notification = new Notification().setAlert(msg);
            AndroidNotification androidnotification = new AndroidNotification();
            androidnotification.setTitle(TITLE);
            pushPayload.notification.AndroidNotification = androidnotification;


            try {
                var result = client.SendPush(pushPayload);
                //由于统计数据并非非是即时的,所以等待一小段时间再执行下面的获取结果方法
                //System.Threading.Thread.Sleep(10000);
                //如需查询上次推送结果执行下面的代码
                //var apiResult = client.getReceivedApi(result.msg_id.ToString());
                //var apiResultv3 = client.getReceivedApi_v3(result.msg_id.ToString());
                //如需查询某个messageid的推送结果执行下面的代码
                //var queryResultWithV2 = client.getReceivedApi("1739302794");
                //var querResultWithV3 = client.getReceivedApi_v3("1739302794");
                return new SimpleResultModel() { suc = true, msg = result.msg_id.ToString() };
            }
            catch (APIRequestException e) {
                return new SimpleResultModel() { suc = false, msg = "HTTP Status: " + e.Status + ";Error Code: " + e.ErrorCode + ";Error Message: " + e.ErrorMessage };
            }
            catch (APIConnectionException e) {
                return new SimpleResultModel() { suc = false, msg = e.message };
            }
            
        }

        public static SimpleResultModel SendToAlias(string msg,string alias)
        {
            PushPayload pushPayload = new PushPayload();
            pushPayload.platform = Platform.android();
            pushPayload.audience = Audience.s_alias(alias.Split(','));
            pushPayload.notification = new Notification().setAlert(msg);
            AndroidNotification androidnotification = new AndroidNotification();
            androidnotification.setTitle(TITLE);            
            pushPayload.notification.AndroidNotification = androidnotification;

            try {
                var result = client.SendPush(pushPayload);                
                return new SimpleResultModel() { suc = true, msg = result.msg_id.ToString() };
            }
            catch (APIRequestException e) {
                return new SimpleResultModel() { suc = false, msg = "HTTP Status: " + e.Status + ";Error Code: " + e.ErrorCode + ";Error Message: " + e.ErrorMessage };
            }
            catch (APIConnectionException e) {
                return new SimpleResultModel() { suc = false, msg = e.message };
            }
        }

        public static SimpleResultModel SendToTags(string msg, string tags)
        {
            PushPayload pushPayload = new PushPayload();
            pushPayload.platform = Platform.android();
            pushPayload.audience = Audience.s_tag(tags.Split(','));
            pushPayload.notification = new Notification().setAlert(msg);
            AndroidNotification androidnotification = new AndroidNotification();
            androidnotification.setTitle(TITLE);
            pushPayload.notification.AndroidNotification = androidnotification;

            try {
                var result = client.SendPush(pushPayload);
                return new SimpleResultModel() { suc = true, msg = result.msg_id.ToString() };
            }
            catch (APIRequestException e) {
                return new SimpleResultModel() { suc = false, msg = "HTTP Status: " + e.Status + ";Error Code: " + e.ErrorCode + ";Error Message: " + e.ErrorMessage };
            }
            catch (APIConnectionException e) {
                return new SimpleResultModel() { suc = false, msg = e.message };
            }
        }

        public static SimpleResultModel QuerySendResult(string msgId)
        {
            var resultWithV3 = client.getReceivedApi_v3(msgId);
            if (resultWithV3.isResultOK()) {
                return new SimpleResultModel() { suc = true, msg = "发送成功" };
            }
            else {
                return new SimpleResultModel() { suc = false, msg = resultWithV3.getErrorCode() + ":" + resultWithV3.getErrorMessage() };
            }
        }

    }
}