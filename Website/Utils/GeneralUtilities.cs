using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Utils
{
    public class GeneralUtilities
    {
        public static string StandardShortDate(DateTime date)
        {
            return date.ToString("dd MMM");
        }
    }
}
