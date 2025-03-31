import { useEffect, useState } from "react";
import { Link, useHistory, useParams } from "react-router-dom";
import {
  Button, Row, Col, Table,
} from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import * as yup from "yup";
import InputText from "Components/Controls/InputText";
import BrokerageDto from "Models/Brokerage/BrokerageDto";
import brokerageApi from "Services/BrokerageApi";
import InputTextArea from "Components/Controls/InputTextArea";
import AppCard from "Components/Controls/AppCard";
import InputState from "Components/Controls/InputState";
import BrokerSearchDto from "Models/Broker/BrokerSearchDto";
import brokerApi from "Services/BrokerApi";
import BrokerSearchCriteriaDto from "Models/Broker/BrokerSearchCriteriaDto";
import InputAggregator from "Components/Controls/InputAggregator";

const validationSchema = yup.object().shape({
  name: yup.string().label("Name").required().max(100),
  phone: yup.string().label("Phone").nullable().max(100),
  address: yup.string().label("Address").nullable().max(100),
  suburb: yup.string().label("Suburb").nullable().max(100),
  state: yup.string().label("State").nullable().max(100),
  postcode: yup.string().label("Postcode").nullable().max(100),
  creditLicence: yup.string().label("Credit Licence").nullable().max(100),
  authorisedCreditRep: yup.string().label("Authorised Credit Rep").nullable().max(100),
  numberOfBrokers: yup.string().label("Number of Brokers").nullable().max(100),
  aggregatorPartner: yup.string().label("Aggregator Partner").nullable().max(100),
  notes: yup.string().label("Notes").max(6000),
});

export default function BrokerageEdit() {
  const { brokerageId } = useParams<{ brokerageId?: string | undefined }>();

  const [brokerage, setBrokerage] = useState(new BrokerageDto());
  const [isNewBrokerage, setIsNewBrokerage] = useState(!(brokerageId));
  const [brokers, setBrokers] = useState<BrokerSearchDto[]>([]);

  const history = useHistory();

  useEffect(() => {
    if (brokerageId) {
      findBrokerage(brokerageId);
    } else {
      setBrokerage(new BrokerageDto());
      setIsNewBrokerage(true);
    }
  }, [brokerageId]);

  async function findBrokerage(id: string) {
    const data = await brokerageApi.find(id);
    setBrokerage(data);
    setIsNewBrokerage(false);

    const brokerCriteria = new BrokerSearchCriteriaDto();
    brokerCriteria.brokerageId = id;
    const { brokerList } = await brokerApi.search(brokerCriteria);
    setBrokers(brokerList);
    return data;
  }

  async function saveBrokerage(formData: BrokerageDto) {
    let response: BrokerageDto;

    if (isNewBrokerage) {
      response = await brokerageApi.add(formData);
    } else {
      response = await brokerageApi.update(formData);
    }

    setBrokerage(response);
    return response;
  }

  async function handleFormSubmit(formData: BrokerageDto, { setSubmitting }: FormikHelpers<BrokerageDto>) {
    await saveBrokerage(formData);
    setSubmitting(false);
    history.goBack();
  }

  function handleCancel() {
    history.goBack();
  }

  return (
    <div>
      <Formik
        initialValues={brokerage}
        enableReinitialize
        validationSchema={validationSchema}
        onSubmit={handleFormSubmit}
      >
        {({ isSubmitting }) => (
          <FormikForm autoComplete="off">
            <AppCard title={`${isNewBrokerage ? "Add" : "Edit"} Brokerage`}>
              <Row>
                <Col md>
                  <InputText name="name" label="Name" />
                </Col>
                <Col md>
                  <InputText name="phone" label="Phone" />
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
                  <InputText name="creditLicence" label="Credit Licence" />
                </Col>
                <Col md>
                  <InputText name="authorisedCreditRep" label="Authorised Credit Rep" />
                </Col>
              </Row>

              <Row>
                <Col md>
                  <InputAggregator />
                </Col>
                <Col md>
                  <InputText name="numberOfBrokers" label="Number Of Brokers" />
                </Col>
              </Row>

              <Row>
                <Col md>
                  <InputTextArea name="notes" label="Notes" />
                </Col>
              </Row>

              <Button type="submit" disabled={isSubmitting}>Save</Button>
              <Button
                variant="secondary"
                className="ml-1"
                disabled={isSubmitting}
                onClick={handleCancel}
              >Cancel
              </Button>
              <Link
                to={`/admin/broker/add/${brokerageId}`}
                hidden={isNewBrokerage}
                className="btn btn-secondary"
              >Add Broker
              </Link>
            </AppCard>
          </FormikForm>
        )}
      </Formik>

      {!isNewBrokerage && brokers && (
        <AppCard title="Brokers">
          <Table responsive hover>
            <thead>
              <tr>
                <th>Name</th>
                <th>Phone</th>
                <th>E-mail</th>
              </tr>
            </thead>
            <tbody>
              {brokers?.map((broker) => (
                <tr key={broker.brokerId}>
                  <td>
                    <Link to={`/admin/broker/edit/${broker.brokerId}`}>{`${broker.firstName} ${broker.lastName}`}</Link>
                  </td>
                  <td>{broker.phone}</td>
                  <td>{broker.email}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        </AppCard>
      )}
    </div>
  );
}
