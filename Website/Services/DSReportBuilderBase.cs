using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class DSReportBuilderBase
    {
        public void AddPart(object part, ref StringBuilder stringBuilder)
        {
            string stringPart = part == null ? "" : part.ToString().Replace(",", " ").Replace(";", " ").Replace("\n", " ");

            stringBuilder.Append(stringPart);
            stringBuilder.Append(";");
        }

        public void AddPart(int part, ref StringBuilder stringBuilder)
        {
            AddPart(part.ToString(), ref stringBuilder);
        }

        public void AddPart(decimal part, ref StringBuilder stringBuilder)
        {
            AddPart(part.ToString("#.##"), ref stringBuilder);
        }

        public string FormatNumberDate(int number, DateTime date)
        {
            return number + " (" + Utils.GeneralUtilities.StandardShortDate(date) + ")";
        }
    }
}
