using iTR.Lib;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;


namespace Invoice.Utils
{
    /// <summary>
    /// 写日志类
    /// </summary>
    public class InvoiceLogger
    {

        public static void WriteToDB(string log, string fErrorCode = "",string fCheckErrcode="", string fDescription = "", string fFileName = "", string fJsonData = "", string fInvoiceType = "", string type = "InvoiceMessage", string caller = "", string method = "")
        {
            try
            {
                string sql = $"Insert Into [DataService].[dbo].[InvoiceLogs](FLog,FErrorCode,FCheckErrcode,FDescription,FFileName,FJsonData,FInvoiceType,FCaller,FMethod)Values('{log}','{fErrorCode}','{fCheckErrcode}','{fDescription}','{fFileName}','{fJsonData}','{fInvoiceType}','{caller}','{method}')";
                SQLServerHelper runner = new SQLServerHelper();
                runner.ExecuteSqlNone(sql);
                runner = null;
            }
            catch (Exception err)
            {
                throw err;
            }

        }



    }
}
