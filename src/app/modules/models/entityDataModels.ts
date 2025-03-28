export class AddTransactionModel {
    WhoAreYouRepresenting: string = '';
    IsBuyerFinancingPurchase: string = '';
    WhoIsProcessingTransaction: string = '';
    AgentPercentList: string = '';
    SellerPercentList: string = '';
    AgentPercentage: string = '';
    AgentDollarAmount: string = '';
    SellerPercentage: string = '';
    SellerDollarAmount: string = '';
    AdditionalComm: string = '';
    Valid: boolean = false;
}
export class BuyerModel {
    BuyerFirstName: string = '';
    BuyerLastName: string = '';
    BuyerPhone: string = '';
    BuyerEmail: string = '';
    CoBuyerFirstName: string = '';
    CoBuyerLastName: string = '';
    CoBuyerPhone: string = '';
    CoBuyerEmail: string = '';
    Valid: boolean = false;
}
export class SellerModel {
    SellerFirstName: string = '';
    SellerLastName: string = '';
    SellerPhone: string = '';
    SellerEmail: string = '';
    CoSellerFirstName: string = '';
    CoSellerLastName: string = '';
    CoSellerPhone: string = '';
    CoSellerEmail: string = '';
    Valid: boolean = false;
}
export class CoOpAgentModel {
    CoAgentCompanyName: string = '';
    CoAgentFirstName: string = '';
    CoAgentLastName: string = '';
    CoAgentPhone: string = '';
    CoAgentEmail: string = '';
    AdditionalCompanyName: string = '';
    AdditionalFirstName: string = '';
    AdditionalLastName: string = '';
    AdditionalPhone: string = '';
    AdditionalEmail: string = '';
    Valid: boolean = false;
}
export class MortgageModel {
    LoanOfficerCompanyName: string = '';
    LoanOfficerFirstName: string = '';
    LoanOfficerLastName: string = '';
    LoanOfficerPhone: string = '';
    LoanOfficerEmail: string = '';
    AdditionalCompanyName: string = '';
    AdditionalFirstName: string = '';
    AdditionalLastName: string = '';
    AdditionalPhone: string = '';
    AdditionalEmail: string = '';
    Valid: boolean = false;
}
export class TitleModel {
    TitleCompanyName: string = '';
    TitleRepFirstName: string = '';
    TitleRepLastName: string = '';
    TitleRepPhone: string = '';
    TitleRepEmail: string = '';
    AdditionalCompanyName: string = '';
    AdditionalFirstName: string = '';
    AdditionalLastName: string = '';
    AdditionalPhone: string = '';
    AdditionalEmail: string = '';
    Valid: boolean = false;
}
export class PropertyModel {
    EnterAddress: string = '';
    PropertyAddress: string = '';
    PropertyCity: string = '';
    PropertyZipCode: string = '';
    PropertyState: string = '';
    Valid: boolean = false;
}
export class TimelineModel {
    PurchaseAgreementDate: string = '';
    ClosingDate: string = '';
    DaysToObtainFinancing: string = '';
    DaysForHomeOwnerCommitment: string = '';
    DaysForInspectionPeriod: string = '';
    HOADocDeliveryPeriod: string = '';
    Valid: boolean = false;
}
export class UploadDocsModel {
    PurchaseAgreement: string = '';
    TaxSheet: string = '';
    CounterOffer: string = '';
    DocumentType: string = '';
    Valid: boolean = false;
}
export class CompanyModel {
    CompanyName: string = '';
    OfficerName: string = '';
    OfficerPhone: string = '';
    OfficerEmail: string = '';
}

export class MortgageCompanyModel {
    Id: number = 0;
    TransactionId: number = 0;
    MortgagePrefferdVendor: string = '';
    MortgageCompanyName: string = '';
    LoanOfficerName: string = '';
    LoanOfficerLastName: string = '';
    LoanOfficerPhone: string = '';
    LoanOfficerEmail: string = '';
    DeleteDate: Date = new Date();
    CreateDate: Date = new Date();
}
export class TitleCompanyModel {
    Id: number = 0;
    TransactionId: number = 0;
    TitlePrefferdVendor: string = '';
    TitleCompanyName: string = '';
    TitleRepName: string = '';
    TitleRepLastName: string = '';
    TitleRepPhone: string = '';
    TitleRepEmail: string = '';
    DeleteDate: Date = new Date();
    CreateDate: Date = new Date();
}
export class CoAgentCompanyModel {
    Id: number = 0;
    AgentCompanyName: string = "";
    AgentFirstName: string = ""; 
    AgentLastName: string = "";
    AgentPhone: string = "";
    AgentEmail: string = "";
    DeleteDate: Date = new Date();
    CreateDate: Date = new Date();
}

export class TaskModel {
    Id: number = 0;
    TaskName: string = "";
    TaskDescription: string = "";
    Case: string = "";
    Target: string = "";
    DependentTask: string = "";
    Deprel: string = "";
    Float: string = "";
    WizardQuestion: string = "";
    CreateDate: Date = new Date();
}
export class TransactionModel {
    Id: number = 0;
    WhoAreYouRepresenting: string = "";
    PropertyAddress: string = "";
    PropertyCity: string = "";
    PropertyZipCode: string = "";
    PropertyState: string = "";
    AgentCompany: string = "";
    AgentLastName: string = "";
    AgentEmail: string = "";
    AgentName: string = "";
    AgentPhone: string = "";
    AssignDate: string = "";
    AssignedDealsToElf: any = [];
    BuyerEmail: string = "";
    BuyerFirstName: string = "";
    BuyerLastName: string = "";
    BuyerPhone: string = "";
    ClientId: number = 0;
    ClientName: string = "";
    ClientPhone: string = "";
    ClosingDate: string = "";
    CoAgentCompanyName: string = "";
    CoAgentEmail: string = "";
    CoAgentLastName: string = "";
    CoAgentName: string = "";
    CoAgentPhone: string = "";
    CoBuyerEmail: string = "";
    CoBuyerFirstName: string = "";
    CoBuyerLastName: string = "";
    CoBuyerPhone: string = "";
    CoSellerEmail: string = "";
    CoSellerFirstName: string = "";
    CoSellerLastName: string = "";
    CoSellerPhone: string = "";
    CompanyId: number = 0;
    CompleteDate: string = "";
    CompleteStatus: string = "";
    CountOfCriticalTask: string = "";
    CountOfTodayDueTask: string = "";
    CreateDate: string = "";
    DaysForHomeOwnerCommitment: string = "";
    DaysForInspectionPeriod: string = "";
    DaysToObtainFinancing: string = "";
    DeleteDate: string = "";
    EditStatus: boolean = false;
    EmailTransaction: string = "";
    GenericTransactionTask: any;
    HOADocDeliveryPeriod: string = "";
    HomeWarranty: string = "";
    IsAssigned: boolean = false;
    IsBuyerFinancingPurchase: boolean = false;
    IsDaysforInspectionPeriod: boolean = false;
    IsDaystoObtainFinancing: boolean = false;
    IsEmailSentToClient: boolean = false;
    IsHoadocDeliveryPeriod: boolean = false;
    IsHomeWarrantyOrderedBy: boolean = false;
    IsSelfElf: boolean = false;
    LastValidStep: string = "";
    LoanOfficerEmail: string = "";
    LoanOfficerLastName: string = "";
    LoanOfficerName: string = "";
    LoanOfficerPhone: string = "";
    MortgageCompanyName: string = "";
    MortgagePrefferdVendor: string = "";
    NextTaskToBeDue: string = "";
    OverviewNote: any = [];
    PurchaseAgreementDate: string = "";
    SellerEmail: string = "";
    SellerFirstName: string = "";
    SellerLastName: string = "";
    SellerPhone: string = "";
    TabStatus: string = "";
    TitleCompanyName: string = "";
    TitlePrefferdVendor: string = "";
    TitleRepEmail: string = "";
    TitleRepLastName: string = "";
    TitleRepName: string = "";
    TitleRepPhone: string = "";
    TransactionCommunication: any = [];
    TransactionElf: string = "";
    TransactionMortgageInfo: any = [];
    TransactionNotes: any = [];
    TransactionTasks: any = [];
    TransactionsDocs: any = [];
    UserId: string = "";
    WhoOrderTitle: string = "";

    LoanOfficerCompanyName: string = "";
    LoanOfficerFirstName: string = "";
    AdditionalCompanyName: string = "";
    AdditionalFirstName: string = "";
    AdditionalLastName: string = "";
    AdditionalPhone: string = "";
    AdditionalEmail: string = "";

    PurchaseAgreement: string = "";
    TaxSheet: string = "";
    CounterOffer: string = "";
    DocumentType: string = "";

    AgentPercentList: string = "";
    SellerPercentList: string = "";
    AgentPercentage: string = "";
    AgentDollarAmount: string = "";
    SellerPercentage: string = "";
    SellerDollarAmount: string = "";
    AdditionalComm: string = "";
    EnterAddress: string = "";
}

export class TransactionDocumentModel {
    Id: number = 0;
    TransactionId: number = 0;
    DeleteDate: Date = new Date();
    CreateDate: Date = new Date();
    DocType: string = "";
    DocName: string = "";
    OriginalFileName: string = "";
    FileSize: string = "";
}

export class AddAssistantModel {
    Id: number = 0;
    Company: number = 0;
    FirstName: string = "";
    LastName: string = "";
    Email: string = "";
    PhoneNo: string = "";
    ParentId: string = "";
    AssistantFor: string = "";
    TransactionId: number = 0;
}

export class EditAssistantModel {
    TransactionId: number = 0;
    Type: string = "";
    Name: string = "";
    LastName: string = "";
    Phone: string = "";
    Email: string = "";
    AssistantId: number = 0;
    CompanyName: string = "";
}

export class EditClientModel {
    TransactionId: number = 0;
    Type: string = "";
    FirstName: string = "";
    LastName: string = "";
    Phone: string = "";
    Email: string = "";
}

export class GenericTaskCompleteModel {
    TransactionId: number = 0;
    TaskId: number = 0;
    TaskName: string = "";
    TaskDescription: string = "";
    IsGenericTask: boolean = false;
}

export class MortgagePreferredVendorList {
    Id: number = 0;
    MortgagePrefferdVendor: string = "";
    TransactionId: number = 0;
    MortgageCompanyName: string = "";
    LoanOfficerName: string = "";
    LoanOfficerPhone: string = "";
    LoanOfficerEmail: string = "";
    DeleteDate: Date = new Date();
    CreateDate: Date = new Date();
    LoanOfficerLastName: string = "";

    Transaction: TransactionModel = new TransactionModel();
    MortgagePreferredVendorsMaping: MortgagePreferredVendorsMapingModel = new MortgagePreferredVendorsMapingModel();
}

export class MortgagePreferredVendorsMapingModel {
    Id: number = 0;
    UserId: string = "";
    TransactionMortgageInfoId: number = 0;
}

export class TitlePreferredVendorList {
    Id: number = 0;
    TitlePrefferdVendor: string = "";
    TransactionId: number = 0;
    TitleCompanyName: string = "";
    TitleRepName: string = "";
    TitleRepPhone: string = "";
    TitleRepEmail: string = "";
    DeleteDate: Date = new Date();
    CreateDate: Date = new Date();
    TitleRepLastName: string = "";
}

export class ReOpenTransactionModel {
    Id: number = 0;
    ClosingDate: Date = new Date();
}

export class InvitedElvesModel {
    Id: number = 0;
    RecieverFirstName: string = "";
    RecieverLastName: string = "";
    RecieverEmail: string = "";
    RecieverRole: string = "";
    SenderUserId: string = "";
    SenderName: string = "";
    BrokerageId: number = 0;
    EmailActionStatus: number = 0;
    ResendStatus: number = 0;
    SendDate: Date = new Date();
    ActionDate: Date = new Date();
}

export class TaskCompletionModel {
    TransactionId: number = 0;
    GenericTransactionTaskId: number = 0;
    Token: string = "";
    TitleRepUserId: string = "";
    Status: string = "";
    UploadFile: string = "";
    GenrericTaskName: string = "";
    GenrericTaskCase: string = "";
    PropertyAddress: string = "";
}

export class ShareTransactionsModel {
    UserId: string = "";
    CanShareTransactions: boolean = false;
}