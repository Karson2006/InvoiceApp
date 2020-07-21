using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using InvoiceUtils.OAAttachment;
using iTR.Lib;
using Exception = System.Exception;
namespace Invoice.Utils
{
    public class EncrptionUtil
    {




        public static string GetMD5Str(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
                // To force the hex string to lower-case letters instead of
                // upper-case, use he following line instead:
                // sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString().ToLower();
        }

        /// <summary>
        /// 有密码的AES加密 
        /// </summary>
        /// <param name="text">加密字符</param>
        /// <param name="password">加密的密码</param>
        /// <param name="iv">密钥</param>
        /// <returns></returns>
        public static string encrypt(string content, string password)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(password);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(content);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="text"></param>
        /// <param name="password"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static string decrypt(string content, string password)
        {

            try
            {

                byte[] keyArray = UTF8Encoding.UTF8.GetBytes(password);
                byte[] toEncryptArray = Convert.FromBase64String(content);

                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return UTF8Encoding.UTF8.GetString(resultArray);
            }
            catch (Exception ex)
            {
                return ex.Message;

            }

        }
        /// <summary>
        /// 附件解密
        /// </summary>
        /// <param name="inFileName">待解密文件</param>
        /// <param name="outFileName">解密后文件</param>
        /// <returns></returns>
        public static bool AttDecrypt(string inFileName,string outFileName)
        {
            try
            {
                if (inFileName.Trim().Length==0|| outFileName.Trim().Length == 0)
                {
                    return false;
                }
                AttMainClient attMain = new AttMainClient();
                attMain.decrypt(inFileName, outFileName);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        /// <summary>
        /// 附件加密
        /// </summary>
        /// <param name="inFileName">待加密文件</param>
        /// <param name="outFileName">加密后文件</param>
        /// <param name="type">加密方式：0轻度加密，1深度加密</param>
        /// <returns></returns>
        public static bool AttEncrypt(string inFileName, string outFileName, int t)
        {
            string type = "";
            if (inFileName.Trim().Length == 0 || outFileName.Trim().Length == 0)
            {
                return false;
            }
            if (t != 0 && t != 1)
            {
                return false;
            }
            if (t == 0)
            {
                type = "ICoder.VERSON01";
            }
            if (t == 1)
            {
                type = "ICoder.VERSON02";
            }
            try
            {
                AttMainClient attMain = new AttMainClient();
                attMain.encrypt(inFileName, outFileName, type);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
       


    }
}
