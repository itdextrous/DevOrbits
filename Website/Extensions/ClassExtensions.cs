using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MyVoltage.Extensions
{
    public static class ClassExtensions
    {
        public static string GetDescription<T>(this T enumerationValue)
    where T : struct
        {
            Type type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                return "";
            }
            //Tries to find a DescriptionAttribute for a potential friendly name
            //for the enum
            MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    //Pull out the description value
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            //If we have no description attribute, just return the ToString of the enum
            return enumerationValue.ToString();
        }

        public static string ToBoolean(this bool? value, bool? isDetaultTrue = null)
        {
            if (value.HasValue)
            {
                if (value.Value)
                    return "Yes";
                else
                    return "No";
            }
            if (isDetaultTrue.HasValue)
            {
                if (isDetaultTrue.Value)
                    return "Yes";
                else
                    return "No";
            }
            return "Unknown";
        }

        public static string ToIsOnlineStatus(this bool? value)
        {
            if (value.HasValue)
                return value.Value.ToIsOnlineStatus();

            return "!UNKNOWN!";
        }

        public static string ToIsOnlineStatus(this bool value)
        {
            if (value)
                return "Online";
            else
                return "Offline";
        }

        public static string ToBoolean(this bool value)
        {
            if (value)
                return "Yes";
            else
                return "No";
        }

        public static string ToActiveStatus(this bool value)
        {
            if (value)
                return "Active";
            else
                return "Inactive";
        }

        public static string ToActiveStatus(this bool? value)
        {
            if (value.HasValue)
                return value.Value.ToActiveStatus();
            else
                return "Not Set";
        }

        public static string ToAutoDisconnectStatus(this bool value)
        {
            if (value)
                return "Auto";
            else
                return "Manual";
        }

        public static string ToContactorState(this bool value)
        {
            if (value)
                return "Connected";
            else
                return "Disconnected";
        }

        public static string ToContactorState(this bool? value)
        {
            if (value.HasValue)
                return value.Value.ToContactorState();
            else
                return "";
        }

        public static string ToDecimal(this decimal value, int decimalPlaces)
        {
            return value.ToString($"N{decimalPlaces}");
        }

        public static string ToDecimal(this decimal? value, int decimalPlaces, string blankValue = "")
        {
            if (value.HasValue)
                return value.Value.ToDecimal(decimalPlaces);
            else
                return blankValue;
        }

        public static string ToReading(this decimal value, string unit = "")
        {
            return value.ToString("N0") + (!string.IsNullOrEmpty(unit) ? " " + unit : "");
        }

        public static string ToReading(this decimal? value, string unit = "", string blankValue = "")
        {
            if (value.HasValue)
                return value.Value.ToReading(unit);
            else
                return blankValue;
        }

        public static string ToMoney(this decimal value, string unit = "", bool showZero = true)
        {
            if (!showZero && value == 0)
                return "-";

            return (!string.IsNullOrEmpty(unit) ? unit + " " : "") + value.ToString("N2");
        }

        public static string ToMoney(this decimal? value, string unit = "", string blankValue = "")
        {
            if (value.HasValue)
                return value.Value.ToMoney(unit);
            else
                return blankValue;
        }

        public static string ToMoneyNoDecimal(this decimal value, string unit = "")
        {
            return (!string.IsNullOrEmpty(unit) ? unit + " " : "") + value.ToString("N0");
        }

        public static string ToMoneyNoDecimal(this decimal? value, string unit = "", string blankValue = "")
        {
            if (value.HasValue)
                return value.Value.ToMoneyNoDecimal(unit);
            else
                return blankValue;
        }

        public static string ToDateAndTimeShort(this DateTime? value, bool returnBlank = false, string timeLineBreak = " ")
        {
            if (value.HasValue)
            {
                return value.Value.ToDateAndTimeShort(timeLineBreak);
            }
            if (returnBlank)
                return "";

            return "Unknown";
        }

        public static string ToDateAndTimeShort(this DateTime value, string timeLineBreak = " ")
        {
            return value.ToString($"yyyy-MM-dd{timeLineBreak}HH:mm");
        }


        public static string ToDateShort(this DateTime? value, bool returnBlank = false)
        {
            if (value.HasValue)
            {
                return value.Value.ToDateShort();
            }
            if (returnBlank)
                return "";

            return "Unknown";
        }

        public static string ToDateShort(this DateTime value)
        {
            return value.ToString("yyyy-MM-dd");
        }

        public static string ToMonth(this DateTime? value, bool returnBlank = false)
        {
            if (value.HasValue)
            {
                return value.Value.ToMonth();
            }
            if (returnBlank)
                return "";

            return "Unknown";
        }

        public static string ToMonth(this DateTime value)
        {
            return value.ToString("yyyy MMM");
        }

        public static string ToXML<T, Y>(this Y obj)
        {
            var xml = "";
            try
            {
                XmlSerializer xBalanceNotificationSMSConfig = new XmlSerializer(typeof(T));

                using (var sww = new StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(sww))
                    {
                        xBalanceNotificationSMSConfig.Serialize(writer, obj);
                        xml = sww.ToString(); // Your XML
                    }
                }
            }
            catch { }
            return xml;

        }

        public static T ToObject<T>(this string XML)
        {
            try
            {
                XmlSerializer xBalanceNotificationSMSConfig = new XmlSerializer(typeof(T));

                var srr = new StringReader(XML);

                return (T)xBalanceNotificationSMSConfig.Deserialize(srr);
            }
            catch { }
            return default(T);
        }

        public static DateTime AddWorkdays(this DateTime originalDate, int workDays)
        {
            DateTime tmpDate = originalDate;
            while (workDays > 0)
            {
                tmpDate = tmpDate.AddDays(1);
                if (tmpDate.DayOfWeek < DayOfWeek.Saturday &&
                    tmpDate.DayOfWeek > DayOfWeek.Sunday &&
                    !tmpDate.IsHoliday())
                    workDays--;
            }
            return tmpDate;
        }

        public static bool IsHoliday(this DateTime originalDate)
        {
            // INSERT YOUR HOlIDAY-CODE HERE!
            return false;
        }

        public static string RemoveIllegalFilenameChars(this string filename)
        {
            string newfileName = filename;

            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                newfileName = newfileName.Replace(c.ToString(), string.Empty);

            foreach (var c in System.IO.Path.GetInvalidPathChars())
                newfileName = newfileName.Replace(c.ToString(), string.Empty);

            return newfileName;
        }
    }
}
