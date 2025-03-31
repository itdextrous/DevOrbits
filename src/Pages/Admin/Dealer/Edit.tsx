import { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import { Button, Row, Col } from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import * as yup from "yup";
import InputText from "Components/Controls/InputText";
import DealerDto from "Models/Dealer/DealerDto";
import dealerApi from "Services/DealerApi";
import AppCard from "Components/Controls/AppCard";
import InputNumber from "Components/Controls/InputNumber";
import InputState from "Components/Controls/InputState";
import InputStockType from "Components/Controls/InputStockType";
import InputVehicleCount from "Components/Controls/InputVehicleCount";
import InputDealerManagementSystem from "Components/Controls/InputDealerManagementSystem";

const validationSchema = yup.object().shape({
  dealershipName: yup.string().label("Dealership Name").required().max(100),
  firstName: yup.string().label("First Name").required().max(100),
  lastName: yup.string().label("Last Name").required().max(100),
  email: yup.string().label("E-mail").nullable().email()
    .max(100),
  phone: yup.string().label("Phone").nullable().max(100),
  dealerLicence: yup.string().label("Dealer Licence").nullable().max(100),
  dealerManagementSystem: yup.string().label("Dealer Management System").nullable().max(100),
  address: yup.string().label("Address").nullable().max(100),
  suburb: yup.string().label("Suburb").nullable().max(100),
  state: yup.string().label("State").nullable().max(20),
  postcode: yup.string().label("Postcode").nullable().max(10),
  stockType: yup.number().label("Stock Type").required().integer()
    .min(1, "Select stock type"),
  vehicleCount: yup.string().label("Vehicle Count").max(100),
  sites: yup.number().label("Sites").required().min(1),
});

export default function DealerEdit() {
  const { dealerId } = useParams<{ dealerId?: string | undefined }>();

  const [dealer, setDealer] = useState(new DealerDto());
  const [isNewDealer, setIsNewDealer] = useState(!(dealerId));

  const history = useHistory();

  useEffect(() => {
    if (dealerId) {
      findDealer(dealerId);
    } else {
      setDealer(new DealerDto());
      setIsNewDealer(true);
    }
  }, [dealerId]);

  async function findDealer(id: string) {
    const data = await dealerApi.find(id);
    setDealer(data);
    setIsNewDealer(false);
    return data;
  }

  async function saveDealer(formData: DealerDto) {
    let response: DealerDto;

    if (isNewDealer) {
      response = await dealerApi.add(formData);
    } else {
      response = await dealerApi.update(formData);
    }

    setDealer(response);
    return response;
  }

  async function handleFormSubmit(formData: DealerDto, { setSubmitting }: FormikHelpers<DealerDto>) {
    await saveDealer(formData);
    setSubmitting(false);
    history.goBack();
  }

  function handleCancel() {
    history.goBack();
  }
  return (
    <div>
      <Formik
        initialValues={dealer}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard title={`${isNewDealer ? "Add" : "Edit"} Dealer`}>

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

            <AppCard title="Administrator for Dealership">
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

              <Row>
                <Col md>
                  <Button type="submit" disabled={isSubmitting}>Save</Button>
                  <Button variant="secondary" className="ml-1" disabled={isSubmitting} onClick={handleCancel}>Cancel</Button>
                </Col>
              </Row>
            </AppCard>
          </FormikForm>
        )}
      </Formik>
    </div>
  );
}
