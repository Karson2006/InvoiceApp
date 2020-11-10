//using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace Invoice.Utils
{
    public class ApiUtil
    {
        private static string client_id = "Dmr52ovwDN0ESd";
        private static string client_secret = "k6lirHrIjV3oiwO046HT3rc1idj4kE";
        private static string encrypt_key = "WwFjhx77iTxgcwlg";
        private static string base_url = "https://api-dev.piaozone.com/test";

        /// <summary>
        /// 正式链接信息
        /// </summary>
        //private static string client_id = "NcIxr84dGmLH6wNitGrZ";// ConfigurationManager.AppSettings["client_id"];
        //private static string client_secret = "b68bdc3068ab4754839f844b0525e616";// ConfigurationManager.AppSettings["client_secret"];
        //private static string encrypt_key = "KgCgtIKtitcuSYW5";//ConfigurationManager.AppSettings["encrypt_key"];
        //private static string base_url = "https://api.piaozone.com";// ConfigurationManager.AppSettings["base_url"];

        private static string token_url = "/base/oauth/token";
        private static string text_check_url = "/m13/bill/invoice/sys/check?access_token=";
        private static string img_check_url = "/m3/bill/invoice/img/Check/info?access_token=";
        private static string img_distguish_url = "/m3/bill/invoice/img/analyze/multiple/info?access_token=";
        private static string pdf_check_url = "/m3/bill/invoice/pdf/check?access_token=";
        private static string multiple_img_check_url = "/m3/bill/invoice/img/analyze/multiple/check?access_token=";
        private static string timeStamp = GetTimestamp(DateTime.Now);
        private static string filePath =  ConfigurationManager.AppSettings["path"];
        private static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        public static string ClientId
        {
            get
            {
                return client_id;
            }
            set
            {
                client_id = value;
            }
        }
        public static string ClientSecret
        {
            get
            {
                return client_secret;
            }
            set
            {
                client_secret = value;
            }
        }
        public static string EncryptKey
        {
            get
            {
                return encrypt_key;
            }
            set
            {
                encrypt_key = value;
            }
        }
        public static string BaseUrl
        {
            get
            {
                return base_url;
            }
        }
        public static string TokenUrl
        {
            get
            {
                return token_url;
            }
        }
        public static string TextCheckUrl
        {
            get
            {
                return text_check_url;
            }
        }
        public static string PDFCheckUrl
        {
            get
            {
                return pdf_check_url;
            }
        }
        public static string ImgDistguishUrl
        {
            get
            {
                return img_distguish_url;
            }
        }
        public static string ImgCheckUrl
        {
            get
            {
                return img_check_url;
            }
        }
        public static string MultiImgCheckUrl
        {
            get
            {
                return multiple_img_check_url;
            }
        }
        public static string TimeSpan
        {
            get
            {
                return timeStamp;
            }
        }
        public static string FilePath
        {
            get
            {
                return filePath;
            }
        }
       
    }
}