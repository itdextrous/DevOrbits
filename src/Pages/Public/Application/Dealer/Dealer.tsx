import { useState, useCallback } from "react";
import { useHistory } from "react-router-dom";
import { Button, Row, Col } from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import * as yup from "yup";
import InputText from "Components/Controls/InputText";
import DealerApplicationDto from "Models/DealerApplication/DealerApplicationDto";
import dealerApplicationApi from "Services/DealerApplicationApi";
import InputNumber from "Components/Controls/InputNumber";
import AppCard from "Components/Controls/AppCard";
import InputState from "Components/Controls/InputState";
import InputStockType from "Components/Controls/InputStockType";
import InputVehicleCount from "Components/Controls/InputVehicleCount";
import InputDealerManagementSystem from "Components/Controls/InputDealerManagementSystem";
import { CardElement, useStripe, useElements } from "@stripe/react-stripe-js";
import notification from "Components/Utility/Notifications";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import '../../Signup/signup.css';
import Plans from '../../Signup/Plans/Plans';
import { IStripeCustomerDto } from "../../../../Models/Stripe/StripeCustomer";
import stripeApi from "../../../../Services/StripeApi";
import { IStripeSubscription } from "../../../../Models/Stripe/StripeSubscription";
import Loader from 'react-loader-spinner';

const validationSchema = yup.object().shape({
  dealershipName: yup.string().label("Dealership Name").required().max(100),
  firstName: yup.string().label("First Name").required().max(100),
  lastName: yup.string().label("Last Name").required().nullable()
    .max(100),
  email: yup.string().label("E-mail").required().nullable()
    .max(100),
  phone: yup.string().label("Phone").required().nullable()
    .max(100),
  dealerLicence: yup.string().label("Dealer Licence").required().max(100),
  dealerManagementSystem: yup.string().label("Dealer Management System").required().nullable()
    .max(100),
  address: yup.string().label("Address").required().nullable()
    .max(100),
  suburb: yup.string().label("Suburb").required().nullable()
    .max(100),
  state: yup.string().label("State").required().nullable()
    .max(20),
  postcode: yup.string().label("Postcode").required().nullable()
    .max(10),
  stockType: yup.number().label("Stock Type").required().nullable()
    .integer()
    .min(1, "Select stock type"),
  vehicleCount: yup.string().label("Vehicle Count").required().nullable()
    .max(100),
  sites: yup.number().label("Sites").required().integer()
    .min(1),
});

export default function DealerApplication() {
  const stripe: any = useStripe();
  const elements = useElements();
  const [loaderState, setLoaderState] = useState({ isLoading: false })
  const [dealerApplication, setDealerApplication] = useState(new DealerApplicationDto());
  const history = useHistory();
  const [selectedPlan, setSelectedPlan] = useState({
    id: "",
    SubscriptionName: "",
    Site: 0,
    AutomatedListingManagement: 0,
    QualifiedLeads: 0,
    EmailSupport: 0,
    SubscriptionPrice: 0,
    PhoneSupport: 0,
    MultipleSiteManagement: 0,
    PremiumServices: 0,
    AccountManager: 0,
    CustomPricing: 0,
  });
  const callback = useCallback((selectedPlanDetails) => {
    setSelectedPlan(selectedPlanDetails);
  }, []);
  async function saveDealerApplication(formData: DealerApplicationDto) {
    const response = await dealerApplicationApi.add(formData);
    setDealerApplication(response);
    return response;
  }
  function capitalizeFirstLetter(str: string): string {
    const plan = str.split("-");
    const first = `${plan[0].charAt(0).toUpperCase()}${plan[0].slice(1)}`;
    if (plan.length > 1) {
      return `${first}-${plan[1].charAt(0).toUpperCase()}${plan[1].slice(1)}`;
    }
    return first;
  }
  async function handleFormSubmit(formData: DealerApplicationDto, { setSubmitting }: FormikHelpers<DealerApplicationDto>) {
    if (elements == null) {
      return;
    }
    const { error, paymentMethod } = await stripe.createPaymentMethod({
      type: "card",
      card: elements.getElement(CardElement)
    });
    if (error) {
      toast.error(error.message);
      return;
    }
    var dealerCreatedData = await saveDealerApplication(formData);
    formData.dealerApplicationId = dealerCreatedData.dealerApplicationId;
    const stripeCustomer: IStripeCustomerDto = {
      email: formData.email || "",
      productPlan: "",
      stripeToken: "",
    };
    stripeApi.createCustomer(stripeCustomer).then((response) => {
      if (response) {
        const subscription: IStripeSubscription = {
          customer: response.id,
          paymentMethod: paymentMethod.id,
          unitPrice: selectedPlan.SubscriptionPrice,
          planName: capitalizeFirstLetter(selectedPlan.SubscriptionName),
          adminEmail: formData.email || ""
        };
        stripeApi.createSubscription(subscription).then((result) => {
          if (result) {
            toast.success("Subscription created successfully");
            //Stripe update user
            formData.planName = subscription.planName;
            formData.stripeCustomerId = response.id;
            dealerApplicationApi.updatePublic(formData).then((res) => {
              toast.success("User Created. Please login with credentials");
              setLoaderState({ isLoading: false });
            });
          } else {
            setLoaderState({ isLoading: false });
          }
        });
      } else {
        setLoaderState({ isLoading: false });
      }
    });
      setSubmitting(false);
     //  history.goBack();
       notification.success("Your application has been submitted");
  }
  function handleCancel() {
    history.goBack();
  }

  return (
    <div>
      {
        loaderState.isLoading ?
          <div className="loader_style">
            <Loader type="Circles" color="#00BFFF" height={80} width={80} />
          </div>
          : ""
      }
      <Formik
        initialValues={dealerApplication}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard>
              <Plans parentCallback={callback} />
            </AppCard>
            <AppCard title="Dealer Application for National Auto Network">
              <Row>
                <Col md>
                  <InputText name="dealershipName" label="Dealership Name" />
                </Col>
              </Row>
              <Row>
                <Col md>
                  <InputText name="address" label="Address" />
                </Col>
                <Col md>
                  <InputText name="suburb" label="Suburb" />
                </Col>
                <Col md={2}>
                  <InputText name="postcode" label="Postcode" />
                </Col>
                <Col md={2}>
                  <InputState />
                </Col>
              </Row>
              <Row>
                <Col md>
                  <InputStockType />
                </Col>
                <Col md>
                  <InputVehicleCount />
                </Col>
              </Row>
              <Row>
                <Col md>
                  <InputDealerManagementSystem />
                </Col>
                <Col md>
                  <InputNumber name="sites" label="Sites" />
                </Col>
              </Row>
            </AppCard>
            <AppCard title="Administrator Details">
              <Row>
                <Col md>
                  <InputText name="firstName" label="First Name" />
                </Col>
                <Col md>
                  <InputText name="lastName" label="Last Name" />
                </Col>
              </Row>
              <Row>
                <Col md>
                  <InputText name="email" label="E-mail" />
                </Col>
                <Col md>
                  <InputText name="phone" label="Phone" />
                </Col>
              </Row>
              <Row>
                <Col md={6}>
                  <InputText name="dealerLicence" label="Dealer Licence" />
                </Col>
              </Row>
            </AppCard>
            <AppCard title="Billing Section">
              <Row>
                <Col md>
                  <CardElement options={{ hidePostalCode: true }} />
                  <ToastContainer />
                </Col>
              </Row>
              <Row>
                <Col md>
                  <Button type="submit" className="margin_right" disabled={!stripe || !elements || isSubmitting}>Create Account & Buy Plan</Button>
                  <Button variant="secondary" disabled={isSubmitting} onClick={handleCancel}>Cancel</Button>
                </Col>
              </Row>
            </AppCard>
          </FormikForm>
        )}
      </Formik>
    </div>
  );
}