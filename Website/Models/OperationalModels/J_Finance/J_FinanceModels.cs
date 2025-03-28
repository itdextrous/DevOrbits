using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.J_Finance.J_FinanceModels
{
    public class J_Finance_AdministrationModel
    {
        public List<J_Finance_AdministrationItem> J_Finance_AdministrationItems { get; set; }

        public class J_Finance_AdministrationItem
        {
            public string CustomerNo { get; set; }
            public decimal? Balance { get; set; }
            public AccountTypeEnum AccountType { get; set; }
            public string MeterDescription { get; set; }
            public string SerialNo { get; set; }
            public DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public string OnlineStatus { get; set; }
            public string DisconnectionType { get; set; }
            public string ContactorState { get; set; }
        }
    }

    public class J_Finance_ReceiptLogModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [DisplayName("Customer Number")]
        public string CustomerNumber { get; set; }

        [DisplayName("Include not Successfull transactions")]
        public List<SelectListItem> IncludeFailed { get; set; }

        [DisplayName("Payment Method")]
        public List<SelectListItem> PaymentMethodID { get; set; }

        [DisplayName("Show Only Missing")]
        public List<SelectListItem> ShowOnlyMissing { get; set; }

        public int TotalEntries { get; set; }
        public List<J_Finance_ReceiptLogItem> J_Finance_ReceiptLogItems { get; set; }

        public class J_Finance_ReceiptLogItem
        {
            public DateTime CreateDate { get; set; }
            public string Reference { get; set; }
            public string PaymentMethod { get; set; }
            public int PaymentMethodID { get; set; }
            public decimal Amount { get; set; }
            public string Reason { get; set; }
            public string CustomerNumber { get; set; }
            public string FullName { get; set; }
            public string CompanyName { get; set; }
            public string SkybillCompanyName { get; set; }
            public string SkybillCustomerNo { get; set; }
            public decimal? SkybillFeeAmount { get; set; }
            public bool? FeeRequired { get; set; }
            public bool? Vending1Required { get; set; }
            public int? Vending1LogID { get; set; }
            public bool? Vending2Required { get; set; }
            public int? Vending2LogID { get; set; }
            public bool? Vending3Required { get; set; }
            public int? Vending3LogID { get; set; }
            public bool? Vending4Required { get; set; }
            public int? Vending4LogID { get; set; }
            public string SerialNumber { get; set; }

            public decimal? SkybillFeeAmount6810 { get; set; }
            public decimal? SkybillFeeAmount5611 { get; set; }
            public decimal? Vending1Amount7191 { get; set; }
            public decimal? Vending1Amount8640 { get; set; }
            public decimal? Vending1Amount5621 { get; set; }
            public decimal? Vending2Amount7191 { get; set; }
            public decimal? Vending2Amount8640 { get; set; }
            public decimal? Vending2Amount5621 { get; set; }
            public decimal? Vending3Amount7191 { get; set; }
            public decimal? Vending3Amount8640 { get; set; }
            public decimal? Vending3Amount5621 { get; set; }
            public decimal? Vending4Amount7191 { get; set; }
            public decimal? Vending4Amount8640 { get; set; }
            public decimal? Vending4Amount5621 { get; set; }
            public string Vending1SkybillCompanyName { get; set; }
            public decimal? Vending1Amount6810 { get; set; }
            public decimal? Vending1Amount5611 { get; set; }
            public string Vending2SkybillCompanyName { get; set; }
            public decimal? Vending2Amount6810 { get; set; }
            public decimal? Vending2Amount5611 { get; set; }
            public string Vending3SkybillCompanyName { get; set; }
            public decimal? Vending3Amount6810 { get; set; }
            public decimal? Vending3Amount5611 { get; set; }
            public string Vending4SkybillCompanyName { get; set; }
            public decimal? Vending4Amount6810 { get; set; }
            public decimal? Vending4Amount5611 { get; set; }
            public DateTime? SkybillCheckupDate { get; set; }
            public decimal? Vending2Amount2910 { get; set; }

            public string NSSkybillDocumentNo { get; set; }
            public int? NSPaymentID { get; set; }
            public int? NSInternalDBID { get; set; }

            public StatusType PaymentStatus
            {
                get
                {
                    if (SkybillCheckupDate.HasValue)
                    {
                        if (!string.IsNullOrEmpty(SkybillCompanyName) && !string.IsNullOrEmpty(SkybillCustomerNo))
                        {
                            return StatusType.Found;
                        }
                        else
                        {
                            return StatusType.Missing;
                        }
                    }
                    return StatusType.NotSynced;
                }
            }

            public StatusType FeeStatus
            {
                get
                {
                    if (SkybillCheckupDate.HasValue)
                    {
                        if (FeeRequired.HasValue)
                        {
                            if (FeeRequired.Value)
                            {
                                if (SkybillFeeAmount.HasValue)
                                {
                                    return StatusType.Found;
                                }
                                else
                                {
                                    return StatusType.Missing;
                                }
                            }
                            else
                            {
                                return StatusType.NA;
                            }
                        }
                    }
                    return StatusType.NotSynced;
                }
            }

            public Tuple<StatusType, decimal> Vending1Status
            {
                get
                {
                    if (PaymentMethod == PaymentMethodEnum.Retail.ToString())
                        return new Tuple<StatusType, decimal>(StatusType.NA, 0);

                    if (Vending1Required.HasValue)
                    {
                        if (Vending1Required.Value)
                        {
                            if (Vending1Amount5611.HasValue && Vending1Amount5621.HasValue && Vending1Amount6810.HasValue && Vending1Amount7191.HasValue && Vending1Amount8640.HasValue
                                && ((Vending1Amount5611.Value + Vending1Amount5621.Value + Vending1Amount6810.Value + Vending1Amount7191.Value + Vending1Amount8640.Value) != 0))
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Found, (Vending1Amount5611.Value + Vending1Amount5621.Value + Vending1Amount6810.Value + Vending1Amount7191.Value + Vending1Amount8640.Value));
                            }
                            else
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Missing, ((Vending1Amount5611.HasValue ? Vending1Amount5611.Value : 0) + (Vending1Amount5621.HasValue ? Vending1Amount5621.Value : 0) + (Vending1Amount6810.HasValue ? Vending1Amount6810.Value : 0) + (Vending1Amount7191.HasValue ? Vending1Amount7191.Value : 0) + (Vending1Amount8640.HasValue ? Vending1Amount8640.Value : 0)));
                            }
                        }
                        else
                        {
                            return new Tuple<StatusType, decimal>(StatusType.NA, 0);
                        }
                    }

                    return new Tuple<StatusType, decimal>(StatusType.NotSynced, 0);
                }
            }

            public Tuple<StatusType, decimal> Vending2Status
            {
                get
                {
                    if (
                        PaymentMethod == PaymentMethodEnum.Retail.ToString()
                        || PaymentMethod == PaymentMethodEnum.EFT.ToString()
                        || PaymentMethod == PaymentMethodEnum.iPay.ToString()
                        )
                        return new Tuple<StatusType, decimal>(StatusType.NA, 0);

                    if (Vending2Required.HasValue)
                    {
                        if (Vending2Required.Value)
                        {
                            if (Vending2Amount5611.HasValue && Vending2Amount5621.HasValue && Vending2Amount6810.HasValue && Vending2Amount7191.HasValue && Vending2Amount8640.HasValue && Vending2Amount2910.HasValue
                                && ((Vending2Amount5611.Value + Vending2Amount5621.Value + Vending2Amount6810.Value + Vending2Amount7191.Value + Vending2Amount8640.Value + Vending2Amount2910.Value) != 0))
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Found, (Vending2Amount5611.Value + Vending2Amount5621.Value + Vending2Amount6810.Value + Vending2Amount7191.Value + Vending2Amount8640.Value + Vending2Amount2910.Value));
                            }
                            else
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Missing, ((Vending2Amount5611.HasValue ? Vending2Amount5611.Value : 0) + (Vending2Amount5621.HasValue ? Vending2Amount5621.Value : 0) + (Vending2Amount6810.HasValue ? Vending2Amount6810.Value : 0) + (Vending2Amount7191.HasValue ? Vending2Amount7191.Value : 0) + (Vending2Amount8640.HasValue ? Vending2Amount8640.Value : 0) + (Vending2Amount2910.HasValue ? Vending2Amount2910.Value : 0)));
                            }
                        }
                        else
                        {
                            return new Tuple<StatusType, decimal>(StatusType.NA, 0);
                        }
                    }

                    return new Tuple<StatusType, decimal>(StatusType.NotSynced, 0);
                }
            }

            public Tuple<StatusType, decimal> Vending3Status
            {
                get
                {
                    if (
                        PaymentMethod == PaymentMethodEnum.Retail.ToString()
                        || PaymentMethod == PaymentMethodEnum.EFT.ToString()
                        || PaymentMethod == PaymentMethodEnum.iPay.ToString()
                        )
                        return new Tuple<StatusType, decimal>(StatusType.NA, 0);

                    if (Vending3Required.HasValue)
                    {
                        if (Vending3Required.Value)
                        {
                            if (Vending3Amount5611.HasValue && Vending3Amount5621.HasValue && Vending3Amount6810.HasValue && Vending3Amount7191.HasValue && Vending3Amount8640.HasValue
                                && ((Vending3Amount5611.Value + Vending3Amount5621.Value + Vending3Amount6810.Value + Vending3Amount7191.Value + Vending3Amount8640.Value) != 0))
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Found, (Vending3Amount5611.Value + Vending3Amount5621.Value + Vending3Amount6810.Value + Vending3Amount7191.Value + Vending3Amount8640.Value));
                            }
                            else
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Missing, ((Vending3Amount5611.HasValue ? Vending3Amount5611.Value : 0) + (Vending3Amount5621.HasValue ? Vending3Amount5621.Value : 0) + (Vending3Amount6810.HasValue ? Vending3Amount6810.Value : 0) + (Vending3Amount7191.HasValue ? Vending3Amount7191.Value : 0) + (Vending3Amount8640.HasValue ? Vending3Amount8640.Value : 0)));
                            }
                        }
                        else
                        {
                            return new Tuple<StatusType, decimal>(StatusType.NA, 0);
                        }
                    }

                    return new Tuple<StatusType, decimal>(StatusType.NotSynced, 0);
                }
            }

            public Tuple<StatusType, decimal> Vending4Status
            {
                get
                {
                    if (
                        PaymentMethod == PaymentMethodEnum.Retail.ToString()
                        || PaymentMethod == PaymentMethodEnum.EFT.ToString()
                        || PaymentMethod == PaymentMethodEnum.iPay.ToString()
                        )
                        return new Tuple<StatusType, decimal>(StatusType.NA, 0);

                    if (Vending4Required.HasValue)
                    {
                        if (Vending4Required.Value)
                        {
                            if (Vending4Amount5611.HasValue && Vending4Amount5621.HasValue && Vending4Amount6810.HasValue && Vending4Amount7191.HasValue && Vending4Amount8640.HasValue
                                && ((Vending4Amount5611.Value + Vending4Amount5621.Value + Vending4Amount6810.Value + Vending4Amount7191.Value + Vending4Amount8640.Value) != 0))
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Found, (Vending4Amount5611.Value + Vending4Amount5621.Value + Vending4Amount6810.Value + Vending4Amount7191.Value + Vending4Amount8640.Value));
                            }
                            else
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Missing, ((Vending4Amount5611.HasValue ? Vending4Amount5611.Value : 0) + (Vending4Amount5621.HasValue ? Vending4Amount5621.Value : 0) + (Vending4Amount6810.HasValue ? Vending4Amount6810.Value : 0) + (Vending4Amount7191.HasValue ? Vending4Amount7191.Value : 0) + (Vending4Amount8640.HasValue ? Vending4Amount8640.Value : 0)));
                            }
                        }
                        else
                        {
                            return new Tuple<StatusType, decimal>(StatusType.NA, 0);
                        }
                    }

                    return new Tuple<StatusType, decimal>(StatusType.NotSynced, 0);
                }
            }

            public enum StatusType
            {
                [Description("-")]
                NotSynced = 0,
                [Description("N/A")]
                NA = 1,
                [Description("Missing")]
                Missing = 2,
                [Description("Found")]
                Found = 3,
            }
        }
    }

    public class J_Finance_ReceiptLogExceptionsModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TotalEntries { get; set; }
        public List<J_Finance_ReceiptLogExceptionsItem> J_Finance_ReceiptLogExceptionsItems { get; set; }

        public class J_Finance_ReceiptLogExceptionsItem
        {
            public DateTime CreateDate { get; set; }
            public string Reference { get; set; }
            public string PaymentMethod { get; set; }
            public int PaymentMethodID { get; set; }
            public decimal Amount { get; set; }
            public string Reason { get; set; }
            public string CustomerNumber { get; set; }
            public string FullName { get; set; }
            public string CompanyName { get; set; }
            public string SkybillCompanyName { get; set; }
            public string SkybillCustomerNo { get; set; }
            public decimal? SkybillFeeAmount { get; set; }
            public bool? FeeRequired { get; set; }
            public bool? Vending1Required { get; set; }
            public int? Vending1LogID { get; set; }
            public bool? Vending2Required { get; set; }
            public int? Vending2LogID { get; set; }
            public bool? Vending3Required { get; set; }
            public int? Vending3LogID { get; set; }
            public bool? Vending4Required { get; set; }
            public int? Vending4LogID { get; set; }
            public string SerialNumber { get; set; }

            public decimal? SkybillFeeAmount6810 { get; set; }
            public decimal? SkybillFeeAmount5611 { get; set; }
            public decimal? Vending1Amount7191 { get; set; }
            public decimal? Vending1Amount8640 { get; set; }
            public decimal? Vending1Amount5621 { get; set; }
            public decimal? Vending2Amount7191 { get; set; }
            public decimal? Vending2Amount8640 { get; set; }
            public decimal? Vending2Amount5621 { get; set; }
            public decimal? Vending3Amount7191 { get; set; }
            public decimal? Vending3Amount8640 { get; set; }
            public decimal? Vending3Amount5621 { get; set; }
            public decimal? Vending4Amount7191 { get; set; }
            public decimal? Vending4Amount8640 { get; set; }
            public decimal? Vending4Amount5621 { get; set; }
            public string Vending1SkybillCompanyName { get; set; }
            public decimal? Vending1Amount6810 { get; set; }
            public decimal? Vending1Amount5611 { get; set; }
            public string Vending2SkybillCompanyName { get; set; }
            public decimal? Vending2Amount6810 { get; set; }
            public decimal? Vending2Amount5611 { get; set; }
            public string Vending3SkybillCompanyName { get; set; }
            public decimal? Vending3Amount6810 { get; set; }
            public decimal? Vending3Amount5611 { get; set; }
            public string Vending4SkybillCompanyName { get; set; }
            public decimal? Vending4Amount6810 { get; set; }
            public decimal? Vending4Amount5611 { get; set; }
            public DateTime? SkybillCheckupDate { get; set; }
            public decimal? Vending2Amount2910 { get; set; }

            public string NSSkybillDocumentNo { get; set; }
            public int? NSPaymentID { get; set; }
            public int? NSInternalDBID { get; set; }

            public StatusType PaymentStatus
            {
                get
                {
                    if (SkybillCheckupDate.HasValue)
                    {
                        if (!string.IsNullOrEmpty(SkybillCompanyName) && !string.IsNullOrEmpty(SkybillCustomerNo))
                        {
                            return StatusType.Found;
                        }
                        else
                        {
                            return StatusType.Missing;
                        }
                    }
                    return StatusType.NotSynced;
                }
            }

            public StatusType FeeStatus
            {
                get
                {
                    if (SkybillCheckupDate.HasValue)
                    {
                        if (FeeRequired.HasValue)
                        {
                            if (FeeRequired.Value)
                            {
                                if (SkybillFeeAmount.HasValue)
                                {
                                    return StatusType.Found;
                                }
                                else
                                {
                                    return StatusType.Missing;
                                }
                            }
                            else
                            {
                                return StatusType.NA;
                            }
                        }
                    }
                    return StatusType.NotSynced;
                }
            }

            public Tuple<StatusType, decimal> Vending1Status
            {
                get
                {
                    if (PaymentMethod == PaymentMethodEnum.Retail.ToString())
                        return new Tuple<StatusType, decimal>(StatusType.NA, 0);

                    if (Vending1Required.HasValue)
                    {
                        if (Vending1Required.Value)
                        {
                            if (Vending1Amount5611.HasValue && Vending1Amount5621.HasValue && Vending1Amount6810.HasValue && Vending1Amount7191.HasValue && Vending1Amount8640.HasValue
                                && ((Vending1Amount5611.Value + Vending1Amount5621.Value + Vending1Amount6810.Value + Vending1Amount7191.Value + Vending1Amount8640.Value) != 0))
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Found, (Vending1Amount5611.Value + Vending1Amount5621.Value + Vending1Amount6810.Value + Vending1Amount7191.Value + Vending1Amount8640.Value));
                            }
                            else
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Missing, ((Vending1Amount5611.HasValue ? Vending1Amount5611.Value : 0) + (Vending1Amount5621.HasValue ? Vending1Amount5621.Value : 0) + (Vending1Amount6810.HasValue ? Vending1Amount6810.Value : 0) + (Vending1Amount7191.HasValue ? Vending1Amount7191.Value : 0) + (Vending1Amount8640.HasValue ? Vending1Amount8640.Value : 0)));
                            }
                        }
                        else
                        {
                            return new Tuple<StatusType, decimal>(StatusType.NA, 0);
                        }
                    }

                    return new Tuple<StatusType, decimal>(StatusType.NotSynced, 0);
                }
            }

            public Tuple<StatusType, decimal> Vending2Status
            {
                get
                {
                    if (
                        PaymentMethod == PaymentMethodEnum.Retail.ToString()
                        || PaymentMethod == PaymentMethodEnum.EFT.ToString()
                        || PaymentMethod == PaymentMethodEnum.iPay.ToString()
                        )
                        return new Tuple<StatusType, decimal>(StatusType.NA, 0);

                    if (Vending2Required.HasValue)
                    {
                        if (Vending2Required.Value)
                        {
                            if (Vending2Amount5611.HasValue && Vending2Amount5621.HasValue && Vending2Amount6810.HasValue && Vending2Amount7191.HasValue && Vending2Amount8640.HasValue && Vending2Amount2910.HasValue
                                && ((Vending2Amount5611.Value + Vending2Amount5621.Value + Vending2Amount6810.Value + Vending2Amount7191.Value + Vending2Amount8640.Value + Vending2Amount2910.Value) != 0))
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Found, (Vending2Amount5611.Value + Vending2Amount5621.Value + Vending2Amount6810.Value + Vending2Amount7191.Value + Vending2Amount8640.Value + Vending2Amount2910.Value));
                            }
                            else
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Missing, ((Vending2Amount5611.HasValue ? Vending2Amount5611.Value : 0) + (Vending2Amount5621.HasValue ? Vending2Amount5621.Value : 0) + (Vending2Amount6810.HasValue ? Vending2Amount6810.Value : 0) + (Vending2Amount7191.HasValue ? Vending2Amount7191.Value : 0) + (Vending2Amount8640.HasValue ? Vending2Amount8640.Value : 0) + (Vending2Amount2910.HasValue ? Vending2Amount2910.Value : 0)));
                            }
                        }
                        else
                        {
                            return new Tuple<StatusType, decimal>(StatusType.NA, 0);
                        }
                    }

                    return new Tuple<StatusType, decimal>(StatusType.NotSynced, 0);
                }
            }

            public Tuple<StatusType, decimal> Vending3Status
            {
                get
                {
                    if (
                        PaymentMethod == PaymentMethodEnum.Retail.ToString()
                        || PaymentMethod == PaymentMethodEnum.EFT.ToString()
                        || PaymentMethod == PaymentMethodEnum.iPay.ToString()
                        )
                        return new Tuple<StatusType, decimal>(StatusType.NA, 0);

                    if (Vending3Required.HasValue)
                    {
                        if (Vending3Required.Value)
                        {
                            if (Vending3Amount5611.HasValue && Vending3Amount5621.HasValue && Vending3Amount6810.HasValue && Vending3Amount7191.HasValue && Vending3Amount8640.HasValue
                                && ((Vending3Amount5611.Value + Vending3Amount5621.Value + Vending3Amount6810.Value + Vending3Amount7191.Value + Vending3Amount8640.Value) != 0))
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Found, (Vending3Amount5611.Value + Vending3Amount5621.Value + Vending3Amount6810.Value + Vending3Amount7191.Value + Vending3Amount8640.Value));
                            }
                            else
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Missing, ((Vending3Amount5611.HasValue ? Vending3Amount5611.Value : 0) + (Vending3Amount5621.HasValue ? Vending3Amount5621.Value : 0) + (Vending3Amount6810.HasValue ? Vending3Amount6810.Value : 0) + (Vending3Amount7191.HasValue ? Vending3Amount7191.Value : 0) + (Vending3Amount8640.HasValue ? Vending3Amount8640.Value : 0)));
                            }
                        }
                        else
                        {
                            return new Tuple<StatusType, decimal>(StatusType.NA, 0);
                        }
                    }

                    return new Tuple<StatusType, decimal>(StatusType.NotSynced, 0);
                }
            }

            public Tuple<StatusType, decimal> Vending4Status
            {
                get
                {
                    if (
                        PaymentMethod == PaymentMethodEnum.Retail.ToString()
                        || PaymentMethod == PaymentMethodEnum.EFT.ToString()
                        || PaymentMethod == PaymentMethodEnum.iPay.ToString()
                        )
                        return new Tuple<StatusType, decimal>(StatusType.NA, 0);

                    if (Vending4Required.HasValue)
                    {
                        if (Vending4Required.Value)
                        {
                            if (Vending4Amount5611.HasValue && Vending4Amount5621.HasValue && Vending4Amount6810.HasValue && Vending4Amount7191.HasValue && Vending4Amount8640.HasValue
                                && ((Vending4Amount5611.Value + Vending4Amount5621.Value + Vending4Amount6810.Value + Vending4Amount7191.Value + Vending4Amount8640.Value) != 0))
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Found, (Vending4Amount5611.Value + Vending4Amount5621.Value + Vending4Amount6810.Value + Vending4Amount7191.Value + Vending4Amount8640.Value));
                            }
                            else
                            {
                                return new Tuple<StatusType, decimal>(StatusType.Missing, ((Vending4Amount5611.HasValue ? Vending4Amount5611.Value : 0) + (Vending4Amount5621.HasValue ? Vending4Amount5621.Value : 0) + (Vending4Amount6810.HasValue ? Vending4Amount6810.Value : 0) + (Vending4Amount7191.HasValue ? Vending4Amount7191.Value : 0) + (Vending4Amount8640.HasValue ? Vending4Amount8640.Value : 0)));
                            }
                        }
                        else
                        {
                            return new Tuple<StatusType, decimal>(StatusType.NA, 0);
                        }
                    }

                    return new Tuple<StatusType, decimal>(StatusType.NotSynced, 0);
                }
            }

            public enum StatusType
            {
                [Description("-")]
                NotSynced = 0,
                [Description("N/A")]
                NA = 1,
                [Description("Missing")]
                Missing = 2,
                [Description("Found")]
                Found = 3,
            }
        }
    }

    public class J_Finance_ReceiptLog_EditModel
    {
        public Data.Payment Payment { get; set; }
        public Data.Customer CurrentCustomer { get; set; }
        public string RetURL { get; set; }

        [Display(Name = "Customer")]
        [Required]
        public List<SelectListItem> Customer { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class J_Finance_SagepayAllocationExceptionsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_SagepayAllocationExceptionsItem> J_Finance_SagepayAllocationExceptionsItems { get; set; }

        public class J_Finance_SagepayAllocationExceptionsItem : Data.Payment
        {
            public bool RequiresConvFee { get; set; }
            public string CustomerNumber { get; set; }
            public string SerialNumber { get; set; }
            public string FullName { get; set; }
        }
    }

    public class J_Finance_UnipinAllocationExceptionsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_UnipinAllocationExceptionsItem> J_Finance_UnipinAllocationExceptionsItems { get; set; }

        public class J_Finance_UnipinAllocationExceptionsItem : Data.UniPin
        {
            public bool RequiresConvFee { get; set; }
            public string CustomerNumber { get; set; }
            public string FullName { get; set; }
        }
    }

    public class J_Finance_AllocationRerunModel
    {
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        [Required]
        public List<SelectListItem> CompanyID { get; set; }

        public bool IsSuccessfull { get; set; }

        public List<J_Finance_AllocationRerunItem> J_Finance_AllocationRerunItems { get; set; }
        public J_Finance_AllocationRerunItem InProgressItem
        {
            get
            {
                if (J_Finance_AllocationRerunItems != null && J_Finance_AllocationRerunItems.Count > 0)
                    return (from p in J_Finance_AllocationRerunItems
                            where !p.DateEnded.HasValue
                            && p.DateStarted.HasValue
                            && p.Progress != 100
                            orderby p.DateStarted.Value
                            select p).FirstOrDefault();
                return null;
            }
        }
        public class J_Finance_AllocationRerunItem : Data.J_Finance_AllocationRerun_Log
        {
            public string UserName { get; set; }
            public string CompanyName { get; set; }
        }
    }

    public class J_Finance_ExternalChargesModel
    {
        [DisplayName("Customer Number")]
        [Required]
        public List<SelectListItem> SkybillCustomerNos { get; set; }

        [DisplayName("Posting Date")]
        [ValidateDateRange]
        [Required]
        public DateTime PostingDate { get; set; }

        [DisplayName("Reference Number")]
        [Required]
        public string ReferenceNumber { get; set; }

        [DisplayName("Amount")]
        [ValidateAmount]
        [Required]
        public decimal Amount { get; set; }

        [DisplayName("Upload Confirmation Email")]
        [RegularExpression(@"(^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$)", ErrorMessage = "Upload Confirmation Email incorrect.")]
        [Required]
        public string UploadConfirmationEmail { get; set; }

        [DisplayName("Notify Client")]
        public bool NotifyClient { get; set; }

        [DisplayName("Upload Attachment")]
        [Required]
        public IFormFile file { get; set; }

        public string Result { get; set; }

        public bool IsSuccess { get; set; }

        public string ClientConfirmationEmail { get; set; }

        public class ValidateDateRange : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                // your validation logic
                if (Convert.ToDateTime(value) >= DateTime.Now)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult("Date may not be in the past.");
                }
            }
        }
        public class ValidateAmount : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                // your validation logic
                if (Convert.ToDecimal(value) > 0)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult("Amount must be more than 0.");
                }
            }
        }
    }

    public class J_Finance_ExternalChargesReverseModel
    {
        [DisplayName("Reference Number")]
        [Required]
        public string ReferenceNumber { get; set; }

        public string Result { get; set; }

        public bool IsSuccess { get; set; }

        public MyVoltage.Data.ExternalChargesSchedulingImport ExternalChargesSchedulingImport { get; set; }

        public string UploadURL { get; set; }
    }

    public class J_Finance_MeterReconReportModel
    {
        [Required]
        [DisplayName("Serial Number")]
        public string Serial { get; set; }
        [Required]
        [RegularExpression(@"(^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$)", ErrorMessage = "Email Address incorrect.")]
        [DisplayName("Email Address")]
        public string Email { get; set; }
        [Required]
        [DisplayName("If water meter, please select resource type below")]
        public List<SelectListItem> WaterMeterResourceType { get; set; }
        public string Result { get; set; }

    }

    public class MeterReconReport_SkybillBillingItem
    {
        public DateTime Month { get { return new DateTime(Date.Year, Date.Month, 1); } }
        public string CustomerNo { get; set; }
        public string MeterNo { get; set; }
        public string MeterSerial { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal OpeningReading { get; set; }
        public decimal ClosingReading { get; set; }
        public decimal Consumption { get { return this.ClosingReading - this.OpeningReading; } }
        public decimal Tariff { get { return Consumption > 0 ? TotalExVAT / Consumption : 0; } }
        public decimal TotalExVAT { get; set; }
    }

    public class MeterReconReport_MirrorReadingItem
    {
        public DateTime Month { get { return new DateTime(TimeLogged.Year, TimeLogged.Month, 1); } }
        public string Serial { get; set; }
        public DateTime TimeLogged { get; set; }
        public decimal VirtualOdometerReading { get; set; }
        public decimal Difference { get; set; }
    }

    public class MeterReconReport_M2MReadingItem
    {
        public string Serial { get; set; }
        public DateTime TimeLogged { get; set; }
        public List<Register> Registers { get; set; }
        public class Register
        {
            public string RegisterName { get; set; }
            public decimal? Reading { get; set; }
        }
    }

    public class MeterReconReport_SummaryItem
    {
        public DateTime Month { get { return new DateTime(Date.Year, Date.Month, 1); } }
        public DateTime Date { get; set; }
        public string Skybill_MeterNo { get; set; }
        public string Skybill_MeterSerial { get; set; }
        public string Skybill_Description { get; set; }
        public decimal Skybill_OpeningReading { get; set; }
        public decimal Skybill_ClosingReading { get; set; }
        public decimal Skybill_Consumption { get { return this.Skybill_ClosingReading - this.Skybill_OpeningReading; } }
        public decimal Skybill_Tariff { get { return Skybill_Consumption > 0 ? Skybill_TotalExVAT / Skybill_Consumption : 0; } }
        public decimal Skybill_TotalExVAT { get; set; }
        public string Mirror_Serial { get; set; }
        //public DateTime? Mirror_TimeLogged { get; set; }
        public decimal? Mirror_VirtualOdometerReading { get; set; }
        public decimal? Mirror_Difference { get; set; }
        public string M2M_Serial { get; set; }
        //public DateTime? M2M_TimeLogged { get; set; }
        public string M2M_RegisterName { get; set; }
        public decimal? M2M_VirtualOdometerReading { get; set; }
        public decimal? M2M_Difference { get; set; }
    }

    public class MeterReconReport_SummaryItem_Monthly
    {
        public DateTime Date { get; set; }
        public string Skybill_MeterNo { get; set; }
        public string Skybill_MeterSerial { get; set; }
        public string Skybill_Description { get; set; }
        public decimal Skybill_OpeningReading { get; set; }
        public decimal Skybill_ClosingReading { get; set; }
        public decimal Skybill_Consumption { get { return this.Skybill_ClosingReading - this.Skybill_OpeningReading; } }
        public decimal Skybill_Tariff { get { return Skybill_Consumption > 0 ? Skybill_TotalExVAT / Skybill_Consumption : 0; } }
        public decimal Skybill_TotalExVAT { get; set; }
        //public string Mirror_Serial { get; set; }
        //public DateTime? Mirror_TimeLogged { get; set; }
        public decimal? Mirror_OpeningReading { get; set; }
        public decimal? Mirror_ClosingReading { get; set; }
        public decimal? Mirror_Difference { get { return this.Mirror_ClosingReading - this.Mirror_OpeningReading; } }
        //public string M2M_Serial { get; set; }
        //public DateTime? M2M_TimeLogged { get; set; }
        //public string M2M_OpeningRegisterName { get; set; }
        public decimal? M2M_OpeningReading { get; set; }
        //public string M2M_ClosingRegisterName { get; set; }
        public decimal? M2M_ClosingReading { get; set; }
        public decimal? M2M_Difference { get { return this.M2M_ClosingReading - this.M2M_OpeningReading; } }
    }

    public class J_Finance_BulkReadingsExportModel
    {
        public string SerialNos { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime EndTime { get; set; }
        public int Interval { get; set; }
        public List<J_Finance_BulkReadingsExportRegister> Registers { get; set; }
        public string ErrorMessage { get; set; }
        public string SelectedType { get; set; }
    }
    public class J_Finance_BulkReadingsExportRegister
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public List<string> Type
        {
            get
            {
                return new List<string>()
                {
                    "register",
                    "diff"
                };
            }
        }
        public string SelectedType { get; set; }
        public bool Selected { get; set; }
    }

    public class J_Finance_JournalPaymentAllocationModel
    {
        [Display(Name = "Posting Date")]
        [Required]
        public DateTime? PostingDate { get; set; }

        [Display(Name = "Description")]
        [Required]
        public string Description { get; set; }

        [Display(Name = "Amount")]
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be more than 0")]
        public decimal? Amount { get; set; }

        [Display(Name = "Account")]
        [Required]
        public List<SelectListItem> BalAccountNo { get; set; }

        public string ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class J_Finance_JournalSagepayRelease_SummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public decimal? SkybillSagePayBalance { get; set; }
        public string TableRowID { get; set; }
        public decimal? NetcashBalance { get; set; }
        public DateTime? NetcashBalanceDate { get; set; }
    }

    public class J_Finance_JournalSagepayRelease_DetailsModel
    {
        public decimal? SkybillSagepayBalance { get; set; }

        [Display(Name = "Amount")]
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be more than 0")]
        public decimal? Amount { get; set; }

        public string ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class J_Finance_JournalCigicellRecon_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<J_Finance_JournalCigicellRecon_SummaryItem> J_Finance_JournalCigicellRecon_SummaryItems { get; set; }

        public class J_Finance_JournalCigicellRecon_SummaryItem : Data.Company
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public List<SelectListItem> MovementReport { get; set; }
            public string CompanyName { get; set; }
            public string TableRowID { get; set; }

            public List<J_Finance_JournalCigicellRecon_SummarySubItem> J_Finance_JournalCigicellRecon_SummarySubItems { get; set; }
            public class J_Finance_JournalCigicellRecon_SummarySubItem
            {
                public DateTime Month { get; set; }
                public decimal SkybillCigicellBalance { get; set; }
                public decimal SkybillVendingBalance { get; set; }
                public decimal Total { get { return SkybillCigicellBalance + SkybillVendingBalance; } }
            }

        }
    }

    public class J_Finance_JournalCigicellRecon_DailyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_JournalCigicellRecon_DailyProductItem> J_Finance_JournalCigicellRecon_DailyGL { get; set; }
        public List<J_Finance_JournalCigicellRecon_DailyProductItem> J_Finance_JournalCigicellRecon_DailyNS { get; set; }

        public class J_Finance_JournalCigicellRecon_DailyProductItem
        {
            public string Name { get; set; }
            public Dictionary<DateTime, decimal?> DailyValues { get; set; }
        }

    }

    public class J_Finance_CigicellWeeklySummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_CigicellWeeklySummaryProductItem> J_Finance_CigicellWeeklySummaryProductItems { get; set; }

        public class J_Finance_CigicellWeeklySummaryProductItem
        {
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal VendingAmount { get; set; }

            public decimal PaidPerc
            {
                get
                {
                    if (PaidAmount != 0)
                        return (PaidAmount / PaidAmount) * 100.0m;

                    return 0;
                }
            }
            public decimal VendingPerc
            {
                get
                {
                    if (PaidAmount != 0)
                        return (VendingAmount / PaidAmount) * 100.0m;

                    return 0;
                }
            }
            public decimal NettPerc
            {
                get
                {
                    return PaidPerc + VendingPerc;
                }
            }
        }

    }

    public class J_Finance_CigicellWeeklyDetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_CigicellWeeklyDetailsProductItem> J_Finance_CigicellWeeklyDetailsProductItems { get; set; }

        public class J_Finance_CigicellWeeklyDetailsProductItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal VendingAmount { get; set; }

            public decimal PaidPerc
            {
                get
                {
                    if (PaidAmount != 0)
                        return (PaidAmount / PaidAmount) * 100.0m;

                    return 0;
                }
            }
            public decimal VendingPerc
            {
                get
                {
                    if (PaidAmount != 0)
                        return (VendingAmount / PaidAmount) * 100.0m;

                    return 0;
                }
            }
            public decimal NettPerc
            {
                get
                {
                    return PaidPerc + VendingPerc;
                }
            }
        }

        public decimal PaidAmount
        {
            get
            {
                if (J_Finance_CigicellWeeklyDetailsProductItems != null && J_Finance_CigicellWeeklyDetailsProductItems.Count != 0)
                    return J_Finance_CigicellWeeklyDetailsProductItems.Select(p => p.PaidAmount).Sum();

                return 0;
            }
        }
        public decimal VendingAmount
        {
            get
            {
                if (J_Finance_CigicellWeeklyDetailsProductItems != null && J_Finance_CigicellWeeklyDetailsProductItems.Count != 0)
                    return J_Finance_CigicellWeeklyDetailsProductItems.Select(p => p.VendingAmount).Sum();

                return 0;
            }
        }
        public decimal NettAmount
        {
            get
            {
                return PaidAmount + VendingAmount;
            }
        }
        public decimal PaidPerc
        {
            get
            {
                if (PaidAmount != 0)
                    return (PaidAmount / PaidAmount) * 100.0m;

                return 0;
            }
        }
        public decimal VendingPerc
        {
            get
            {
                if (PaidAmount != 0)
                    return (VendingAmount / PaidAmount) * 100.0m;

                return 0;
            }
        }
        public decimal NettPerc
        {
            get
            {
                return PaidPerc + VendingPerc;
            }
        }
    }

    public class J_Finance_CigicellTransactionDetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> ShowVendingIncorrectOnly { get; set; }
        public List<SelectListItem> Company { get; set; }

        public List<J_Finance_CigicellTransactionDetailsProductItem> J_Finance_CigicellTransactionDetailsProductItems { get; set; }

        public class J_Finance_CigicellTransactionDetailsProductItem : Data.UniPin
        {
            public int? CompanyID { get; set; }
            public decimal VendingAmount
            {
                get
                {
                    decimal amount = 0;

                    if (Vending1Amount5611.HasValue)
                        amount = amount + Vending1Amount5611.Value;
                    if (Vending1Amount5621.HasValue)
                        amount = amount + Vending1Amount5621.Value;
                    if (Vending1Amount6810.HasValue)
                        amount = amount + Vending1Amount6810.Value;
                    if (Vending1Amount7191.HasValue)
                        amount = amount + Vending1Amount7191.Value;
                    if (Vending1Amount8640.HasValue)
                        amount = amount + Vending1Amount8640.Value;

                    if (Vending2Amount5611.HasValue)
                        amount = amount + Vending2Amount5611.Value;
                    if (Vending2Amount5621.HasValue)
                        amount = amount + Vending2Amount5621.Value;
                    if (Vending2Amount6810.HasValue)
                        amount = amount + Vending2Amount6810.Value;
                    if (Vending2Amount7191.HasValue)
                        amount = amount + Vending2Amount7191.Value;
                    if (Vending2Amount8640.HasValue)
                        amount = amount + Vending2Amount8640.Value;

                    if (Vending3Amount5611.HasValue)
                        amount = amount + Vending3Amount5611.Value;
                    if (Vending3Amount5621.HasValue)
                        amount = amount + Vending3Amount5621.Value;
                    if (Vending3Amount6810.HasValue)
                        amount = amount + Vending3Amount6810.Value;
                    if (Vending3Amount7191.HasValue)
                        amount = amount + Vending3Amount7191.Value;
                    if (Vending3Amount8640.HasValue)
                        amount = amount + Vending3Amount8640.Value;

                    if (Vending4Amount5611.HasValue)
                        amount = amount + Vending4Amount5611.Value;
                    if (Vending4Amount5621.HasValue)
                        amount = amount + Vending4Amount5621.Value;
                    if (Vending4Amount6810.HasValue)
                        amount = amount + Vending4Amount6810.Value;
                    if (Vending4Amount7191.HasValue)
                        amount = amount + Vending4Amount7191.Value;
                    if (Vending4Amount8640.HasValue)
                        amount = amount + Vending4Amount8640.Value;

                    amount = amount * -1.0m;
                    return amount;
                }
            }

            public decimal PaidPerc
            {
                get
                {
                    if (PaidAmount != 0)
                        return (PaidAmount / PaidAmount) * 100.0m;

                    return 0;
                }
            }
            public decimal VendingPerc
            {
                get
                {
                    if (PaidAmount != 0)
                        return (VendingAmount / PaidAmount) * 100.0m;

                    return 0;
                }
            }
            public decimal NettPerc
            {
                get
                {
                    return PaidPerc + VendingPerc;
                }
            }
        }

        public decimal PaidAmount
        {
            get
            {
                if (J_Finance_CigicellTransactionDetailsProductItems != null && J_Finance_CigicellTransactionDetailsProductItems.Count != 0)
                    return J_Finance_CigicellTransactionDetailsProductItems.Select(p => p.PaidAmount).Sum();

                return 0;
            }
        }
        public decimal VendingAmount
        {
            get
            {
                if (J_Finance_CigicellTransactionDetailsProductItems != null && J_Finance_CigicellTransactionDetailsProductItems.Count != 0)
                    return J_Finance_CigicellTransactionDetailsProductItems.Select(p => p.VendingAmount).Sum();

                return 0;
            }
        }
        public decimal NettAmount
        {
            get
            {
                return PaidAmount + VendingAmount;
            }
        }
        public decimal PaidPerc
        {
            get
            {
                if (PaidAmount != 0)
                    return (PaidAmount / PaidAmount) * 100.0m;

                return 0;
            }
        }
        public decimal VendingPerc
        {
            get
            {
                if (PaidAmount != 0)
                    return (VendingAmount / PaidAmount) * 100.0m;

                return 0;
            }
        }
        public decimal NettPerc
        {
            get
            {
                return PaidPerc + VendingPerc;
            }
        }
    }

    public class J_Finance_JournalSagepayRecon_SummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public decimal? SkybillSagepayBalance { get; set; }
        public decimal? SkybillVendingBalance { get; set; }
        public decimal? Difference
        {
            get
            {
                if (SkybillSagepayBalance.HasValue && SkybillVendingBalance.HasValue)
                    return SkybillSagepayBalance.Value + SkybillVendingBalance.Value;

                return null;
            }
        }
        public string TableRowID { get; set; }
    }

    public class J_Finance_CompanyFinancialDetailsModel
    {
        [DisplayName("Customer Details")]
        public string CustomerDetails { get; set; }
        [DisplayName("Billing Type")]
        public List<SelectListItem> BillingTypeID { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Electricity_Cons_EndUser_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Electricity_Cons_EndUser_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Electricity_Cons_EndUser_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Electricity_Cons_CommonArea_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Electricity_Cons_CommonArea_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Electricity_Cons_CommonArea_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Electricity_Fixed_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Electricity_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Electricity_Fixed_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Water_Cons_EndUsers_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Water_Cons_EndUsers_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Water_Cons_EndUsers_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Water_Cons_CommonArea_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Water_Cons_CommonArea_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Water_Cons_CommonArea_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Water_Fixed_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Water_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Water_Fixed_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Sanitation_Cons_EndUsers_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Sanitation_Cons_EndUsers_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Sanitation_Cons_EndUsers_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Sanitation_Cons_CommonArea_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Sanitation_Cons_CommonArea_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Sanitation_Cons_CommonArea_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Sanitation_Fixed_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Sanitation_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Sanitation_Fixed_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Gas_Cons_EndUsers_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Gas_Cons_EndUsers_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Gas_Cons_EndUsers_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Gas_Cons_CommonArea_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Gas_Cons_CommonArea_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Gas_Cons_CommonArea_BillingType { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_Gas_Fixed_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_Gas_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_Gas_Fixed_BillingType { get; set; }

        [DisplayName("Electricity")]
        public string Sales_MeteringFees_Cons_EndUsers_Electricity { get; set; }
        [DisplayName("Water")]
        public string Sales_MeteringFees_Cons_EndUsers_Water { get; set; }
        [DisplayName("Gas")]
        public string Sales_MeteringFees_Cons_EndUsers_Gas { get; set; }
        [DisplayName("Other")]
        public string Sales_MeteringFees_Cons_EndUsers_Other { get; set; }

        [DisplayName("Electricity")]
        public string Sales_MeteringFees_Cons_CommonArea_Electricity { get; set; }
        [DisplayName("Water")]
        public string Sales_MeteringFees_Cons_CommonArea_Water { get; set; }
        [DisplayName("Gas")]
        public string Sales_MeteringFees_Cons_CommonArea_Gas { get; set; }
        [DisplayName("Other")]
        public string Sales_MeteringFees_Cons_CommonArea_Other { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_MeteringFees_Fixed_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_MeteringFees_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_MeteringFees_Fixed_BillingType { get; set; }

        [DisplayName("Electricity")]
        public string Sales_OtherFees_Cons_EndUsers_Electricity { get; set; }
        [DisplayName("Water")]
        public string Sales_OtherFees_Cons_EndUsers_Water { get; set; }
        [DisplayName("Gas")]
        public string Sales_OtherFees_Cons_EndUsers_Gas { get; set; }
        [DisplayName("Other")]
        public string Sales_OtherFees_Cons_EndUsers_Other { get; set; }

        [DisplayName("Electricity")]
        public string Sales_OtherFees_Cons_CommonArea_Electricity { get; set; }
        [DisplayName("Water")]
        public string Sales_OtherFees_Cons_CommonArea_Water { get; set; }
        [DisplayName("Gas")]
        public string Sales_OtherFees_Cons_CommonArea_Gas { get; set; }
        [DisplayName("Other")]
        public string Sales_OtherFees_Cons_CommonArea_Other { get; set; }

        [DisplayName("Invoice To")]
        public string Sales_OtherFees_Cons_Fixed_InvoiceTo { get; set; }
        [DisplayName("Tariff")]
        public string Sales_OtherFees_Cons_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Sales_OtherFees_Cons_Fixed_BillingType { get; set; }

        [DisplayName("Invoice From")]
        public string Supply_Electricity_Cons_InvoiceFrom { get; set; }
        [DisplayName("Tariff")]
        public string Supply_Electricity_Cons_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Supply_Electricity_Cons_BillingType { get; set; }

        [DisplayName("Invoice From")]
        public string Supply_Electricity_Fixed_InvoiceFrom { get; set; }
        [DisplayName("Tariff")]
        public string Supply_Electricity_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Supply_Electricity_Fixed_BillingType { get; set; }

        [DisplayName("Invoice From")]
        public string Supply_Water_Cons_InvoiceFrom { get; set; }
        [DisplayName("Tariff")]
        public string Supply_Water_Cons_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Supply_Water_Cons_BillingType { get; set; }

        [DisplayName("Invoice From")]
        public string Supply_Water_Fixed_InvoiceFrom { get; set; }
        [DisplayName("Tariff")]
        public string Supply_Water_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Supply_Water_Fixed_BillingType { get; set; }

        [DisplayName("Invoice From")]
        public string Supply_Sanitation_Cons_InvoiceFrom { get; set; }
        [DisplayName("Tariff")]
        public string Supply_Sanitation_Cons_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Supply_Sanitation_Cons_BillingType { get; set; }

        [DisplayName("Invoice From")]
        public string Supply_Fixed_InvoiceFrom { get; set; }
        [DisplayName("Tariff")]
        public string Supply_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Supply_Fixed_BillingType { get; set; }

        [DisplayName("Invoice From")]
        public string Supply_Gas_Cons_InvoiceFrom { get; set; }
        [DisplayName("Tariff")]
        public string Supply_Gas_Cons_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Supply_Gas_Cons_BillingType { get; set; }

        [DisplayName("Invoice From")]
        public string Supply_Gas_Fixed_InvoiceFrom { get; set; }
        [DisplayName("Tariff")]
        public string Supply_Gas_Fixed_Tariff { get; set; }
        [DisplayName("Billing Type")]
        public string Supply_Gas_Fixed_BillingType { get; set; }

        [DisplayName("Electricity")]
        public string Supply_Metering_Electricity { get; set; }
        [DisplayName("Water")]
        public string Supply_Metering_Water { get; set; }
        [DisplayName("Gas")]
        public string Supply_Metering_Gas { get; set; }
        [DisplayName("Other")]
        public string Supply_Metering_Other { get; set; }

        [DisplayName("Electricity")]
        public string Supply_Other_Electricity { get; set; }
        [DisplayName("Water")]
        public string Supply_Other_Water { get; set; }
        [DisplayName("Gas")]
        public string Supply_Other_Gas { get; set; }
        [DisplayName("Other")]
        public string Supply_Other_Other { get; set; }
    }

    public class J_Finance_WinshuttleExportModel
    {
        [Display(Name = "From Date")]
        [Required]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [Required]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Partner")]
        [Required]
        public List<SelectListItem> PartnerID { get; set; }

        [Display(Name = "Customer Purchase Order Number VBKD BSTKD")]
        [Required]
        public string Customer_Purchase_Order_Number_VBKD_BSTKD { get; set; }

        public List<J_Finance_WinshuttleExportItem> J_Finance_WinshuttleExportItems { get; set; }

        public class J_Finance_WinshuttleExportItem
        {
            public string CentreName { get; set; }
            public string CustomerRegisteredName { get; set; }
            public string CustomerTradingName { get; set; }
            public string AccountNumber { get; set; }
            public DateTime DateRead { get; set; }
            public decimal OpeningReading { get; set; }
            public decimal ClosingReading { get; set; }
            public decimal DiffM3 { get { return ClosingReading - OpeningReading; } }
            public decimal ConvFact { get; set; }
            public decimal KgToInvoice { get { return DiffM3 * ConvFact; } }
            public string InvoiceNumber { get; set; }
            public string Batch { get; set; }
            public string PlantNo { get; set; }
            public string StockRefNo { get; set; }
            public string Notes { get; set; }
            public string UnbilledReason { get; set; }
        }
    }

    public class J_Finance_WinshuttleExportRequestResultModel
    {
        public bool AlreadyExist { get; set; }
        public Data.J_Finance_WinshuttleExport J_Finance_WinshuttleExport { get; set; }
    }

    public class J_Finance_WinshuttleExportRequestsModel
    {
        public List<J_Finance_WinshuttleExportRequestItem> J_Finance_WinshuttleExportRequestItems { get; set; }
        public class J_Finance_WinshuttleExportRequestItem : Data.J_Finance_WinshuttleExport
        {
            public string Username { get; set; }
            public string PartnerName { get; set; }
            public int ItemCount { get; set; }
        }
    }

    public class J_Finance_PQAllocationViewModel
    {
        public List<PQAllocationItem> J_Finance_PQAllocationItems { get; set; }
    }

    public class J_Finance_PQAllocationCreateModel
    {
        [Display(Name = "Serial")]
        [Required]
        public string Serial { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class J_Finance_PQAllocationEditModel
    {
        public PQAllocationItem J_Finance_PQAllocation { get; set; }
    }

    public class J_Finance_NetcashMissingSkybillJournalsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> ShowSystemTransactions { get; set; }
        public List<SelectListItem> ShowErrorTransactions { get; set; }

        public List<J_Finance_NetcashMissingSkybillJournalsItem> J_Finance_NetcashMissingSkybillJournalsItems { get; set; }

        public class J_Finance_NetcashMissingSkybillJournalsItem : Data.NetcashStatement
        {
            public string SkybillDescription { get; set; }
        }
    }

    public class J_Finance_NetcashMissingSkybillJournals_AddModel
    {
        public Data.NetcashStatement NetcashStatement { get; set; }

        [Display(Name = "Posting Date")]
        [Required]
        public DateTime PostingDate { get; set; }

        [Display(Name = "Customer")]
        [Required]
        public List<SelectListItem> Customer { get; set; }

        [Display(Name = "Create Convenience Fee")]
        [Required]
        public bool CreateConvenienceFee { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class J_Finance_NetcashMissingSkybillJournals_UpdatePaymentIDModel
    {
        public Data.NetcashStatement NetcashStatement { get; set; }

        [Display(Name = "Payment ID")]
        [Required]
        public int? PaymentID { get; set; }

        [Display(Name = "Skybill Document No")]
        [Required]
        public string SkybillDocumentNo { get; set; }

        public bool IsSuccess { get; set; }
        public bool AllowEdit { get; set; }

        public string BackURL { get; set; }
    }

    public class J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNoModel
    {
        public Data.NetcashStatement NetcashStatement { get; set; }

        [Display(Name = "Skybill Document No")]
        [Required]
        public string SkybillDocumentNo { get; set; }

        public bool IsSuccess { get; set; }
        public bool AllowEdit { get; set; }
    }

    public class J_Finance_NetcashManualPaymentsModel
    {
        public bool ShowAllEntries { get; set; }

        public List<J_Finance_NetcashManualPaymentsItem> J_Finance_NetcashManualPaymentsItems { get; set; }

        public class J_Finance_NetcashManualPaymentsItem : Data.NetcashManualPayment
        {
            public string ApprovedByUsername { get; set; }
            public Data.NetcashStatement NetcashStatement { get; set; }
            public string CompanyName { get; set; }
            public Data.NetcashManualPaymentRule NetcashManualPaymentRule { get; set; }
            public string SerialNumber { get; set; }
        }

        public class MissingDepositsItem : Data.NetcashStatement
        {
            public string CompanyName { get; set; }
            public List<SelectListItem> Customer { get; set; }
            public bool AlreadyHasRule { get; set; }
        }

        public List<MissingDepositsItem> MissingDeposits { get; set; }

        public class NetcashManualPaymentRuleItem : Data.NetcashManualPaymentRule
        {
            public string CreatedByUsername { get; set; }
            public string CompanyName { get; set; }
        }

        public List<NetcashManualPaymentRuleItem> NetcashManualPaymentRuleItems { get; set; }
        public F_SystemGeneratedReports_DirectDepositsAllocation_Request LatestRequest { get; set; }
        public class F_SystemGeneratedReports_DirectDepositsAllocation_Request : Data.F_SystemGeneratedReports_DirectDepositsAllocation_Request
        {
            public string CreatedByUsername { get; set; }
        }
    }

    public class J_Finance_NetcashReport_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> MovementReport { get; set; }
        public List<J_Finance_NetcashReport_SummaryItem> J_Finance_NetcashReport_SummaryItems { get; set; }

        public class J_Finance_NetcashReport_SummaryItem : Data.Company
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public List<SelectListItem> MovementReport { get; set; }
            public string CompanyName { get; set; }
            public string TableRowID { get; set; }

            public List<J_Finance_NetcashReport_SummarySubItem> J_Finance_NetcashReport_SummarySubItems { get; set; }
            public class J_Finance_NetcashReport_SummarySubItem
            {
                public DateTime Month { get; set; }
                public decimal System { get; set; }
                public decimal Netcash { get; set; }
                public decimal Total { get { return System - Netcash; } }
            }

        }
    }
    public class J_Finance_NetcashReport_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_NetcashReport_MonthlyProductItem> J_Finance_NetcashReport_MonthlyGL { get; set; }
        public List<J_Finance_NetcashReport_MonthlyProductItem> J_Finance_NetcashReport_MonthlyNS { get; set; }

        public class J_Finance_NetcashReport_MonthlyProductItem
        {
            public string Name { get; set; }
            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

    }
    public class J_Finance_NetcashReport_DailyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_NetcashReport_DailyProductItem> J_Finance_NetcashReport_DailyGL { get; set; }
        public List<J_Finance_NetcashReport_DailyProductItem> J_Finance_NetcashReport_DailyNS { get; set; }

        public class J_Finance_NetcashReport_DailyProductItem
        {
            public string Name { get; set; }
            public Dictionary<DateTime, decimal?> DailyValues { get; set; }
        }

    }
    public class J_Finance_NetcashReport_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string JournalNo { get; set; }
        public string JournalName { get; set; }
        public List<J_Finance_NetcashReport_DetailsItem> J_Finance_NetcashReport_DetailsItems { get; set; }

        public class J_Finance_NetcashReport_DetailsItem : MyVoltage.Data.NetcashStatement
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
        }
    }

    public class J_Finance_NetcashServicesChargesReconModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_NetcashServicesChargesReconProductItem> J_Finance_NetcashServicesChargesReconGL { get; set; }
        public List<J_Finance_NetcashServicesChargesReconProductItem> J_Finance_NetcashServicesChargesReconNS { get; set; }

        public class J_Finance_NetcashServicesChargesReconProductItem
        {
            public string Name { get; set; }
            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }
        public Dictionary<DateTime, bool> FoundInSkybill { get; set; }

        public F_SystemGeneratedReports_NetcashServicesChargesRecon_Request LatestRequest { get; set; }

        public class F_SystemGeneratedReports_NetcashServicesChargesRecon_Request : Data.F_SystemGeneratedReports_NetcashServicesChargesRecon_Request
        {
            public string CreatedByUsername { get; set; }
        }

    }

    public class J_Finance_NetcashTransactionsReconModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_NetcashTransactionsReconProductItem> J_Finance_NetcashTransactionsReconGL { get; set; }
        public List<J_Finance_NetcashTransactionsReconProductItem> J_Finance_NetcashTransactionsReconNS { get; set; }

        public class J_Finance_NetcashTransactionsReconProductItem
        {
            public string Name { get; set; }
            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }


    }

    public class J_Finance_NetcashTransactionsRecon_DailyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<J_Finance_NetcashTransactionsRecon_DailyProductItem> J_Finance_NetcashTransactionsRecon_DailyGL { get; set; }
        public List<J_Finance_NetcashTransactionsRecon_DailyProductItem> J_Finance_NetcashTransactionsRecon_DailyNS { get; set; }

        public class J_Finance_NetcashTransactionsRecon_DailyProductItem
        {
            public string Name { get; set; }
            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }


    }

    public class J_Finance_ExternalChargedSummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<J_Finance_ExternalChargedSummaryItem> J_Finance_ExternalChargedSummaryItems { get; set; }

        public class J_Finance_ExternalChargedSummaryItem
        {
            public string CustomerNo { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            public List<J_Finance_ExternalChargedSummarySubItem> J_Finance_ExternalChargedSummarySubItems_Invoice { get; set; }
            public List<J_Finance_ExternalChargedSummarySubItem> J_Finance_ExternalChargedSummarySubItems_Payment { get; set; }

            public class J_Finance_ExternalChargedSummarySubItem
            {
                public DateTime Month { get; set; }
                public decimal? Amount { get; set; }
                public string CellStyle { get; set; }
                public string ToolTip { get; set; }
            }

        }
    }

}
