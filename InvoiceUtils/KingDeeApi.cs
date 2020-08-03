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
            bool authflag = false;

            List<string> authType = new List<string> { "1", "2", "3", "4", "5", "12", "13", "15" };

            //二手车没有税率 12 机动车识别和验真都返回taxrate 另外判断
            List<string> taxtype
                = new List<string> { "1", "2", "3", "4", "5", "15" };
            //应用问题
            List<string> errAPI = new List<string> { "0001", "0002", "1004", "1007", "1020", "1200", "1214", "1301", "1101", "1119", "1006", "1132", "3109", "9999", "0005" };
            //待查验
            List<string> notauth = new List<string>() { "1002", "1001", "1014", "3110" };
            //确定不通过的
            List<string> noPass = new List<string>() { "0006", "0009", "1005", "1008", "1009", "0313", "0314" };
        


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
                //不需要验真的状态

                //识别成功
                if (invoiceDisResult.errcode == "0000"|| invoiceDisResult.errcode == "10300")
                {
                    
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
                        item.taxRate = item.taxRate == null ? "" : item.taxRate;
                        item.taxAmount = item.taxAmount == null ? "" : item.taxAmount;

                        //不需要验真发票必须初始化的值

                        item.serialNo = item.serialNo == null ? "" : item.serialNo;
                        item.salerName = item.salerName == null ? "" : item.salerName;
                        item.salerAccount = item.salerAccount == null ? "" : item.salerAccount;
                        item.amount = item.amount == null ? "" : item.amount;
                        item.buyerTaxNo = item.buyerTaxNo == null ? "" : item.buyerTaxNo;

                        //已经初始化完成，开始判断是否串号
                        if (invoiceDisResult.errcode == "10300")
                        {
                            //发票代码转具体发票
                            item.invoiceType = Enum.GetName(typeof(InvoiceType), int.Parse(item.invoiceType));
                            item.checkErrcode = "10300";
                            item.checkStatus = "不通过";
                            item.checkDescription = "发票串号";
                            //添加发票
                            invoiceCheckResult.CheckDetailList.Add(item);
                            invoiceCheckResult.description = "操作成功";
                            //修改操作码
                            invoiceCheckResult.errcode = "0000";
                            InvoiceLogger.WriteToDB("发票串号", $"{invoiceCheckResult.errcode}", "", $"{invoiceCheckResult.description}", fileName, logjson, item.invoiceType);
                            //条件不满足 进行下一个
                            continue;
                        }
                        //验真类型
                        if (authType.Contains(item.invoiceType))
                        {
                            authflag = false;
                            //提前判断 如果查验条件不满足，不去验真
                            if (item.invoiceNo.Trim().Length == 0)
                            {
                                authflag = true;
                                item.checkDescription += " 发票号码识别为空 ";
                            }
                            if (item.invoiceCode.Trim().Length == 0)
                            {
                                authflag = true;
                                item.checkDescription += " 发票代码识别为空 ";
                            }

                            if (item.invoiceDate.Trim().Length == 0)
                            {
                                authflag = true;
                                item.checkDescription += " 发票日期识别为空 ";
                            }
                            //增值税普通发票、增值税电子普通发票（含通行费发票）、增值税普通发票(卷票)
                            if (item.invoiceType == "1" || item.invoiceType == "3" || item.invoiceType == "5" || item.invoiceType == "15")
                            {
                                if (item.checkCode.Trim().Length == 0)
                                {
                                    authflag = true;
                                    item.checkDescription += " 发票检验码识别为空 ";
                                }
                            }

                            //机动车和 纸质专用发票必须要有 InvoiceMoney
                            if (item.invoiceType == "2" || item.invoiceType == "4" || item.invoiceType == "12")
                            {
                                if (item.invoiceMoney.Trim().Length == 0)
                                {
                                    authflag = true; 
                                    item.checkDescription += " 不含税金额识别为空 ";
                                }
                            }
                            // 二手车
                            if (item.invoiceType == "13")
                            {
                                if (item.totalAmount.Trim().Length == 0)
                                {
                                    authflag = true;
                                    item.checkDescription += " 车价合计识别为空 ";
                                }
                            }
                            //必须同时满足几个条件
                            if (authflag)
                            {
                                //发票代码转具体发票
                                item.invoiceType = Enum.GetName(typeof(InvoiceType), int.Parse(item.invoiceType));
                                item.checkErrcode = "10005";
                                item.checkStatus = "未查验";                                
                                //先写日志
                                InvoiceLogger.WriteToDB("发票查验条件不满足", $"{invoiceCheckResult.errcode}", "", $"{invoiceCheckResult.description}",  fileName, logjson, item.invoiceType);
                                item.checkDescription = "未识别到完整发票信息";

                                //添加发票
                                invoiceCheckResult.CheckDetailList.Add(item);
                                //条件不满足 进行下一个
                                continue;
                            }
                            //纸质专用发票，机动车 用invoiceMoney 验真,其他用totalAmount 避免校验码和金额同时为空
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
                                //验真状态 识别成功都有状态
                                item.checkErrcode = recive.errcode == null ? "" : recive.errcode;
                                item.checkDescription = recive.description == null ? "" : recive.description;
                                //避免验真不通过之后，获取null值发生异常
                                item.serialNo = recive.data.serialNo == null ? "" : recive.data.serialNo;
                                item.salerName = recive.data.salerName == null ? "" : recive.data.salerName;
                                item.salerAccount = recive.data.salerAccount == null ? "" : recive.data.salerAccount;
                                item.amount = recive.data.amount == null ? "" : recive.data.amount;
                                item.buyerTaxNo = recive.data.buyerTaxNo == null ? "" : recive.data.buyerTaxNo;
                                item.taxAmount = recive.data.taxAmount == null ? "" : recive.data.taxAmount;

                                //税率
                                if (taxtype.Contains(item.invoiceType))
                                {
                                    if (recive.data.items != null)
                                    {
                                        item.taxRate = recive.data.items[0].taxRate == null ? "" : recive.data.items[0].taxRate;
                                    }

                                }


                                //发票代码转具体发票
                                item.invoiceType = Enum.GetName(typeof(InvoiceType), int.Parse(item.invoiceType));
                                //设置查验结果
                                if (recive.errcode == "0000")
                                {
                                    if (recive.data.cancelMark == "N")
                                    {
                                        item.checkStatus = "通过";
                                    }
                                    else
                                    {
                                        item.checkErrcode = "10004";
                                        item.checkStatus = "不通过";
                                        InvoiceLogger.WriteToDB("发票作废", invoiceCheckResult.errcode, recive.errcode, recive.description, fileName, logjson, item.invoiceType);
                                    }
                                }
                                else
                                {

                                    //不通过的
                                    if (noPass.Contains(recive.errcode))
                                    {
                                        //变成统一返回码
                                        item.checkErrcode = "10002";
                                        item.checkDescription = "所查发票不存在";
                                        item.checkStatus = "不通过";
                                    
                                    }
                                    else
                                    {

                                        item.checkStatus = "未查验";
                                        //重新说明 接口错误
                                        if (errAPI.Contains(recive.errcode))
                                        {                                           
                                            item.checkErrcode = "10003";
                                            item.checkDescription = "发票查验应用系统错误!";
                                        }
                                    }
                                    InvoiceLogger.WriteToDB("查验未通过", invoiceCheckResult.errcode, recive.errcode, recive.description, fileName, logjson, item.invoiceType);
                                }

                                ////发票代码转具体发票
                                //item.invoiceType = Enum.GetName(typeof(InvoiceType), int.Parse(item.invoiceType));
                            }
                            catch (Exception ex)
                            {
                                item.checkErrcode = "10001";
                                item.checkDescription = "验真异常发票";
                                //添加发票
                                //发票代码转具体发票
                                item.invoiceType = Enum.GetName(typeof(InvoiceType), int.Parse(item.invoiceType));
                                invoiceCheckResult.CheckDetailList.Add(item);
                                InvoiceLogger.WriteToDB("验真异常:" + ex.Message, invoiceCheckResult.errcode, "",invoiceCheckResult.description, fileName, logjson, item.invoiceType);
                                continue;
                            }
                        }

                        //不用验真的
                        else
                        {    
                            item.checkErrcode = "0000";
                            item.checkDescription = "不验真发票状态正常";

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
                            //发票代码转具体发票
                            item.invoiceType = Enum.GetName(typeof(InvoiceType), int.Parse(item.invoiceType));
                            logjson = JsonConvert.SerializeObject(item);
                        }
                        //添加发票
                        invoiceCheckResult.CheckDetailList.Add(item);

                    }
                }

            }
            catch (Exception ex)
            {
                invoiceCheckResult.errcode = "20000";
                invoiceCheckResult.description = "识别 + 验真时 异常" + ex.Message;
                InvoiceLogger.WriteToDB("识别 + 验真 异常:" + ex.Message, invoiceCheckResult.errcode,"", invoiceCheckResult.description, fileName);
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

