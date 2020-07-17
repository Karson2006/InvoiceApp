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
  
}
