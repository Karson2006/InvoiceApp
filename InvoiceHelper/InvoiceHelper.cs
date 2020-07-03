using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using iTR.Lib;
using System.Xml;


namespace iTR.OP.Invoice
{
    public class InvoiceHelper
    {
        XmlDocument cfgDoc = null;
        public InvoiceHelper()
        {
            cfgDoc = new XmlDocument();
            cfgDoc.Load("cfg.xml");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileType">1:PDF</param>
        /// <param name="timeOutSecond"></param>
        /// <returns></returns>
        public InvoiceCheckResult Scan_Check(string fileName, string fileType,int timeOutSecond = 8)
        {
            InvoiceCheckResult result = null;
            try
            {
                byte[] bytes = bytes = GetBytesByPath(fileName);;
              
                string url = cfgDoc.SelectSingleNode("Configuration/Url0").InnerText.Trim();
                string timeoutSecond = cfgDoc.SelectSingleNode("Configuration/TimeOutSeceonds").InnerText.Trim();
                if(timeoutSecond.Length ==0)
                    timeoutSecond ="8";
                string validCode = cfgDoc.SelectSingleNode("Configuration/ValidCode").InnerText.Trim();
                string base64String = Convert.ToBase64String(bytes);;
               
                string jsonString = "{" +
                                "\"secret\":\"" + validCode + "\"," +
                                "\"type\":" + fileType + "," +
                                "\"base64\":\"" + base64String + "\"}";

                string resultJson =iTR.Lib.WebInvoke.PostJson(url, jsonString,int.Parse(timeoutSecond));

                result = JsonConvert.DeserializeObject<InvoiceCheckResult>(resultJson);
            }
            catch (Exception err)
            {
                //throw err;
                FileLogger.WriteLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "处理文件异常： " + err.Message, 2);
            }

            return result;
        }

        public InvoiceCheckResult Scan(string fileName, string fileType, int timeOutSecond = 8)
        {
            InvoiceCheckResult result = null;
            try
            {
                byte[] bytes = GetBytesByPath(fileName);//获取文件byte[]

                string url = cfgDoc.SelectSingleNode("Configuration/Url1").InnerText.Trim();
                string timeoutSecond = cfgDoc.SelectSingleNode("Configuration/TimeOutSeceonds").InnerText.Trim();
                if (timeoutSecond.Length == 0)
                    timeoutSecond = "8";
                string validCode = cfgDoc.SelectSingleNode("Configuration/ValidCode").InnerText.Trim();
                string base64String = Convert.ToBase64String(bytes);
                string jsonString = "{" +
                                "\"secret\":\"" + validCode + "\"," +
                                "\"type\":" + fileType + "," +
                                "\"base64\":\"" + base64String + "\"}";

                string resultJson = iTR.Lib.WebInvoke.PostJson(url, jsonString, int.Parse(timeoutSecond));

                result = JsonConvert.DeserializeObject<InvoiceCheckResult>(resultJson);
            }
            catch (Exception err)
            {
                throw err;
            }

            return result;
        }

        #region PostJson
        //public string PostJson(string url, string josn, int timeoutSeconds = 8)
        //{

        //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //    request.Method = "POST";
        //    request.ContentType = "application/json";
        //    //声明一个HttpWebRequest请求

        //    request.Timeout = timeoutSeconds * 1000;
        //    //转发机制
        //    Encoding encoding = Encoding.UTF8;
        //    Stream streamrequest = request.GetRequestStream();
        //    StreamWriter streamWriter = new StreamWriter(streamrequest, encoding);
        //    streamWriter.Write(josn);
        //    streamWriter.Flush();
        //    streamWriter.Close();
        //    streamrequest.Close();

        //    //设置连接超时时间
        //    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //    Stream streamresponse = response.GetResponseStream();
        //    StreamReader streamReader = new StreamReader(streamresponse, encoding);
        //    string result = streamReader.ReadToEnd();
        //    streamresponse.Close();
        //    streamReader.Close();

        //    return result;
        //}
        #endregion
        
        public static byte[] GetBytesByPath(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
            BinaryReader br = new BinaryReader(fs, utf8);
            //BinaryReader br = new BinaryReader(fs);
            byte[] bytes = br.ReadBytes((int)fs.Length);
            fs.Flush();
            fs.Close();
            return bytes;
        }

    }

    public class InvoiceCheckResult
    {
        public string errcode { get; set; }
        public string description { get; set; }

        public List<InvoiceDetail> list;
    }


    public class InvoiceDetail
    {
        /// <summary>
        /// 发票流水号
        /// </summary>
        public string serialNo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string invoiceCode { get; set; }
        public string invoiceNo { get; set; }
        public string invoiceDate { get; set; }
        public string salerName { get; set; }
        public string amount { get; set; }
        public string taxAmount { get; set; }
        public string totalAmount { get; set; }
        public string invoiceType { get; set; }
        public string buyerTaxNo { get; set; }
        public string salerAccount { get; set; }
        public string checkStatus { get; set; }
        public string checkErrcode { get; set; }
        public string checkCode { get; set; }
        public string checkDescription { get; set; }
        
    }
}
