import { useState } from "react";
import { useHistory } from "react-router-dom";
import {
  Alert, Button, Row, Col,
} from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import * as yup from "yup";
import CustomerDto from "Models/Customer/CustomerDto";
import customerApi from "Services/CustomerApi";
import AppCard from "Components/Controls/AppCard";
import InputText from "Components/Controls/InputText";
import InputTextArea from "Components/Controls/InputTextArea";
import InputDate from "Components/Controls/InputDate";
import InputNumber from "Components/Controls/InputNumber";
import InputState from "Components/Controls/InputState";
import InputApplicationStatus from "Components/Controls/InputApplicationStatus";

//eslint-disable-next-line
const phoneValidateRegex = /^\({0,1}((0|\+61)(2|4|3|7|8)){0,1}\){0,1}(\ |-){0,1}[0-9]{2}(\ |-){0,1}[0-9]{2}(\ |-){0,1}[0-9]{1}(\ |-){0,1}[0-9]{3}$/;

// const  country_codes =['+61-','UK-44-']
//  const phone ="61-1234567890";
//  const is_valid_number=country_codes.some(elem =>phone.match('^'+elem));

const validationSchema = yup.object().shape({
  firstName: yup.string().label("First Name").required().max(100),
  lastName: yup.string().label("Last Name").required().max(100),
  email: yup.string().label("Email").required().email().max(100),
  phone: yup.string().matches(phoneValidateRegex, 'Please add(+61) with valid number').required("Phone number is required field").label("Phone").nullable().min(12).max(20),
  address: yup.string().label("Address").nullable().max(100),
  suburb: yup.string().label("Suburb").nullable().max(100),
  state: yup.string().label("State").nullable().max(100),
  postcode: yup.string().label("Postcode").nullable().max(100),
  applicationStatus: yup.number().label("Application Status").required().min(1, "Select the application status"),
  approvedDate: yup.string().label("Approved Date").nullable(),
  financier: yup.string().label("Financier").nullable().max(100),
  approvalAmount: yup.number().label("Approval Amount").nullable(),
  vehicleRequirements: yup.string().label("Vehicle Requirements").nullable(),
  budget: yup.string().label("Budget").nullable().max(100),
  availableDeposit: yup.number().label("Available Deposit").nullable(),
  shortlistSubmittedDate: yup.string().label("Shortlist Submitted Date").nullable(),
});

export default function CustomerInvite() {
  const [customer, setCustomer] = useState(new CustomerDto());
  const history = useHistory();

  async function saveCustomer(formData: CustomerDto) {
    const response = await customerApi.add(formData);
    setCustomer(response);
    return response;
  }

  async function handleFormSubmit(formData: CustomerDto, { setSubmitting }: FormikHelpers<CustomerDto>) {
    await saveCustomer(formData);
    setSubmitting(false);
    history.goBack();
  }

  function handleCancel() {
    history.goBack();
  }

  return (
    <div>
      <Formik
        initialValues={customer}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard title="Invite Customer">
              <Row>
                <Col md>
                  <InputText name="firstName" label="First Name" />
                </Col>
                <Col md>
                  <InputText name="lastName" label="Last Name" />
                </Col>
                <Col md>
                  <InputText name="email" label="E-mail" />
                </Col>
                <Col md={3}>
                  <InputText name="phone" label="Phone" />
                </Col>
              </Row>
              <Row>
                <Col md>
                  <InputText name="address" label="Address" />
                </Col>
                <Col md>
                  <InputText label="Suburb" />
                </Col>
                <Col md>
                  <InputState />
                </Col>
                <Col md>
                  <InputText name="postcode" label="Postcode" />
                </Col>
              </Row>

              <Row>
                <Col md>
                  <InputApplicationStatus />
                </Col>
                <Col md>
                  <InputDate name="approvedDate" label="Approved Date" />
                </Col>
              </Row>

              <Row>
                <Col md>
                  <InputText name="financier" label="Financier" />
                </Col>
                <Col md>
                  <InputNumber name="approvalAmount" label="Approval Amount" allowDecimals />
                </Col>
              </Row>
              <Row>
                <Col md>
                  <InputTextArea name="vehicleRequirements" label="Vehicle Requirements" />
                </Col>
              </Row>
              <Row>
                <Col md>
                  <InputText name="budget" label="Budget" />
                </Col>
                <Col md>
                  <InputNumber name="availableDeposit" label="Available Deposit" allowDecimals />
                </Col>
                <Col md>
                  <InputDate name="shortlistSubmittedDate" label="Shortlist Submitted" />
                </Col>
              </Row>
              <Alert variant="info">
                The customer will be sent an invite code by email/txt so they can login to the National Auto Network portal.
              </Alert>
              <Button variant="primary" type="submit" disabled={isSubmitting}>Invite Customer</Button>
              <Button variant="secondary" onClick={() => handleCancel()}>Cancel</Button>
            </AppCard>
          </FormikForm>
        )}
      </Formik>
    </div>
  );
}
