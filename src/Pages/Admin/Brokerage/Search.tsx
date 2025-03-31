import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Button, Col, Row, Table,
} from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import InputText from "Components/Controls/InputText";
import brokerageApi from "Services/BrokerageApi";
import BrokerageSearchCriteriaDto from "Models/Brokerage/BrokerageSearchCriteriaDto";
import BrokerageSearchResultsDto from "Models/Brokerage/BrokerageSearchResultsDto";
import AppCard from "Components/Controls/AppCard";

export default function BrokerageSearch() {
  const [criteria, setCriteria] = useState(new BrokerageSearchCriteriaDto());
  const [searchResults, setSearchResults] = useState(new BrokerageSearchResultsDto());

  useEffect(() => {
    brokerageApi.search(criteria)
      .then((results) => {
        setSearchResults(results);
      });
  }, [criteria]);

  function handleFormSubmit(values: BrokerageSearchCriteriaDto, { setSubmitting }: FormikHelpers<BrokerageSearchCriteriaDto>) {
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
            <AppCard title="Brokerage search">
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
                  <Button type="submit" disabled={isSubmitting}>Search</Button>
                  <Button
                    type="reset"
                    variant="secondary"
                    className="mx-1"
                    disabled={isSubmitting}
                    onClick={handleReset}
                  >Clear
                  </Button>
                  <Link to="/admin/brokerage/edit" className="btn btn-secondary">Add New</Link>
                </Col>
              </Row>
            </AppCard>
          </FormikForm>
        )}
      </Formik>

      {searchResults?.brokerageList && (
        <AppCard>
          <Table responsive hover>
            <thead>
              <tr>
                <th>Name</th>
                <th>Phone</th>
                <th>State</th>
              </tr>
            </thead>
            <tbody>
              {searchResults?.brokerageList?.map((item) => (
                <tr key={item.brokerageId}>
                  <td>
                    <Link to={`/admin/brokerage/edit/${item.brokerageId}`}>{item.name}</Link>
                  </td>
                  <td>{item.phone}</td>
                  <td>{item.state}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        </AppCard>
      )}

    </>
  );
}
