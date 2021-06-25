using iTR.Lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Invoice.Utils
{

    public class KingDeeApi
    {
        static string disData;
        static List<string> authType = new List<string> { "1", "2", "3", "4", "5", "12", "13", "15" };

        //二手车没有税率 12 机动车识别和验真都返回taxrate 另外判断
        static List<string> taxtype
            = new List<string> { "1", "2", "3", "4", "5", "15" };
        //应用问题
        static List<string> errAPI = new List<string> { "0001", "0002", "1004", "1007", "1020", "1200", "1214", "1301", "1101", "1119", "1132", "3109", "9999", "0005" };
        //待查验
        static List<string> notauth = new List<string>() { "1002", "1001", "1014", "3110" };
        //确定不通过的
        static List<string> noPass = new List<string>() { "0006", "0009", "1005", "1006", "1008", "1009", "0313", "0314" };



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
        /// 手动查验发票方法
        /// </summary>
        /// <param name="code">发票代码</param>
        /// <param name="no">发票号码</param>
        /// <param name="date">发票日期</param>
        /// <param name="money">不含税金额</param>
        /// <param name="checkCode">校验码后六位</param>
        /// <returns></returns>
        public static InvoiceCheckResult ManualCheck(string code, string no, string date, string money = "", string checkCode = "")
        {
            //验真用另一个数据结构
            AuthData authData = new AuthData();
            InvoiceCheckDetail invoiceCheckDetail = new InvoiceCheckDetail();
            InvoiceCheckResult invoiceCheckResult = new InvoiceCheckResult() { CheckDetailList = new List<InvoiceCheckDetail>() };
            invoiceCheckResult.errcode = "0000";
            invoiceCheckResult.description = "手动验真正常";
            string logjson = "";
            string jsonstr = "";
            string token;
            if (code.Trim().Length == 0 || no.Trim().Length == 0 || date.Trim().Length == 0)
            {
                invoiceCheckDetail.checkErrcode = "10005";
                invoiceCheckDetail.checkDescription = "未查验";
            }
            else
            {
                try
                {
                    //日期处理
                    string tempdate = DateTime.Parse(date).ToString("yyyyMMdd");
                    date = tempdate;
                    token = GetAccessToken();
                    authData.invoiceCode = code;
                    authData.invoiceNo = no;
                    authData.invoiceDate = date;
                    authData.invoiceMoney = money;
                    //以下大部分是空格处理，有空格内容，查验接口会返回无法使用的状态，程序退出
                    //这接口只是能用的状态，有时候接口问题随缘出现
                    if (checkCode.Length>6)
                    {
                        authData.checkCode = checkCode.Replace(" ", "").Substring(checkCode.Length-6);
                    }
                    else
                    {
                        authData.checkCode = checkCode.Replace(" ","");
                    }
                    authData.invoiceNo = no.Replace(" ", "");
                    authData.invoiceCode = code.Replace(" ", "");
                    authData.isCreateUrl = "1";
                    invoiceCheckDetail = KingdeeCheck(token, ref invoiceCheckDetail, authData, ref logjson, ref jsonstr, ref invoiceCheckResult, 2, "手动查验方式");
                }
                catch (Exception ex)
                {
                    invoiceCheckResult.errcode = "20000";
                    invoiceCheckResult.description = ex.Message;
                    InvoiceLogger.WriteToDB("手动查验异常退出:"+ ex.Message, invoiceCheckResult.errcode,"", "", "", logjson, "");
                }
            }
            invoiceCheckResult.CheckDetailList.Add(invoiceCheckDetail);
            return invoiceCheckResult;
        }

        private static InvoiceCheckDetail KingdeeCheck(string token, ref InvoiceCheckDetail item, AuthData authData, ref string logjson, ref string jsonstr, ref InvoiceCheckResult invoiceCheckResult, int type, string fileName = "")
        {
            ReciveData recive = new ReciveData();
            //转验真json字符串
            jsonstr = JsonConvert.SerializeObject(authData);
            try
            {
                //获取查验结果
                FileLogger.WriteLog("开始调用金蝶发票查验接口", 1, "KingDeeApi", "Run", "DataService", "AppMessage");
                jsonstr = PostJson(ApiUtil.BaseUrl + ApiUtil.TextCheckUrl + token, jsonstr);
                FileLogger.WriteLog("金蝶接口查验结果" + jsonstr, 1, "KingDeeApi", "Run", "DataService", "AppMessage");
                jsonstr = jsonstr.Replace("'", "");
                FileLogger.WriteLog("结束调用金蝶发票查验接口", 1, "KingDeeApi", "Run", "DataService", "AppMessage");
                //保存到日志的验真结果
                logjson = jsonstr;
                recive = GetCheckResult(jsonstr);

                //验真状态 识别成功都有状态
                item.checkErrcode = recive?.errcode == null ? "" : recive.errcode;
                item.checkDescription = recive?.description == null ? "" : recive.description;
                //避免验真不通过之后，获取null值发生异常
                item.serialNo = recive?.data?.serialNo == null ? "" : recive.data.serialNo;
                item.salerName = recive?.data?.salerName == null ? "" : recive.data.salerName;
                item.salerAccount = recive?.data?.salerAccount == null ? "" : recive.data.salerAccount;
                item.amount = recive?.data?.amount == null ? "" : recive.data.amount;
                item.buyerTaxNo = recive?.data?.buyerTaxNo == null ? "" : recive.data.buyerTaxNo;
                item.taxAmount = recive?.data?.taxAmount == null ? "" : recive.data.taxAmount;

                //手动查验没有识别数据
                if (type == 2)
                {
                    item.invoiceType = recive?.data?.invoiceType == null ? "" : recive.data.invoiceType;
                    item.invoiceCode = recive?.data?.invoiceCode == null ? "" : recive.data.invoiceCode;
                    item.invoiceNo = recive?.data?.invoiceNo == null ? "" : recive.data.invoiceNo;
                    item.invoiceDate = recive?.data?.invoiceDate == null ? "" : recive.data.invoiceDate;
                    item.invoiceMoney = recive?.data?.invoiceMoney == null ? "" : recive.data.invoiceMoney;
                    item.checkCode = recive?.data?.checkCode == null ? "" : recive.data.checkCode;
                    item.totalAmount = recive?.data?.totalAmount == null ? "" : recive.data.totalAmount;
                    item.taxRate = recive?.data?.taxRate == null ? "" : recive.data.taxRate;
                    item.taxAmount = recive?.data?.taxAmount == null ? "" : recive.data.taxAmount;
                    item.serialNo = recive?.data?.serialNo == null ? "" : recive.data.serialNo;
                    item.salerName = recive?.data?.salerName == null ? "" : recive.data.salerName;
                    item.salerAccount = recive?.data?.salerAccount == null ? "" : recive.data.salerAccount;
                    item.amount = recive?.data?.amount == null ? "" : recive.data.amount;
                    item.electronicTicketNum = recive?.data?.electronicTicketNum == null ? "" : recive.data.electronicTicketNum;
                    item.printingSequenceNo = recive?.data?.printingSequenceNo ?? "";

                }
                item.buyerTaxNo = recive?.data?.buyerTaxNo == null ? "" : recive.data.buyerTaxNo;
                //税率
                if (taxtype.Contains(item.invoiceType))
                {
                    if (recive?.data?.items != null)
                    {
                        item.taxRate = recive?.data.items[0].taxRate == null ? "" : recive.data.items[0].taxRate;
                    }

                }

                if (item.invoiceType.Trim().Length != 0)
                {
                    //发票代码转具体发票
                    item.invoiceType = Enum.GetName(typeof(InvoiceType), int.Parse(item.invoiceType));
                }

                //设置查验结果
                if (recive?.errcode == "0000")
                {
                    if (recive?.data?.cancelMark == "N")
                    {
                        item.checkStatus = "通过";
                        //在加一次判断，免税的发票，设置0%，没有税率的也设置0%
                        if (item.taxAmount.Trim().Length > 0)
                        {
                            //0.00
                            if (double.Parse(item.taxAmount) == 0.00)
                            {
                                item.taxRate = "0%";
                            }
                        }
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

            }
            catch (Exception ex)
            {
                item.checkErrcode = "10001";
                item.checkDescription = "查验失败";
                //添加发票
                //发票代码转具体发票
                if (item.invoiceType!=null)
                {
                    //发票代码转具体发票
                    item.invoiceType = Enum.GetName(typeof(InvoiceType), int.Parse(item.invoiceType));
                }
                //发票识别加查验的需要添加异常发票
                if (type == 1)
                {
                    invoiceCheckResult.CheckDetailList.Add(item);
                }
                InvoiceLogger.WriteToDB("验真异常:" + ex.Message+ex.StackTrace??""+ex.InnerException??"", invoiceCheckResult.errcode, "", invoiceCheckResult.description, fileName, logjson, item.invoiceType);
            }

            return item;
        }
        /// <summary>
        /// 识别与查验（需要查验的自动查验）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="base64String">base64 字符串 </param>
        /// <returns></returns>
        public static InvoiceCheckResult Check(string fileName, string base64string)
        {


            string jsonstr = "", logjson = "";
            bool authflag = false;

            //识别 + json 查验
            InvoiceCheckResult invoiceCheckResult = new InvoiceCheckResult() { CheckDetailList = new List<InvoiceCheckDetail>() };
            try
            {
                string token = GetAccessToken();
                //image和pdf用base64识别
                disData = PostImage(ApiUtil.BaseUrl + ApiUtil.ImgDistguishUrl + token, base64string);
                logjson = disData;
                InvoiceDisResult invoiceDisResult = GetDisResult(disData);
                invoiceCheckResult.errcode = invoiceDisResult.errcode;
                invoiceCheckResult.description = invoiceDisResult.description;
                //不需要验真的状态

                //识别成功
                if (invoiceDisResult.errcode == "0000" || invoiceDisResult.errcode == "10300")
                {
                    for (int i = 0; i < invoiceDisResult.data.Count; i++)
                    {
                        InvoiceCheckDetail item = invoiceDisResult.data[i];
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
                        item.printingSequenceNo = item.printingSequenceNo == null ? "" : item.printingSequenceNo;
                        item.electronicTicketNum = item.electronicTicketNum == null ? "" : item.electronicTicketNum;
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
                                InvoiceLogger.WriteToDB("发票查验条件不满足", $"{invoiceCheckResult.errcode}", "", $"{invoiceCheckResult.description}", fileName, logjson, item.invoiceType);
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
                            KingdeeCheck(token, ref item, authData, ref logjson, ref jsonstr, ref invoiceCheckResult, 1, fileName);
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
                        //在加一次判断，免税的发票，设置0%，没有税率的也设置0%
                        if (item.taxAmount.Trim().Length>0)
                        {
                            //0.00
                            if (double.Parse(item.taxAmount)==0.00)
                            {
                                item.taxRate = "0%";
                            }
                        }
                        //添加发票
                        invoiceCheckResult.CheckDetailList.Add(item);

                    }
                }
                else
                {
                    InvoiceLogger.WriteToDB("识别非正常情况日志", invoiceCheckResult.errcode, "", invoiceCheckResult.description, fileName, disData);
                }
            }
            catch (Exception ex)
            {

                //有时候基础连接会已被意外关闭，接口下次可以正常查验
                //意外关闭无错误码 通常是发票无法识别
                if (invoiceCheckResult.description.Contains("意外关闭"))
                {
                    invoiceCheckResult.errcode = "0310";
                    invoiceCheckResult.description = "识别验真时发生异常" + ex.Message;
                }
                else
                {
                    invoiceCheckResult.errcode = "20000";
                    invoiceCheckResult.description = "识别验真时发生异常" + ex.Message;
                }

                InvoiceLogger.WriteToDB("识别验真时发生异常:" + ex.Message, invoiceCheckResult.errcode, "", invoiceCheckResult.description, fileName);
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
            request.Timeout = 20 * 1000;//设置超时

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


        // 发票流水号

        public string serialNo { get; set; }

        public string invoiceCode { get; set; }
        public string invoiceNo { get; set; }
        public string invoiceDate { get; set; }
        public string salerName { get; set; }
        public string amount { get; set; }
        public string taxAmount { get; set; } = "";
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
        public string taxRate { get; set; } = "0%";

        //税率
        public List<TaxRate> items { get; set; }

        public string Note { get; set; }
    }

    public class TaxRate
    {
        public string taxRate { get; set; }
    }
    #endregion
}

