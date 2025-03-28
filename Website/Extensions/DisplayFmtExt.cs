using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Extensions
{
    public static class DisplayFmtExt
    {
        public static string Date(this object value)
        {
            try
            {
                return Convert.ToDateTime(value).ToString("yyyy-MM-dd");
            }

            catch
            {
                return "";
            }
        }
        public static string DateTime(this object value)
        {
            try
            {
                return Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss");
            }

            catch
            {
                return "";
            }
        }

        public static string Money(this object value)
        {
            try
            {
                return Convert.ToDecimal(value).ToString("N");
            }

            catch
            {
                return "";
            }
        }
        public static string Booloean(this object value)
        {
            try
            {
                if (Convert.ToBoolean(value))
                    return "Yes";
                else
                    return "No";
            }

            catch
            {
                return "";
            }
        }
    }
}
