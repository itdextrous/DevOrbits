using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.Globalization;

namespace MyVoltage.Services
{
    public class ExcelConverter : DSReportBuilderBase
    {
        public const string Excel2007MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public static Color HeaderColor = Color.FromArgb(91, 155, 213);
        public static Color EvenColor = Color.FromArgb(189, 215, 238);
        public static Color OddColor = Color.FromArgb(221, 235, 247);
        public static Color SafeColor = Color.FromArgb(198, 239, 206);
        public static Color MediumColor = Color.FromArgb(255, 235, 156);
        public static Color DangerColor = Color.FromArgb(255, 199, 206);

        public byte[] GenerateExcelFileFromCsv(string csv, string sheetName)
        {
            List<int> integerColumns = new List<int>();
            List<int> decimalColumns = new List<int>();

            using (ExcelPackage package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add(sheetName);
                ExcelWorksheet ws = package.Workbook.Worksheets[0];
                ws.Name = sheetName;

                string[] lines = csv.Split('\n');

                int cellCount = 0;

                // Get Column Count
                for (int i = 0; i < lines.Length; i++) // Each Row
                {
                    string[] values = lines[i].Replace("\r", "").Split(',');

                    if (values.Length < 3)
                        continue;

                    int colWidth = values.Where(tbl => tbl.Length > 0).Count();

                    if (colWidth > cellCount)
                    {
                        cellCount = colWidth;
                    }
                }

                // Determine datatype for each column
                for (int j = 0; j < cellCount; j++) // Each Column
                {
                    bool isInteger = true;
                    bool isDecimal = true;

                    for (int k = 1; k < lines.Length-1; k++) // Each row excluding the header names
                    {
                        string[] values = lines[k].Replace("\r", "").Split(',');

                        int itemValue;
                        decimal decimalValue;
                          
                        var newValue = values[j].Replace((char)1, ',');

                        if (newValue != null)
                        {                        
                            if (Decimal.TryParse(newValue, NumberStyles.Float, CultureInfo.CurrentCulture, out decimalValue))
                            {
                                if (newValue.Length < 10) //A 32-bit int cannot hold 10 full digits
                                {
                                    if (int.TryParse(newValue, out itemValue))
                                    {
                                        if ((newValue.ToString()[0] != '0') || (newValue.ToString()[0] == '0' && newValue.Length == 1))
                                        {
                                            string[] headings = lines[0].Replace("\r", "").Split(',');

                                            if (!headings[j].ToUpper().Contains("ID"))
                                            {
                                                if (isInteger != false)
                                                {
                                                    isInteger = true;
                                                }
                                            }
                                            else
                                            {
                                                isInteger = false;
                                            }

                                            isDecimal = false;
                                        }
                                    }
                                    else
                                    {
                                        isInteger = false;

                                        if (isDecimal != false)
                                        {
                                            isDecimal = true;
                                        }
                                    }
                                }
                                else
                                {
                                    isDecimal = false;
                                    isInteger = false;
                                }                              
                            }
                            else
                            {
                                isDecimal = false;
                                isInteger = false;
                            }
                        }

                    }

                    if (isInteger == true)
                    {
                        integerColumns.Add(j);
                    }

                    if (isDecimal == true)
                    {
                        decimalColumns.Add(j);
                    }
                }


                for (int m = 0; m < lines.Length; m++)
                {
                    string[] values = lines[m].Replace("\r", "").Split(',');

                    if (values.Length < 3)
                        continue;

                    for (int n = 0; n < values.Length; n++)
                    {
                        ExcelRange cell = ws.Cells[m + 1, n + 1];
                        var newValue = values[n].Replace((char)1, ',');
                        DateTime newDate = new DateTime();

                        if (m == 0)
                        {
                            cell.Value = newValue;
                        }
                        else
                        {
                            if (newValue != null)
                            {
                                if (integerColumns.Contains(n))
                                {
                                    int itemValue;

                                    if (int.TryParse(newValue, out itemValue))
                                    {
                                        if (newValue.ToString()[0] == '0' && newValue.Length == 1)
                                        {
                                            cell.Value = itemValue;
                                        }
                                        else if (newValue.ToString()[0] == '0')
                                        {
                                            cell.Value = newValue;

                                        }
                                        else
                                        {
                                            cell.Value = itemValue;
                                        }
                                    }
                                    else
                                    {
                                        cell.Value = newValue;
                                    }
                                }
                                else if (decimalColumns.Contains(n))
                                {
                                    decimal decimalValue;

                                    if (Decimal.TryParse(newValue, NumberStyles.Float, CultureInfo.CurrentCulture, out decimalValue))
                                    {
                                        cell.Value = decimalValue;
                                    }
                                    else
                                    {
                                        cell.Value = newValue;
                                    }
                                }
                                else if (DateTime.TryParse(newValue, out newDate))
                                {
                                    cell.Value = (decimal)newDate.ToOADate();
                                    cell.Style.Numberformat.Format = "yyyy/dd/mm hh:mm:ss AM/PM";
                                }
                                else
                                {
                                    cell.Value = newValue;
                                }
                            }
                            else
                            {
                                if (integerColumns.Contains(n))
                                {
                                    cell.Value = 0;
                                }
                                else if (decimalColumns.Contains(n))
                                {
                                    cell.Value = 0.0;
                                }
                                else
                                {
                                    cell.Value = "";
                                }

                            }
                        }
                        cell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Hair;
                        cell.Style.Border.Right.Color.SetColor(Color.White);
                    }

                }
                ExcelRange range = ws.Cells[1, 1, lines.Length, cellCount];

                range.AutoFilter = true;
                range.AutoFitColumns();

                ExcelRange headerRange = ws.Cells[1, 1, 1, cellCount];
                SetRangeStyle(headerRange, HeaderColor, Color.White, true);

                for (int i = 2; i <= lines.Length; i++)
                {
                    ExcelRange rowRange = ws.Cells[i, 1, i, cellCount];

                    if (i % 2 == 0)
                    {
                        SetRangeStyle(rowRange, EvenColor, Color.Black, false);
                    }
                    else
                    {
                        SetRangeStyle(rowRange, OddColor, Color.Black, false);
                    }
                }

                return package.GetAsByteArray();
            }
        }

        private void SetRangeStyle(ExcelRange range, Color backgroundColor, Color fontColor, bool bold)
        {
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(backgroundColor);
            range.Style.Font.Color.SetColor(fontColor);
            range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Hair, Color.White);

            if (bold)
                range.Style.Font.Bold = true;
        }
    }
}
