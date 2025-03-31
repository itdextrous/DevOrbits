export default class CustomerRoleDto {
    relatedId: string = "";
    stripeCustomerId: string = "";
    id: string = "";
    accessFailedCount: number = 0;
    lockoutEnabled: boolean = false;
    lockoutEnd: string = "";
    twoFactorEnabled: boolean = false;
    phoneNumberConfirmed: boolean = true;
    phoneNumber: string = "";
    concurrencyStamp: string = "";
    securityStamp: string = "";
    passwordHash: string = "";
    emailConfirmed: boolean = true;
    normalizedEmail: string = "";
    email: string = "";
    normalizedUserName: string = "";
    userName: string = "";
    customerId: string = "";
  }