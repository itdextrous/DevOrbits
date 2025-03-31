import { IStripeCustomerDto } from "Models/Stripe/StripeCustomer";
import { IUserDto } from "Models/Stripe/UserDto";
import { IStripeSubscription } from "Models/Stripe/StripeSubscription";
import { apiGet, apiPost } from "./ApiService";

class StripeApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Stripe`;

  protected userResourceName = `${process.env.REACT_APP_DOMAIN}/api/Identity/Account`;

  createCustomer(entity: IStripeCustomerDto) {
    return apiPost<IStripeCustomerDto, any>(`${this.resourceName}/CreateStripeCustomer`, entity);
  }

  createSubscription(entity: IStripeSubscription) {
    return apiPost<IStripeSubscription, any>(`${this.resourceName}/Subscription`, entity);
  }

  createUser(entity: IUserDto) {
    return apiPost<IUserDto, any>(`${this.userResourceName}/Register`, entity);
  }

  isEmailExists(email: string) {
    return apiGet<any>(`${this.userResourceName}/IsEmailExists/${email}`);
  }
}

const stripeApi = new StripeApi();
export default stripeApi;
