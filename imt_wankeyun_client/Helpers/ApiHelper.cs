﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using System.Net;
using System.Diagnostics;
using imt_wankeyun_client.Entities.Account;
using imt_wankeyun_client.Entities;
using System.Windows.Media.Imaging;
using System.IO;
using imt_wankeyun_client.Entities.Control;
using imt_wankeyun_client.Entities.Account.Activate;

namespace imt_wankeyun_client.Helpers
{
    public class ApiHelper
    {
        /// <summary>
        /// 模拟的app版本
        /// </summary>
        static string appVersion = "1.4.5";
        static string apiAccountUrl = "https://account.onethingpcs.com";
        static string apiControlUrl = "https://control.onethingpcs.com";
        static Dictionary<string, RestClient> clients = new Dictionary<string, RestClient>();
        internal static Dictionary<string, UserBasicData> userBasicDatas = new Dictionary<string, UserBasicData>();
        internal static Dictionary<string, Device> userDevices = new Dictionary<string, Device>();
        internal static Dictionary<string, UserInfo> userInfos = new Dictionary<string, UserInfo>();
        static RestClient GetClient(string phone)
        {
            if (!clients.ContainsKey(phone))
            {
                var c = new RestClient(apiAccountUrl)
                {
                    CookieContainer = new System.Net.CookieContainer()
                };
                clients.Add(phone, c);
            }
            return clients[phone];
        }
        /// <summary>
        /// 检查手机号是否已经注册
        /// </summary>
        /// <param name="phone">手机号</param>
        /// <returns></returns>
        public static async Task<HttpMessage> CheckRegister(string phone)
        {
            var client = GetClient(phone);
            var data = new Dictionary<string, string>();
            data.Add("phone", phone.Trim());
            var resp = await Task.Run(() =>
            {
                client.BaseUrl = new Uri(apiAccountUrl);
                var request = new RestRequest($"user/check?appversion={appVersion}", Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddParameter("application/x-www-form-urlencoded", GetParams(client, data), ParameterType.RequestBody);
                return client.Execute(request);
            });
            var message = new HttpMessage { statusCode = resp.StatusCode };
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                Debug.WriteLine(resp.Content);
                message.data = JsonHelper.Deserialize<LoginResponse>(resp.Content);
            }
            else
            {
                Debug.WriteLine(resp.Content);
                message.data = resp.Content;
            }
            return message;
        }
        /// <summary>
        /// 登陆
        /// </summary>
        /// <param name="phone">手机号</param>
        /// <param name="pwd">密码</param>
        /// <param name="imgcode">验证码</param>
        /// <param name="account_type">账户类型（默认4）</param>
        /// <param name="deviceid">设备ID</param>
        /// <param name="imeiid">IMEIID</param>
        /// <returns></returns>
        public static async Task<HttpMessage> Login(string phone, string pwd, string imgcode, string account_type,
            string deviceid, string imeiid)
        {
            var client = GetClient(phone);
            var logindata = new Dictionary<string, string>();
            logindata.Add("phone", phone);
            logindata.Add("pwd", pwd);
            if (imgcode.Trim().Length > 0)
            {
                logindata.Add("img_code", imgcode);
                Debug.WriteLine("imgcode.Trim():" + imgcode);
            }
            logindata.Add("account_type", account_type);
            logindata.Add("deviceid", deviceid);
            logindata.Add("imeiid", imeiid);
            var resp = await Task.Run(() =>
            {
                client.BaseUrl = new Uri(apiAccountUrl);
                var request = new RestRequest($"user/login?appversion={appVersion}", Method.POST);
                Debug.WriteLine(GetParams(client, logindata));
                request.AddHeader("cache-control", "no-cache");
                request.AddParameter("application/x-www-form-urlencoded", GetParams(GetClient(phone), logindata), ParameterType.RequestBody);
                return client.Execute(request);
            });
            var message = new HttpMessage { statusCode = resp.StatusCode };
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                Debug.WriteLine(resp.Content);
                message.data = JsonHelper.Deserialize<LoginResponse>(resp.Content);
            }
            else
            {
                Debug.WriteLine(resp.Content);
                message.data = resp.Content;
            }
            return message;
        }
        /// <summary>
        /// 获取设备详情
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public static async Task<HttpMessage> ListPeer(string phone)
        {
            var client = GetClient(phone);
            var data = new Dictionary<string, string>();
            data.Add("appversion", appVersion);
            data.Add("v", "1");
            data.Add("ct", "1");
            var gstr = GetParams(client, data, true);
            var sessionid = GetCookie(client, apiAccountUrl, "sessionid");
            var userid = GetCookie(client, apiAccountUrl, "userid");
            //Debug.WriteLine("ListPeer-gstr:" + gstr);
            //Debug.WriteLine("ListPeer-sessionid:" + sessionid);
            //Debug.WriteLine("ListPeer-userid:" + userid);
            var resp = await Task.Run(() =>
            {
                client.BaseUrl = new Uri(apiControlUrl);
                var request = new RestRequest($"listPeer?{gstr}", Method.GET);
                Debug.WriteLine(GetParams(client, data));
                request.AddHeader("cache-control", "no-cache");
                request.AddParameter("sessionid", sessionid, ParameterType.Cookie);
                request.AddParameter("userid", userid, ParameterType.Cookie);
                return client.Execute(request);
            });
            var message = new HttpMessage { statusCode = resp.StatusCode };
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                Debug.WriteLine(resp.Content);
                var root = JsonHelper.Deserialize<PeerRoot>(resp.Content);
                message.data = root;
            }
            else
            {
                Debug.WriteLine(resp.Content);
                message.data = resp.Content;
            }
            return message;
        }
        /// <summary>
        /// 获取设备详情
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public static async Task<HttpMessage> GetUserInfo(string phone)
        {
            var client = GetClient(phone);
            if (userDevices[phone] == null)
            {
                return null;
            }
            var device_sn = userDevices[phone].device_sn;
            var sessionid = GetCookie(client, apiAccountUrl, "sessionid");
            var userid = GetCookie(client, apiAccountUrl, "userid");
            //Debug.WriteLine("ListPeer-sessionid:" + sessionid);
            //Debug.WriteLine("ListPeer-userid:" + userid);
            var data = new Dictionary<string, string>();
            data.Add("sn", device_sn);
            var pstr = GetParams(client, data, true);
            Debug.WriteLine("GetUserInfo-pstr:" + pstr);
            var resp = await Task.Run(() =>
            {
                client.BaseUrl = new Uri(apiAccountUrl);
                var request = new RestRequest($"activate/userinfo?appversion=1.4.5", Method.POST);
                Debug.WriteLine(GetParams(client, data));
                request.AddHeader("cache-control", "no-cache");
                request.AddParameter("sessionid", sessionid, ParameterType.Cookie);
                request.AddParameter("userid", userid, ParameterType.Cookie);
                request.AddHeader("cache-control", "no-cache");
                request.AddParameter("application/x-www-form-urlencoded", pstr, ParameterType.RequestBody);
                return client.Execute(request);
            });
            var message = new HttpMessage { statusCode = resp.StatusCode };
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                Debug.WriteLine(resp.Content);
                var root = JsonHelper.Deserialize<UserInfoResponse>(resp.Content);
                message.data = root;
            }
            else
            {
                Debug.WriteLine(resp.Content);
                message.data = resp.Content;
            }
            return message;
        }
        /// <summary>
        /// 获取client的cookie的值
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cookieKey"></param>
        /// <returns></returns>
        private static string GetCookie(RestClient client, string domain, string cookieKey)
        {
            var value = "";
            var cookies = client.CookieContainer.GetCookies(new Uri(domain));
            foreach (Cookie t in cookies)
            {
                //Debug.WriteLine("当前Cookie:" + t.Name + "=" + t.Value);
                if (t.Name == cookieKey)
                {
                    value = t.Value;
                    break;
                }
            }
            //Debug.WriteLine($"当前 {cookieKey}=" + value);
            return value;
        }
        private static void SetCookie(RestClient client, string domain, string cookieHeader)
        {
            client.CookieContainer.SetCookies(new Uri(domain), cookieHeader);
        }
        private static string GetParams(RestClient client, Dictionary<string, string> logindata, bool isGet = false)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < logindata.Count; i++)
            {
                var t = logindata.ElementAt(i);
                if (t.Key == "pwd")
                {
                    list.Add(t.Key + "=" + UtilHelper.SignPassword(t.Value));
                }
                else
                {
                    list.Add(t.Key + "=" + t.Value);
                }
            }
            list.Sort();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(list[i]);
                if (i != list.Count - 1)
                {
                    sb.Append("&");
                }
            }
            var gstr = sb.ToString();//如果get，不加key参数
            sb.Append("&");
            var sessionid = GetCookie(client, apiAccountUrl, "sessionid");
            sb.Append("key=").Append(sessionid);

            string str = sb.ToString();//这是sign的原文
            string sign = UtilHelper.GetMD5(str);//这是最终的sign
            return isGet ? $"{gstr}&sign={sign}" : $"{str}&sign={sign}";
        }
        public static async Task<BitmapImage> GetValiImg(string phone, string url)
        {
            var client = GetClient(phone);
            var imagedata = await Task.Run(() =>
            {
                var request = new RestRequest(url, Method.GET);
                return client.Execute(request);
            });
            using (var ms = new MemoryStream(imagedata.RawBytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                return bitmap;
            }
        }
    }
}
