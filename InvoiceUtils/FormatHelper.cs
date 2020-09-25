using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Invoice.Utils
{
    public class FormatHelper
    {
        public static JObject JsonToObject(string jsonstr)
        {
            return JObject.Parse(jsonstr);
        }
        public static string ObjectToJson(JObject jObject)
        {
            return JsonConvert.SerializeObject(jObject);
        }
        public static string FileToBase64(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
            BinaryReader br = new BinaryReader(fs, utf8);
            byte[] bytes = br.ReadBytes((int)fs.Length);
            fs.Flush();
            fs.Close();
            string b64str = Convert.ToBase64String(bytes);
            return b64str;
        }
        public static bool Base64ToFile(string b64str)
        {
            Byte[] bytes = Convert.FromBase64String(b64str);
            try
            {
                // File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                // File.WriteAllBytes(path, bytes);
                throw ex;
            }
            
            return false;
        }


    }
    //1.普通电子发票2.电子发票专票（暂无）3.普通纸质发票4.专用纸质发票5.普通纸质卷票7.通用机打8.的士票9.火车票10.飞机票11.其他12.机动车.13.二手车14.定额发票15.通行费16.客运发票17.过路过桥费18.车船税发票（专票）19.完税证明 20.轮船票 21.酒店账单
    public enum InvoiceType
    {
        普通电子发票 = 1,
        电子发票专票 = 2,
        普通纸质发票 = 3,
        专用纸质发票 =4,
        普通纸质卷票 =5,
        //没有6
        通用机打 =7,
        的士票 =8,
        火车票=9,
        飞机票=10,
        其他=11,
        机动车=12,
        二手车 =13,
        定额发票=14,
        通行费 =15,
        客运发票 =16,
        过路过桥费 =17,
        车船税发票专票=18,
        完税证明=19,
        轮船票 =20,
        酒店账单=21
    }

}
