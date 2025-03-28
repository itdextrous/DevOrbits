using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using GemBox.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Utils
{
    public class CExcel
    {
        public class TaxInvoiceTemplate
        {
            public DateTime Month { get; set; }
            public string Address1 { get; set; }
            public string Address2 { get; set; }
            public string Address3 { get; set; }
            public string Address4 { get; set; }
            public string Address5 { get; set; }
            public string AccountName { get; set; }
            public string SupplyAddress { get; set; }
            public string VatNo { get; set; }
            public string InvoiceNo { get; set; }
            public string CustomerNo { get; set; }
            public string TaxDate { get; set; }
            public string PaymentDueDate { get; set; }
            public decimal BalanceDue { get; set; }
            public decimal BalanceBroughForward { get; set; }
            public decimal PaymentsReceived { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal ClosingBalance { get; set; }
            public decimal CurrentCharges
            {
                get
                {

                    if (this.Items != null && this.Items.Count > 0)
                        return (this.Items.Select(p => p.TotalExVAT).Sum() * -1.0m);
                    else
                        return 0;
                }
            }
            public decimal VATOnCurrentCharges { get; set; }
            public decimal Total
            {
                get
                {
                    return CurrentCharges;
                }
            }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public List<TaxInvoiceTemplateItem> Items { get; set; }
            public class TaxInvoiceTemplateItem
            {
                public string Description { get; set; }
                public string MeterSerial { get; set; }
                public decimal OpeningReading { get; set; }
                public decimal ClosingReading { get; set; }
                public decimal Consumption { get { return this.ClosingReading - this.OpeningReading; } }
                public decimal Tariff { get { return Consumption > 0 ? TotalExVAT / Consumption : 0; } }
                public decimal TotalExVAT { get; set; }

                public Api.SkyBill.TenantConsumptionStatementItem.ResourceType ItemResourceType { get; set; }

            }

            public List<TaxInvoiceTemplateLedgerItem> LedgerItems { get; set; }
            public class TaxInvoiceTemplateLedgerItem
            {
                public string Description { get; set; }
                public DateTime Date { get; set; }
                public decimal TotalExVAT { get; set; }
            }

            public string RecipientName { get; set; }
            public string RecipientAddress { get; set; }
            public string RecipientVATNumber { get; set; }
            public string RecipientReferenceNumber { get; set; }

            public string NetcashBankName { get; set; }
            public string NetcashBankAccountType { get; set; }
            public string NetcashBankAccountNo { get; set; }
            public string NetcashBankBranchCode { get; set; }

            public string SupplierName { get; set; }
            public string SupplierAddress { get; set; }
            public string SupplierVATNumber { get; set; }
            public string SupplierPostal { get; set; }
            public string SupplierPhone { get; set; }
            public string SupplierURL { get; set; }
        }

        public static void GenerateTaxInvoice(string destinationFileName, TaxInvoiceTemplate taxInvoiceTemplate, string templateFileName)
        {
            var template = new ClosedXML.Report.XLTemplate(templateFileName);

            template.AddVariable(taxInvoiceTemplate);
            template.Generate();

            template.SaveAs(destinationFileName);
        }

        public static Stream GenerateTaxInvoice(TaxInvoiceTemplate taxInvoiceTemplate, string templateFileName, bool fillCells = true, string logoPATH = "")
        {
            var template = new ClosedXML.Report.XLTemplate(templateFileName);

            template.AddVariable(taxInvoiceTemplate);
            template.Generate();

            var itemDescirptions = taxInvoiceTemplate.Items.Select(p => p.Description).ToList();
            var ledgerItemDescirptions = taxInvoiceTemplate.LedgerItems.Select(p => p.Description).ToList();

            foreach (var ws in template.Workbook.Worksheets)
            {
                foreach (var row in ws.Rows())
                {
                    foreach (var cell in row.Cells())
                    {
                        if (cell.Value == null)
                        {
                            cell.Value = "";
                            continue;
                        }
                        string cellValue = cell.Value.ToString();
                        if (itemDescirptions.Contains(cellValue)
                            || ledgerItemDescirptions.Contains(cellValue))
                        {
                            row.Height = 30;
                        }
                    }

                }
                if (fillCells)
                {
                    ws.Cells("A1:K100").Style.Fill.BackgroundColor = XLColor.White;
                    if (templateFileName.ToUpper().Contains("TaxInvoiceTemplate.xlsx".ToUpper()))
                        ws.Cells("F9:H11").Style.Fill.BackgroundColor = XLColor.FromHtml("#F79646");
                }

                ws.PageSetup.PrintAreas.Add($"A1:K{ws.LastRowUsed().RowNumber()}");
            }

            var wsForImage = template.Workbook.Worksheet(1);
            if (!string.IsNullOrEmpty(logoPATH))
            {
                var image = wsForImage.AddPicture(logoPATH)
                                    .MoveTo(wsForImage.Cell("B2"));

                //.Scale(0.5); // optional: resize picture
                image.Height = 100;
                image.Width = 250;
            }
            else
            {
                var image = wsForImage.AddPicture(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "DefaultLogo.jpg"))
                                    .MoveTo(wsForImage.Cell("B2"));
                //.Scale(0.5); // optional: resize picture
                image.Height = 100;
                image.Width = 250;
            }

            Stream stream = new MemoryStream();
            template.SaveAs(stream);

            return stream;
        }

        public static DataTable GetDataTableFromExcelTable(ClosedXML.Excel.IXLTable xLTable)
        {
            DataTable dataTable = new DataTable(xLTable.Name);

            foreach (var column in xLTable.HeadersRow().CellsUsed())
            {
                dataTable.Columns.Add(column.Value.ToString());
            }

            foreach (var row in xLTable.RowsUsed())
            {
                if (row.FirstCell().Value == xLTable.HeadersRow().FirstCellUsed().Value)
                    continue;
                //else if (row.FirstCell().Value.ToString().ToUpper().Trim() == "TOTAL")
                //    continue;

                DataRow drNew = dataTable.NewRow();
                int index = 0;
                foreach (var cell in row.Cells())
                {
                    try
                    {
                        drNew[index] = cell.Value;
                    }
                    catch { }
                    index++;
                }
                dataTable.Rows.Add(drNew);
                dataTable.AcceptChanges();
            }

            return dataTable;
        }
    }
}
