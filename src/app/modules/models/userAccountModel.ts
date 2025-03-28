export interface LoginModel {
    email: string;
    password: string;
}

export class RegisterModel {
    FirstName: string = "";
    LastName: string = "";
    Email: string = "";
    PhoneNumber: string = "";
    Password: string = "";
    ConfirmPassword: string = "";
    Role: string = "";
    UserName: string = "";
}
export class ForgotPassword {
    Email: string = "";
}

export class ResetPasswordModel {
    Email: string = "";
    Password: string = "";
    ConfirmPassword: string = "";
    Token: string = "";
    Id: string = "";
}
export interface DialogData {
    key:string;
    obj:any;
    type: any;
}

export interface LoginModel {
    email: string;
    password: string;
}

export class AddUserModel {
    Id: string = "";
    Email: string = "";
    PhoneNumber: string = "";
    Password: string = "";
    ConfirmPassword: string = "";
    UserName: string = "";
    AddUserRoles: any = new AspNetUserRoles();
    AddUserAdditionalData: any = new UserAdditionalData();
}

export class AspNetUserRoles {
    UserId: string = "";
    RoleId: string = "";
}

export class UserAdditionalData {
    Id: number = 0;
    FirstName: string = "";
    LastName: string = "";
    IsActive: boolean = false;
    ProfileImage: string = "";
    LogoImage: string = "";
    PrimaryRole: string = "";
    PhoneNumber: string = "";
    CreateDate: Date = new Date();
    DeleteDate: Date = new Date();
    UserId: string = "";
    Fax: string = "";
    IsFreeAccount: boolean = false;
    TransactionCapacity: number = 0;
    Category: string = "";
    PreferredVendor: string = "";
    Company: string = "";
    StreetAddress: string = "";
    Title: string = "";
    Website: string = "";
    LicenseNumber: string = "";
    SkinPreference: string = "";
    OfficePhone: string = "";
    EmailSignature?: string = "";
    LastLoginDate?: Date = new Date();
    SocialLogins?: string = "";
    IsFacebookLinked?: boolean = false;
    IsTwitterLinked?: boolean = false;
    IsCompanyImageSelected?: boolean = false;
    IsPrimaryBroker?: boolean = false;
    IsProposalAccepted?: boolean = false;
    BillingType?: string = "";
    SmallImage?: string = "";
    RequestIsPendingOrNot?: string = "";
    LogoImageInMail?: string = "";
    //IFormFile LogoImage { get; set; }
}

export interface UserDataModel {
    id: string;
    userName: string;
    email: string;
    firstName: string;
    lastName: string;
    phoneNumber: number;
    role: string;
    active: string;
    accountCreated: string;
    lastLogin: string;
    freeAccount: string;
    fax: string;
    roles:any;
    logoImg:string;
    profileImg:string;
    isActive: boolean; 
  }

  export interface BrokerageDataModel {
    id: number;
    companyName: string;
    companyAddress: string;
    city: string;
    state: string;
    zip: string;
    combinedAddress: string; 
  }