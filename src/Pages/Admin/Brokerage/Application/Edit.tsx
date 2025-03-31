import { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import { Button, Row, Col } from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import * as yup from "yup";
import InputText from "Components/Controls/InputText";
import BrokerageApplicationDto from "Models/BrokerageApplication/BrokerageApplicationDto";
import brokerageApplicationApi from "Services/BrokerageApplicationApi";
import AppCard from "Components/Controls/AppCard";
import InputSelect from "Components/Controls/InputSelect";
import InputAggregator from "Components/Controls/InputAggregator";
import InputState from "Components/Controls/InputState";
import notification from "Components/Utility/Notifications";

const validationSchema = yup.object().shape({
  brokerageName: yup.string().label("Brokerage Name").required().max(100),
  phone: yup.string().label("Phone").nullable().required()
    .max(100),
  address: yup.string().label("Address").nullable().max(100),
  suburb: yup.string().label("Suburb").nullable().max(100),
  state: yup.string().label("State").nullable().required()
    .max(100),
  postcode: yup.string().label("Postcode").nullable().required()
    .max(100),
  numberOfBrokers: yup.string().label("Number Of Brokers").nullable().required()
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

export default function BrokerageApplicationEdit() {
  const { brokerageApplicationId } = useParams<{ brokerageApplicationId?: string | undefined }>();
  const [brokerageApplication, setBrokerageApplication] = useState(new BrokerageApplicationDto());
  const history = useHistory();

  useEffect(() => {
    if (brokerageApplicationId) {
      findBrokerageApplication(brokerageApplicationId);
    } else {
      history.goBack();
    }
  }, [brokerageApplicationId, history]);

  async function findBrokerageApplication(id: string) {
    const data = await brokerageApplicationApi.find(id);
    setBrokerageApplication(data);
    return data;
  }

  async function handleFormSubmit(formData: BrokerageApplicationDto, { setSubmitting }: FormikHelpers<BrokerageApplicationDto>) {
    const brokerage = await brokerageApplicationApi.approve(formData);
    setSubmitting(false);
    notification.success("Brokerage approved.");
    history.push(`/admin/brokerage/edit/${brokerage.brokerageId}`);
  }

  function handleCancel() {
    history.goBack();
  }

  return (
    <div>
      <Formik
        initialValues={brokerageApplication}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard title="Review brokerage application">
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

              <Button variant="primary" type="submit" disabled={isSubmitting}>Approve Application</Button>
              <Button variant="secondary" className="ml-1" disabled={isSubmitting} onClick={handleCancel}>Cancel</Button>
            </AppCard>

          </FormikForm>
        )}
      </Formik>
    </div>
  );
}
