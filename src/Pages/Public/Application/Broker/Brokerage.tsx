import { Button, Col, Row } from "react-bootstrap";
import AppCard from "Components/Controls/AppCard";
import React, { useState, useCallback } from "react";
import InputText from "Components/Controls/InputText";
import InputSelect from "Components/Controls/InputSelect";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import { useHistory } from "react-router-dom";
import * as yup from "yup";
import BrokerageApplicationDto from "Models/BrokerageApplication/BrokerageApplicationDto";
import brokerageApplicationApi from "Services/BrokerageApplicationApi";
import InputAggregator from "Components/Controls/InputAggregator";
import InputState from "Components/Controls/InputState";
import notification from "Components/Utility/Notifications";
import { CardElement, useStripe, useElements } from "@stripe/react-stripe-js";
//import { loadStripe } from "@stripe/stripe-js";
import '../../Signup/signup.css';
import Plans from '../../Signup/Plans/Plans';
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import stripeApi from "../../../../Services/StripeApi";
import { IStripeCustomerDto } from "../../../../Models/Stripe/StripeCustomer";
import { IStripeSubscription } from "../../../../Models/Stripe/StripeSubscription";
import Loader from 'react-loader-spinner';

const validationSchema = yup.object().shape({
  brokerageName: yup.string().label("Brokerage Name").required().max(100),
  phone: yup.string().label("Phone").nullable().required()
    .max(100),
  address: yup.string().label("Address").nullable().required()
    .max(100),
  suburb: yup.string().label("Suburb").nullable().required()
    .max(100),
  state: yup.string().label("State").nullable().required()
    .max(100),
  postcode: yup.string().label("Postcode").nullable().required()
    .max(100),
  numberOfBrokers: yup.string().label("Number of Brokers").nullable().required()
    .max(100),
  aggregatorPartner: yup.string().label("Aggregator Partner").nullable().required()
    .max(100),
  firstName: yup.string().label("First Name").nullable().required()
    .max(100),
  lastName: yup.string().label("Last Name").nullable().required()
    .max(100),
  adminEmail: yup.string().label("E-mail").nullable().required()
    .email()
    .max(100),
  adminPhone: yup.string().label("Phone").nullable().required()
    .max(100),
  // Either creditLicence or authorisedCreditRep must be entered
  creditLicence: yup.string()
    .when("authorisedCreditRep", {
      is: (authorisedCreditRep: string) => !authorisedCreditRep?.length,
      then: yup.string().label("Credit Licence").nullable().required()
        .max(100),
      otherwise: yup.string().label("Credit Licence").nullable().max(100),
    }),
  authorisedCreditRep: yup.string()
    .when("creditLicence", {
      is: (creditLicence: string) => !creditLicence?.length,
      then: yup.string().label("Authorised Credit Rep").nullable().required()
        .max(100),
      otherwise: yup.string().label("Authorised Credit Rep").nullable().max(100),
    }),
}, [["creditLicence", "authorisedCreditRep"]]);
//const stripePromise = loadStripe("pk_test_51JhU3mHuSqqcN6Xk5JGdsIr0v3tG1oDIYGR8rkm5dUrT3Rx2MOcuXG1OXhYVMoVh8r4GAcBMALdqlI63AuPdMwia00DW0Icn5K");

export default function BrokerageApplicationEdit() {
  const [brokerageApplication] = useState(new BrokerageApplicationDto());
  const [loaderState, setLoaderState] = useState({ isLoading: false })
  const stripe: any = useStripe();
  const elements = useElements();
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
  const history = useHistory();

  async function saveBrokerageApplication(formData: BrokerageApplicationDto) {
    return await brokerageApplicationApi.add(formData);
  }

  function capitalizeFirstLetter(str: string): string {
    const plan = str.split("-");
    const first = `${plan[0].charAt(0).toUpperCase()}${plan[0].slice(1)}`;
    if (plan.length > 1) {
      return `${first}-${plan[1].charAt(0).toUpperCase()}${plan[1].slice(1)}`;
    }
    return first;
  }
  async function handleFormSubmit(formData: BrokerageApplicationDto, { setSubmitting }: FormikHelpers<BrokerageApplicationDto>) {
    if (elements == null) {
      return;
    }
    const { error, paymentMethod } = await stripe.createPaymentMethod({
      type: "card",
      card: elements.getElement(CardElement)
    });
    if (error) {
      toast.error(error.message);
      setLoaderState({ isLoading: false });
      return;
    }
    var brokerCreatedData = await saveBrokerageApplication(formData);
    formData.brokerageApplicationId = brokerCreatedData.brokerageApplicationId;
    const stripeCustomer: IStripeCustomerDto = {
      email: formData.adminEmail,
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
          adminEmail: formData.adminEmail 
        };

        stripeApi.createSubscription(subscription).then((result) => {
          if (result) {
            toast.success("Subscription created successfully");
            //Stripe update user
            formData.planName = subscription.planName;
            formData.stripeCustomerId = response.id;
            brokerageApplicationApi.updatePublic(formData).then((res) => {
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
   // history.goBack();
    notification.success("Your application has been submitted");
  }

  function handleCancel() {
    history.goBack();
  }
  const callback = useCallback((selectedPlanDetails) => {
    setSelectedPlan(selectedPlanDetails);
  }, []);
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
        initialValues={brokerageApplication}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard>
              <Plans parentCallback={callback} />
            </AppCard>
            <AppCard title="Brokerage application for National Auto Network">
              <Row>
                <Col md><InputText name="brokerageName" label="Company name" /></Col>
                <Col md><InputText name="phone" label="Phone" /></Col>
              </Row>
              <Row>
                <Col md>
                  <InputText name="address" label="Address" />
                </Col>
                <Col md>
                  <InputText name="suburb" label="Suburb" />
                </Col>
                <Col md={2}><InputText name="postcode" label="Postcode" /></Col>
                <Col md={2}><InputState /></Col>
              </Row>
              <Row>
                <Col md><InputText name="creditLicence" label="Australian Credit Licence" /></Col>
                <Col md={6}><InputText name="authorisedCreditRep" label="Authorised Credit Representative" /></Col>
              </Row>
              <Row>
                <Col md>
                  <InputAggregator />
                </Col>
                <Col md={6} sm>
                  <InputSelect
                    name="numberOfBrokers"
                    label="Number of Brokers"
                    options={[
                      { value: "1-5", label: "1-5" },
                      { value: "6-10", label: "6-10" },
                      { value: "10+", label: "10+" },
                    ]}
                  />
                </Col>
              </Row>
            </AppCard>
            <AppCard title="Administrator details">
              <Row>
                <Col md><InputText name="firstName" label="First name" /></Col>
                <Col md><InputText name="lastName" label="Last name" /></Col>
              </Row>
              <Row>
                <Col md><InputText name="adminEmail" label="E-mail" /></Col>
                <Col md><InputText name="adminPhone" label="Phone" /></Col>
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
                  <Button variant="primary" className="margin_right" type="submit" disabled={!stripe || !elements || isSubmitting}>Create Account & Buy Plan</Button>
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
