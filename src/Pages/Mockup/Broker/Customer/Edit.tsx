import {
  Button, Col, Row, Tab, Tabs,
} from "react-bootstrap";
import AppCard from "Components/Controls/AppCard";
import React from "react";
import InputText from "Components/Controls/InputText";
import InputTextArea from "Components/Controls/InputTextArea";
import InputDate from "Components/Controls/InputDate";
import InputNumber from "Components/Controls/InputNumber";
import { Formik, Form as FormikForm } from "formik";
import InputSelect from "Components/Controls/InputSelect";
import InputState from "Components/Controls/InputState";
import ActivityImage from "./customer-activity.jpg";
import MessagesImage from "./customer-messages.jpg";

export default function CustomerEdit() {
  return (
    <div>
      <Formik
        initialValues={{}}
        onSubmit={() => { }}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard title="Sue Williams">
              <Tabs defaultActiveKey="details">
                <Tab eventKey="details" title="Details">
                  <Row>
                    <Col md>
                      <InputText label="Customer Name" />
                    </Col>
                    <Col md>
                      <InputText label="E-mail" />
                    </Col>
                    <Col md={3}>
                      <InputText label="Phone" />
                    </Col>
                  </Row>
                  <Row>
                    <Col md>
                      <InputText label="Suburb" />
                    </Col>
                    <Col md>
                      <InputState />
                    </Col>
                  </Row>
                  <Row>
                    <Col md>
                      <InputSelect
                        label="Application Status"
                        options={[
                          { value: "Approved", label: "Approved" },
                          { value: "Decision Pending", label: "Decision Pending" },
                          { value: "Application to be submitted", label: "Application to be submitted" },
                          { value: "Declined / Re-work needed", label: "Declined / Re-work needed" },
                        ]}
                      />
                    </Col>
                    <Col md>
                      <InputDate label="Approved Date" />
                    </Col>
                  </Row>
                  <Row>
                    <Col md>
                      <InputText label="Financier" />
                    </Col>
                    <Col md>
                      <InputNumber label="Approval Amount" allowDecimals />
                    </Col>
                  </Row>
                  <Row>
                    <Col md>
                      <InputTextArea label="Vehicle Requirements" />
                    </Col>
                  </Row>
                  <Row>
                    <Col md>
                      <InputText label="Budget" />
                    </Col>
                    <Col md>
                      <InputNumber label="Available Deposit" allowDecimals />
                    </Col>
                    <Col md>
                      <InputDate label="Shortlist Submitted" />
                    </Col>
                  </Row>
                  <Button variant="primary" type="submit" disabled={isSubmitting}>Save</Button>
                  <Button variant="secondary">Cancel</Button>

                </Tab>
                <Tab eventKey="messages" title="Messages">
                  <img src={MessagesImage} alt="activity items" />
                </Tab>
                <Tab eventKey="activity" title="Activity">
                  <img src={ActivityImage} alt="activity items" />
                </Tab>
              </Tabs>
            </AppCard>
          </FormikForm>
        )}
      </Formik>
    </div>
  );
}
