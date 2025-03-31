import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Button, Col, Row, Table,
} from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import InputText from "Components/Controls/InputText";
import brokerApi from "Services/BrokerApi";
import BrokerSearchCriteriaDto from "Models/Broker/BrokerSearchCriteriaDto";
import BrokerSearchResultsDto from "Models/Broker/BrokerSearchResultsDto";
import InputSelect from "Components/Controls/InputSelect";
import AppCard from "Components/Controls/AppCard";

export default function BrokerSearch() {
  const [criteria, setCriteria] = useState(new BrokerSearchCriteriaDto());
  const [searchResults, setSearchResults] = useState(new BrokerSearchResultsDto());

  useEffect(() => {
    brokerApi.search(criteria)
      .then((results) => {
        setSearchResults(results);
      });
  }, [criteria]);

  function handleFormSubmit(values: BrokerSearchCriteriaDto, { setSubmitting }: FormikHelpers<BrokerSearchCriteriaDto>) {
    setCriteria(values);
    setSubmitting(false);
  }

  return (
    <>
      <Formik
        initialValues={criteria}
        onSubmit={handleFormSubmit}
        onReset={handleFormSubmit}
      >
        {({ isSubmitting, handleReset }) => (
          <FormikForm autoComplete="off">
            <AppCard title="Broker search">
              <Row>
                <Col md>
                  <InputSelect
                    name="brokerageId"
                    label="Brokerage Id"
                    options={[]}
                  />
                </Col>
                <Col md>
                  <InputText name="firstName" label="First Name" />
                </Col>
                <Col md>
                  <InputText name="lastName" label="Last Name" />
                </Col>
                <Col md>
                  <InputText name="email" label="E-mail" />
                </Col>
                <Col md>
                  <InputText name="phone" label="Phone" />
                </Col>
              </Row>

              <Row>
                <Col md>
                  <Button type="submit" disabled={isSubmitting}>Search</Button>
                  <Button
                    type="reset"
                    variant="secondary"
                    disabled={isSubmitting}
                    onClick={handleReset}
                  >Clear
                  </Button>
                  <Link to="/admin/broker/edit" className="btn btn-secondary">Add New</Link>
                </Col>
              </Row>
            </AppCard>
          </FormikForm>
        )}
      </Formik>

      <AppCard>
        <Table responsive hover>
          <thead>
            <tr>
              <th>Brokerage</th>
              <th>Name</th>
              <th>E-mail</th>
              <th>Phone</th>
            </tr>
          </thead>
          <tbody>
            {searchResults?.brokerList?.map((item) => (
              <tr key={item.brokerId}>
                <td>{item.brokerageId}</td>
                <td>
                  <Link to={`/admin/broker/edit/${item.brokerId}`}>
                    {`${item.firstName} ${item.lastName}`}
                  </Link>
                </td>
                <td>{item.email}</td>
                <td>{item.phone}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      </AppCard>
    </>
  );
}
