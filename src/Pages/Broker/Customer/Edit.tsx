import { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import { Button, Row, Col, Tabs, Tab, Form } from "react-bootstrap";
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
import ConfirmationModal from "Components/Modals/ConfirmationModal";
import CustomerMessages from "./CustomerMessages";
// import ActivityCard from "./Activity/ActivityCard";

const validationSchema = yup.object().shape({
  firstName: yup.string().label("First Name").required().max(100),
  lastName: yup.string().label("Last Name").required().max(100),
  email: yup.string().label("Email").required().email()
    .max(100),
  phone: yup.string().label("Phone").nullable().max(100),
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

export default function CustomerEdit() {
  const { customerId } = useParams<{ customerId?: string | undefined }>();
  const history = useHistory();
  const [customer, setCustomer] = useState(new CustomerDto());
  const [isConfirmationVisible, setIsConfirmationVisible] = useState(false);

  useEffect(() => {
    if (customerId) {
      findCustomer(customerId);
    } else {
      history.goBack();
    }
  }, [customerId, history]);

  async function findCustomer(id: string) {
    const data = await customerApi.find(id);
    setCustomer(data);
    return data;
  }

  async function saveCustomer(formData: CustomerDto) {
    const response = await customerApi.update(formData);
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

  function handleShowConfirmation() {
    setIsConfirmationVisible(true);
  }

  function handleArchiveOk() {
    setIsConfirmationVisible(false);
    // Archive
    history.push("/");
  }

  const handeCheck = (e:any)=>{
    if(e.target.checked){}
      // Sent Data to api endPoint
  }

  function handleArchiveCancel() {
    setIsConfirmationVisible(false);
  }
  function onConversationClick(conversationId: any) {
    
    history.push(`/broker/messages/conversation/${conversationId}`);

  }
  return (
    <div>
      <Formik
        initialValues={customer}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting, values }) => (
          <FormikForm autoComplete="off">
            <AppCard title={`${values?.firstName} ${values?.lastName}`}>
              <Tabs defaultActiveKey="details">
                <Tab eventKey="details" title="Details">
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
                  <Row className="d-flex align-items-center">
                    <Button variant="primary" className="mr-2" type="submit" disabled={isSubmitting}>Save</Button>
                    <Button variant="secondary" className="mr-2" onClick={() => handleCancel()}>Cancel</Button>
                    <Button variant="secondary" className="mr-2" onClick={handleShowConfirmation}>Archive</Button>
                    <Form.Group className="mb-0" id="formGridCheckbox">
                      <Form.Check type="checkbox" onChange={handeCheck} label="DealMyCar" />
                    </Form.Group>
                  </Row>
                </Tab>
                {/* <Tab eventKey="activity" title="Activity">
                  <img src={ActivityImage} alt="activity items" />
                  <ActivityCard />
                </Tab> */}
                <Tab eventKey="messages" title="Messages">
                  <CustomerMessages onConversationClick={onConversationClick} customerId={customer?.customerId} />     {/* import component to show in messgae tab  */}
                </Tab>
              </Tabs>
            </AppCard>
          </FormikForm>
        )}
      </Formik>

      <ConfirmationModal
        isVisible={isConfirmationVisible}
        onOkClick={handleArchiveOk}
        onCancelClick={handleArchiveCancel}
      >
        Are you sure you want to archive this customer?
      </ConfirmationModal>
    </div>
  );
}
