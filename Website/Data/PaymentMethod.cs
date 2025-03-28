using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class PaymentMethod
    {
        public int PaymentMethodID { get; set; }
        public string Name { get; set; }
    }

    public enum PaymentMethodEnum : int
    {
        [Description("Unipin")]
        Unipin = 0,
        [Description("Mastercard/VISA")]
        MastercardVISA = 1,
        [Description("EFT")]
        EFT = 2,
        [Description("Retail")]
        Retail = 3,
        [Description("iPay")]
        iPay = 4,
        [Description("Master Pass")]
        MasterPass = 5,
        [Description("Visa Checkout")]
        VisaCheckout = 6,
        [Description("Credit Card")]
        CreditCard = 7,
        [Description("Not In Use")]
        NotInUse = 9,
        [Description("Direct Deposit")]
        NetcashManualPayment = 10,

    }

    public static class PaymentMethodFees
    {
        public static FeesEnum GetBankCharges(PaymentMethodEnum paymentMethod, decimal fullAmount, string paymentID)
        {
            switch (paymentMethod)
            {
                case PaymentMethodEnum.MastercardVISA:
                case PaymentMethodEnum.CreditCard:
                    decimal fixedFeeMCVisa = 1.25m;
                    decimal percentageFeeMCVisa = 3.0m / 100;

                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeMCVisa + (fixedFeeMCVisa * VAT),
                        FixedFeeDescription = $"{paymentID} - Mastercard/VISA Cards: 1 at {fixedFeeMCVisa:N}",
                        PercentageFee = (fullAmount * percentageFeeMCVisa) * (1.0m + VAT),
                        PercentageFeeDescription = $"{paymentID} - Mastercard/VISA Card: {fullAmount:N} at {percentageFeeMCVisa * 100:N}%",
                        MVVendingCommission = 0.05m
                    };

                case PaymentMethodEnum.EFT:
                    decimal fixedFeeEFT = 3.5m;
                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeEFT + (fixedFeeEFT * VAT),
                        FixedFeeDescription = $"{paymentID} - Pay Now EFT for {fullAmount:N}",
                        PercentageFee = 0,
                        PercentageFeeDescription = "",
                        MVVendingCommission = 0.05m
                    };
                case PaymentMethodEnum.NetcashManualPayment:
                    decimal fixedFeeNetcashManualPayment= 3.5m;
                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeNetcashManualPayment + (fixedFeeNetcashManualPayment * VAT),
                        FixedFeeDescription = $"{paymentID} - Direct Deposit for {fullAmount:N}",
                        PercentageFee = 0,
                        PercentageFeeDescription = "",
                        MVVendingCommission = 0.05m
                    };
                case PaymentMethodEnum.Retail:
                    decimal fixedFeeRetail = 3.0m;
                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeRetail + (fixedFeeRetail * VAT),
                        FixedFeeDescription = $"{paymentID} - Retail for {fullAmount:N}",
                        PercentageFee = 0,
                        PercentageFeeDescription = "",
                        MVVendingCommission = 0.05m
                    };
                case PaymentMethodEnum.iPay:
                    decimal fixedFeeiPay = 6.0m;
                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeiPay + (fixedFeeiPay * VAT),
                        FixedFeeDescription = $"{paymentID} - iPay: {fullAmount:N} at {(fixedFeeiPay + (fixedFeeiPay * VAT)):N}",
                        PercentageFee = 0,
                        PercentageFeeDescription = "",
                        MVVendingCommission = 0.05m
                    };
                case PaymentMethodEnum.MasterPass:
                    decimal fixedFeeMasterPass = 1.25m;
                    decimal percentageFeeMasterPass = 3.2m / 100;

                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeMasterPass + (fixedFeeMasterPass * VAT),
                        FixedFeeDescription = $"{paymentID} - MasterPass: 1 at {fixedFeeMasterPass:N}",
                        PercentageFee = (fullAmount * percentageFeeMasterPass) * (1.0m + VAT),
                        PercentageFeeDescription = $"{paymentID} - MasterPass: {fullAmount:N} at {percentageFeeMasterPass * 100:N}%",
                        MVVendingCommission = 0.05m
                    };
                case PaymentMethodEnum.VisaCheckout:
                    decimal fixedFeeVisaCheckout = 1.25m;
                    decimal percentageFeeVisaCheckout = 3.2m / 100;

                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeVisaCheckout + (fixedFeeVisaCheckout * VAT),
                        FixedFeeDescription = $"{paymentID} - Visa Checkout: 1 at {fixedFeeVisaCheckout:N}",
                        PercentageFee = (fullAmount * percentageFeeVisaCheckout) * (1.0m + VAT),
                        PercentageFeeDescription = $"{paymentID} - Visa Checkout: {fullAmount:N} at {percentageFeeVisaCheckout * 100:N}%",
                        MVVendingCommission = 0.05m
                    };

                case PaymentMethodEnum.Unipin:
                    decimal fixedFeeUnipin = 0;
                    decimal percentageFeeUnipin = 4.0m / 100;

                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeUnipin + (fixedFeeUnipin * VAT),
                        FixedFeeDescription = $"{paymentID} - Unipin: 1 at {fixedFeeUnipin:N}",
                        PercentageFee = (fullAmount * percentageFeeUnipin) * (1.0m + VAT),
                        PercentageFeeDescription = $"{paymentID} - Unipin: {fullAmount:N} at {percentageFeeUnipin * 100:N}%",
                        MVVendingCommission = 0.05m
                    };

                case PaymentMethodEnum.NotInUse:
                    decimal fixedFeeNotInUse = 0;
                    decimal percentageFeeNotInUse = 0;

                    return new FeesEnum()
                    {
                        FixedFee = fixedFeeNotInUse + (fixedFeeNotInUse * VAT),
                        FixedFeeDescription = $"{paymentID} - NotInUse: 1 at {fixedFeeNotInUse:N}",
                        PercentageFee = (fullAmount * percentageFeeNotInUse) * (1.0m + VAT),
                        PercentageFeeDescription = $"{paymentID} - NotInUse: {fullAmount:N} at {percentageFeeNotInUse * 100:N}%",
                        MVVendingCommission = 0
                    };
            }

            return null;
        }
        public static decimal VAT { get { return 0.15m; } }
        public class FeesEnum
        {
            public decimal FixedFee { get; set; }
            public string FixedFeeDescription { get; set; }
            public decimal PercentageFee { get; set; }
            public string PercentageFeeDescription { get; set; }
            public decimal MVVendingCommission { get; set; }

        }
    }

    public class PaymentMethods_SkybillJournalNo
    {
        [Key]
        public int ID { get; set; }
        public int PaymentMethodID { get; set; }
        public int? SkybillJournalNo { get; set; }
        public PaymentMethodEnum PaymentMethod { get { return (PaymentMethodEnum)PaymentMethodID; } }
    }
}