import { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import { Button, Row, Col } from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import * as yup from "yup";
import InputText from "Components/Controls/InputText";
import BrokerDto from "Models/Broker/BrokerDto";
import brokerApi from "Services/BrokerApi";
import AppCard from "Components/Controls/AppCard";

const validationSchema = yup.object().shape({
  firstName: yup.string().label("First name").required().max(100),
  lastName: yup.string().label("Last name").required().max(100),
  email: yup.string().label("E-mail").required().email()
    .max(100),
  phone: yup.string().label("Phone").nullable().max(100),
});

export default function BrokerEdit() {
  const { brokerageId, brokerId } = useParams<{brokerageId?: string | undefined, brokerId?: string | undefined }>();

  const [broker, setBroker] = useState(new BrokerDto());
  const [isNewBroker, setIsNewBroker] = useState(!(brokerId));

  const history = useHistory();

  useEffect(() => {
    if (brokerId) {
      findBroker(brokerId);
    } else {
      const newBroker = new BrokerDto();
      newBroker.brokerageId = String(brokerageId);
      setBroker(newBroker);
      setIsNewBroker(true);
    }
  }, [brokerId, brokerageId]);

  async function findBroker(id: string) {
    const data = await brokerApi.find(id);
    setBroker(data);
    setIsNewBroker(false);
    return data;
  }

  async function saveBroker(formData: BrokerDto) {
    let response: BrokerDto;
    if (isNewBroker) {
      response = await brokerApi.add(formData);
    } else {
      response = await brokerApi.update(formData);
    }
    setBroker(response);
    return response;
  }

  async function handleFormSubmit(formData: BrokerDto, { setSubmitting }: FormikHelpers<BrokerDto>) {
    await saveBroker(formData);
    setSubmitting(false);
    history.goBack();
  }

  function handleCancel() {
    history.goBack();
  }

  return (
    <div>
      <Formik
        initialValues={broker}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard title={`${isNewBroker ? "Add" : "Edit"} Broker`}>
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
                <Col md>
                  <Button type="submit" disabled={isSubmitting}>Save</Button>
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
