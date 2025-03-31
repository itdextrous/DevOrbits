import { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import { Button, Row, Col } from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import * as yup from "yup";
import InputText from "Components/Controls/InputText";
import DealerApplicationDto from "Models/DealerApplication/DealerApplicationDto";
import dealerApplicationApi from "Services/DealerApplicationApi";
import InputNumber from "Components/Controls/InputNumber";
import AppCard from "Components/Controls/AppCard";
import InputStockType from "Components/Controls/InputStockType";
import InputVehicleCount from "Components/Controls/InputVehicleCount";
import InputState from "Components/Controls/InputState";
import InputDealerManagementSystem from "Components/Controls/InputDealerManagementSystem";
import notification from "Components/Utility/Notifications";

const validationSchema = yup.object().shape({
  dealershipName: yup.string().label("Dealership Name").required().max(100),
  firstName: yup.string().label("First Name").required().max(100),
  lastName: yup.string().label("Last Name").nullable().max(100),
  email: yup.string().label("E-mail").nullable().max(100),
  phone: yup.string().label("Phone").nullable().max(100),
  dealerLicence: yup.string().label("Dealer Licence").required().max(100),
  dealerManagementSystem: yup.string().label("Dealer Management System").required().max(100),
  address: yup.string().label("Address").nullable().max(100),
  suburb: yup.string().label("Suburb").nullable().max(100),
  state: yup.string().label("State").nullable().max(20),
  postcode: yup.string().label("Postcode").nullable().max(10),
  stockType: yup.number().label("Stock Type").required().integer()
    .min(1, "Select stock type"),
  vehicleCount: yup.string().label("Vehicle Count").nullable().max(100),
  sites: yup.number().label("Sites").required(),
});

export default function DealerApplicationEdit() {
  const { dealerApplicationId } = useParams<{ dealerApplicationId?: string | undefined }>();

  const [dealerApplication, setDealerApplication] = useState(new DealerApplicationDto());

  const history = useHistory();

  useEffect(() => {
    if (dealerApplicationId) {
      findDealerApplication(dealerApplicationId);
    } else {
      history.goBack();
    }
  }, [dealerApplicationId, history]);

  async function findDealerApplication(id: string) {
    const data = await dealerApplicationApi.find(id);
    setDealerApplication(data);
    return data;
  }

  async function handleFormSubmit(formData: DealerApplicationDto, { setSubmitting }: FormikHelpers<DealerApplicationDto>) {
    const dealer = await dealerApplicationApi.approve(formData);
    setSubmitting(false);
    notification.success("Dealer approved.");
    history.push(`/admin/dealer/edit/${dealer.dealerId}`);
  }

  function handleCancel() {
    history.goBack();
  }

  return (
    <div>
      <Formik
        initialValues={dealerApplication}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard title="Review Dealer Application">
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
                <Col>
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

              <Button type="submit" disabled={isSubmitting}>Approve Application</Button>
              <Button variant="secondary" className="mx-1" disabled={isSubmitting} onClick={handleCancel}>Delete</Button>
              <Button variant="secondary" disabled={isSubmitting} onClick={handleCancel}>Cancel</Button>
            </AppCard>
          </FormikForm>
        )}
      </Formik>
    </div>
  );
}
