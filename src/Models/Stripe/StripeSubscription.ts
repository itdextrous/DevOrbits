export interface IStripeSubscription {
  paymentMethod: string;
  customer: string;
  planName: string;
  unitPrice: number;
  adminEmail: string;
}