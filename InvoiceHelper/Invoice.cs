using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTR.OP.Invoice
{

    public class Invoice
    {

        //[SugarColumn(ColumnName = "formmain_id")]
        public string FormmainId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        //[SugarColumn(ColumnName = "field0012")]
        public string Field0012 { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        //[SugarColumn(ColumnName = "field0014")]
        public string Field0014 { get; set; }
        /// <summary>
        /// 代码
        /// </summary>
        public string Field0015 { get; set; }
        /// <summary>
        /// 号码
        /// </summary>
        public string Field0016 { get; set; }
        /// <summary>
        /// 校验码
        /// </summary>
        public string Field0032 { get; set; }
        /// <summary>
        /// 开票日期
        /// </summary>
        public string Field0017 { get; set; }
        /// <summary>
        /// 税金
        /// </summary>
        public string Field0026 { get; set; }
        /// <summary>
        /// 开票金额
        /// </summary>
        public string Field0019 { get; set; }
        /// <summary>
        /// 查验次数
        /// </summary>
        public string Field0033 { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public string Field0027 { get; set; }
        /// <summary>
        /// 开票机构
        /// </summary>
        public string Field0018 { get; set; }
        /// <summary>
        /// 是否是发票
        /// </summary>
        public string Field0039 { get; set; }
        /// <summary>
        /// 结果
        /// </summary>
        public string Field0025 { get; set; }
    }
}
