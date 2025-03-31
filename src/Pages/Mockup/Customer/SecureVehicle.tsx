import { Button, Col, Row } from "react-bootstrap";
import { Formik, Form as FormikForm } from "formik";
import AppCard from "Components/Controls/AppCard";
import InputDate from "Components/Controls/InputDate";

export default function SecureVehicle() {
  return (
    <>
      <Formik
        initialValues={{ }}
        onSubmit={() => { }}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard title="Secure Car">
              <p>To secure this vehicle you must pay a holding deposit.</p>
              <p>Holding Deposit <b>$500</b></p>
              <Row>
                <Col md={4}>
                  <InputDate label="Preferred delivery date" />
                </Col>
              </Row>

              <Button disabled={isSubmitting}>Pay Now</Button>
            </AppCard>
          </FormikForm>
        )}
      </Formik>

    </>
  );
}
