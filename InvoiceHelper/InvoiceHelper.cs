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
using Invoice.Utils;
using InvoiceUtils.OAAttachment;


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
        public InvoiceCheckResult Scan_Check(string fileName, string fileType, int timeOutSecond = 8,string mode="1", Dictionary<string,string> param=null)
        {
            InvoiceCheckResult result = null;
            try
            {
                if (mode == "1")
                {
                    //解密后的文件名
                    string decryptFileName = fileName + "_D";
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(fileName);

                    if (fileInfo.Length > 1024 * 1024 * 4)
                    {
                        result = new InvoiceCheckResult();
                        result.errcode = "333333";
                        result.description = "附件大小超过4M";
                    }
                    else
                    {
                        //若没有解过密，再先解密
                        if (!File.Exists(decryptFileName))
                        {
                            EncrptionUtil.AttDecrypt(fileName, decryptFileName);
                        }
                        byte[] bytes = bytes = GetBytesByPath(decryptFileName);
                        string base64String = Convert.ToBase64String(bytes);
                        result = KingDeeApi.Check(fileName, base64String);
                    }
                }
                else
                {
                    result = KingDeeApi.ManualCheck(param["InvoiceCode"], param["InvoiceNo"], param["InvoiceDate"], param["InvoiceMoney"], param["InvoieCheckCode"]);
                }
            }
            catch (System.Exception err)
            {
                FileLogger.WriteLog("处理文件异常： " + err.Message, 1, "InvoiceHelper", "Check_Scan", "DataService", "ErrMessage");
            }

            return result;
        }




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


}
