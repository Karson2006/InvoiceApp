using iTR.Lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Invoice.Utils
{

    public class KingDeeApi
    {
        private static string disData;

        private static string GetAccessToken()
        {
            JObject jObj = new JObject();
            string accessStr = null, accessToken = null;
            //只获取一次时间戳 多次获取肯定会出错
            string timespan = ApiUtil.TimeSpan;
            string sign = EncrptionUtil.GetMD5Str(ApiUtil.ClientId + ApiUtil.ClientSecret + timespan);
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("client_id", ApiUtil.ClientId);
            dic.Add("sign", sign);
            dic.Add("timestamp", timespan);

            jObj["client_id"] = ApiUtil.ClientId;
            jObj["sign"] = sign;
            jObj["timestamp"] = timespan;
            string json = FormatHelper.ObjectToJson(jObj);
            try
            {
                accessStr = PostJson(ApiUtil.BaseUrl + ApiUtil.TokenUrl, json);
                jObj = FormatHelper.JsonToObject(accessStr);
                accessToken = jObj["access_token"].ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return accessToken;
        }
        /// <summary>
        /// 识别与查验（需要查验的自动查验）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="base64String">base64 字符串 </param>
        /// <returns></returns>
        public static InvoiceCheckResult Check(string fileName, string base64string)
        {

            string token = GetAccessToken();
            string jsonstr = "", logjson = "";


            List<string> authType = new List<string> { "1", "2", "3", "4", "5", "12", "13", "15" };

            //二手车没有税率 12 机动车识别和验真都返回taxrate 另外判断
            List<string> taxtype
                = new List<string> { "1", "2", "3", "4", "5", "12", "15" };
            //识别 + json 查验
            InvoiceCheckResult invoiceCheckResult = new InvoiceCheckResult() { CheckDetailList = new List<InvoiceCheckDetail>() };
            try
            {
                //image和pdf用base64识别
                disData = PostImage(ApiUtil.BaseUrl + ApiUtil.ImgDistguishUrl + token, base64string);
                logjson = disData;
                InvoiceDisResult invoiceDisResult = GetDisResult(disData);
                invoiceCheckResult.errcode = invoiceDisResult.errcode;
                invoiceCheckResult.description = invoiceDisResult.description;
                //识别成功
                if (invoiceDisResult.errcode == "0000")
                {
                    InvoiceLogger.WriteToDB("发票识别成功", invoiceCheckResult.errcode, invoiceCheckResult.description, fileName, disData);
                    //确定不通过的
                    List<string> code = new List<string>() { "1200", "1214", "1301", "0006", "0009", "1005", "1008", "1009", "0313", "0314" };
                    foreach (InvoiceCheckDetail item in invoiceDisResult.data)
                    {
                        jsonstr = "";
                        //默认识别结果日志
                        logjson = disData;
                        ReciveData recive = new ReciveData();
                        //虽然识别成功有些数据可能还是null
                        item.invoiceCode = item.invoiceCode == null ? "" : item.invoiceCode;
                        item.invoiceNo = item.invoiceNo == null ? "" : item.invoiceNo;
                        item.invoiceDate = item.invoiceDate == null ? "" : item.invoiceDate;
                        item.invoiceMoney = item.invoiceMoney == null ? "" : item.invoiceMoney;
                        item.checkCode = item.checkCode == null ? "" : item.checkCode;
                        item.totalAmount = item.totalAmount == null ? "" : item.totalAmount;
                        //验真类型
                        if (authType.Contains(item.invoiceType))
                        {
                            //纸质专用发票，机动车 用invoiceMoney 验真,其他用totalAmount
                            if (item.invoiceType != "4" && item.invoiceType != "12")
                            {
                                item.invoiceMoney = item.totalAmount;
                            }
                            //验真用另一个数据结构
                            AuthData authData = new AuthData();

                            authData.invoiceCode = item.invoiceCode;
                            authData.invoiceNo = item.invoiceNo;
                            authData.invoiceDate = item.invoiceDate;
                            authData.invoiceMoney = item.invoiceMoney;
                            authData.checkCode = item.checkCode;
                            authData.isCreateUrl = "1";
                            //转验真json字符串
                            jsonstr = JsonConvert.SerializeObject(authData);
                            try
                            {
                                //获取查验结果
                                jsonstr = PostJson(ApiUtil.BaseUrl + ApiUtil.TextCheckUrl + token, jsonstr);
                                //保存到日志的验真结果
                                logjson = jsonstr;
                                         recive = GetCheckResult(jsonstr);

                                //判断查验结果
                                if (recive.errcode == "0000")
                                {
                                    if (recive.data.cancelMark == "N")
                                    {
                                        item.checkStatus = "通过";

                                    }
                                }
                                else
                                {
                                    if (code.Contains(recive.errcode))
                                    {
                                        item.checkStatus = "不通过";
                                    }
                                    else
                                    {
                                        //不是发票问题
                                        item.checkStatus = "未查验";
                                    }
                                }
                                //避免验真不通过之后，获取null值发生异常
                                item.serialNo = recive.data.serialNo == null ? "" : recive.data.serialNo;
                                item.salerName = recive.data.salerName == null ? "" : recive.data.salerName;
                                item.salerAccount = recive.data.salerAccount == null ? "" : recive.data.salerAccount;
                                item.amount = recive.data.amount == null ? "" : recive.data.amount;
                                item.buyerTaxNo = recive.data.buyerTaxNo == null ? "" : recive.data.buyerTaxNo;

                                //税率
                                if (taxtype.Contains(item.invoiceType))
                                {                                    
                                        item.taxRate = recive.data.items[0].taxRate == null ? "" : recive.data.items[0].taxRate;
                                }

                                InvoiceLogger.WriteToDB("发票验真结果数据状态", recive.errcode, recive.description, fileName, logjson, item.invoiceType);
                            }
                            catch (Exception ex)
                            {
                                InvoiceLogger.WriteToDB("发票验真异常:" + ex.Message, invoiceCheckResult.errcode, invoiceCheckResult.description, fileName, logjson, item.invoiceType);
                                continue;
                            }
                        }

                        //不用验真的
                        else
                        {
                            //避免不需要验真的发票没有数据 获取null 发生异常


                            item.serialNo = item.serialNo == null ? "" : item.serialNo;
                            item.salerName = item.salerName == null ? "" : item.salerName;
                            item.salerAccount = item.salerAccount == null ? "" : item.salerAccount;
                            item.amount = item.amount == null ? "" : item.amount;
                            item.buyerTaxNo = item.buyerTaxNo == null ? "" : item.buyerTaxNo;


                            item.checkStatus = "通过";
                            //火车票
                            if (item.invoiceType == "9")
                            {
                                item.invoiceNo = item.printingSequenceNo;
                            }
                            //飞机票
                            if (item.invoiceType == "10")
                            {
                                item.invoiceNo = item.electronicTicketNum;
                            }
                            logjson = JsonConvert.SerializeObject(item);
                        }
                        //只识别的发票，只有飞机票有taxAmount
                        if (item.invoiceType == "10")
                        {
                            item.taxAmount = item.taxAmount == null ? "" : item.taxAmount;
                        }
                        else
                        {
                            item.taxAmount = item.taxAmount == null ? "" : item.taxAmount;
                        }
                        //只有机动车有 taxrate ，覆盖验真的taxrate 用识别的taxrate
                        if (item.invoiceType == "12")
                        {
                            item.taxRate = item.taxRate == null ? "" : item.taxRate;
                        }
                        //验真状态
                        item.checkErrcode = recive.errcode == null ? "" : recive.errcode;
                        item.checkDescription = recive.description == null ? "" : recive.description;
                        //添加发票
                        invoiceCheckResult.CheckDetailList.Add(item);
                        InvoiceLogger.WriteToDB("发票识别+验真完成", invoiceCheckResult.errcode, invoiceCheckResult.description, fileName, logjson, item.invoiceType);
                    }
                }
                else
                {
                    InvoiceLogger.WriteToDB("", invoiceCheckResult.errcode, invoiceCheckResult.description, fileName, JsonConvert.SerializeObject(invoiceCheckResult));
                }
            }
            catch (Exception ex)
            {
                InvoiceLogger.WriteToDB("发票识别 + 验真 异常:" + ex.Message, invoiceCheckResult.errcode, invoiceCheckResult.description, fileName);
            }
            return invoiceCheckResult;
        }
        //获取识别结果
        private static InvoiceDisResult GetDisResult(string disJson)
        {
            InvoiceDisResult result = JsonConvert.DeserializeObject<InvoiceDisResult>(disJson);
            return result;
        }
        //获取查验结果
        private static ReciveData GetCheckResult(string AuthJson)
        {
            ReciveData result = JsonConvert.DeserializeObject<ReciveData>(AuthJson);
            return result;
        }
        private static string PostJson(string url, string param)
        {

            System.Net.HttpWebRequest request;
            request = (System.Net.HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            request.Timeout = 10 * 1000;//设置超时

            string paraUrlCoded = param;
            byte[] payload;
            payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
            request.ContentLength = payload.Length;
            try
            {

                Stream writer = request.GetRequestStream();
                writer.Write(payload, 0, payload.Length);
                writer.Close();
                System.Net.HttpWebResponse response;
                response = (System.Net.HttpWebResponse)request.GetResponse();
                System.IO.Stream s;
                s = response.GetResponseStream();
                string StrDate = "";
                string strValue = "";
                StreamReader Reader = new StreamReader(s, Encoding.UTF8);
                while ((StrDate = Reader.ReadLine()) != null)
                {
                    strValue += StrDate + "\r\n";
                }
                return strValue;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private static string PostImage(string url, string param)
        {

            System.Net.HttpWebRequest request;
            request = (System.Net.HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.Method = "POST";
            request.ContentType = "text/plain;charset=UTF-8";
            request.KeepAlive = true;
            //不加密？？
            //string encrypted= EncrptionUtil.encrypt(param,ApiUtil.EncryptKey);
            string paraUrlCoded = param;
            byte[] payload;
            payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
            request.ContentLength = payload.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(payload, 0, payload.Length);
            writer.Close();
            System.Net.HttpWebResponse response;
            response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream s;
            s = response.GetResponseStream();
            string StrDate = "";
            string strValue = "";
            StreamReader Reader = new StreamReader(s, Encoding.UTF8);
            while ((StrDate = Reader.ReadLine()) != null)
            {
                strValue += StrDate + "\r\n";
            }

            return strValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static string PostPDF(string url, string filePath)
        {
            #region 拼接请求

            string strURL = url;
            System.Net.HttpWebRequest request;
            request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
            request.Method = "POST";
            string boundary = DateTime.Now.Ticks.ToString();
            byte[] boundarybytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.KeepAlive = true;
            request.Credentials = System.Net.CredentialCache.DefaultCredentials;
            try
            {

                Stream rs = request.GetRequestStream();


                string path = filePath;
                System.IO.DirectoryInfo folderInfo = new DirectoryInfo(path);
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string headerTemplate = "Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\nContent-Type: application/pdf\r\n\r\n";
                string header = string.Format(headerTemplate, filePath);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);

                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[4096];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    rs.Write(buffer, 0, bytesRead);
                }
                fileStream.Close();


                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                rs.Write(trailer, 0, trailer.Length);
                rs.Close();
                #endregion

                System.Net.HttpWebResponse response;
                response = (System.Net.HttpWebResponse)request.GetResponse();
                System.IO.Stream s;
                s = response.GetResponseStream();
                string StrDate = "";
                string strValue = "";
                StreamReader Reader = new StreamReader(s, Encoding.UTF8);
                while ((StrDate = Reader.ReadLine()) != null)
                {
                    strValue += StrDate + "\r\n";
                }

                return strValue;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
    #region 识别和验真和结构


    class InvoiceDisResult
    {
        public string errcode { get; set; }
        public string description { get; set; }

        public List<InvoiceCheckDetail> data = null;
    }



    class AuthData
    {
        public string invoiceCode { get; set; }
        public string invoiceNo { get; set; }
        public string invoiceDate { get; set; }


        public string invoiceMoney { get; set; }
        public string totalAmount { get; set; }
        public string checkCode { get; set; }
        public string isCreateUrl { get; set; }
    }

    class ReciveData
    {
        public string errcode { get; set; }
        public string description { get; set; }

        public InvoiceCheckDetail data { get; set; }
    }

    public class InvoiceCheckResult
    {
        public string errcode { get; set; }
        public string description { get; set; }

        public List<InvoiceCheckDetail> CheckDetailList { get; set; }

    }

    /// <summary>
    /// 查验结构
    /// </summary>
    public class InvoiceCheckDetail
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
        //////合并新项
        public string invoiceMoney { get; set; }

        //火车票的
        public string printingSequenceNo { get; set; }

        //飞机票的
        public string electronicTicketNum { get; set; }

        //需要保存的状态                             
        public string cancelMark { get; set; }
        public string taxRate { get; set; }

        //税率
        public List<TaxRate> items { get; set; }
    }

    public class TaxRate
    {
        public string taxRate { get; set; }
    }
    #endregion
}

