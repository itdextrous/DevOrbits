using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UniPinVerify
    {
        public string DateTime { get; set; }
        public string MeterNumber { get; set; }
        public string Hash { get; set; }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public class UniPinVerifyResult
    {
        public int ResponseStatus { get; set; }
        public string UserName { get; set; }
        public string UserAddress { get; set; }
        public string MeterNumber { get; set; }
        public string ResponseStatusText { get; internal set; }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public class UniPinRequest
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string MeterNumber { get; set; }
        public string AmountInCents { get; set; }
        public string Hash { get; set; }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public class UniPinResult
    {
        public int ResponseStatus { get; set; }
        public string UserName { get; set; }
        public string UserAddress { get; set; }
        public string MeterNumber { get; set; }
        public string Reference { get; set; }
        public string Message { get; set; }

        public decimal ConvenienceFee { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal LoadedAmount { get; set; }
        public decimal Balance { get; set; }
        public string ResponseStatusText { get; internal set; }

        public int NumOfTokens {get;set; }
        public List<string> Tokens { get; set; }

        /**
         		<NumOfTokens>1</NumOfTokens>
			<Tokens>
				<Token TokenType="CREDIT" SortOrder="1"/>
			</Tokens>

          */

        public class Token
        {
            public string TokenType { get; set; }
            public int SortOrder { get; set; }
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public class UniPinLoad
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Hash { get; set; }
    }
}
