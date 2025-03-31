import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Button, Col, Row, Table} from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import InputText from "Components/Controls/InputText";
import brokerageApplicationApi from "Services/BrokerageApplicationApi";
import BrokerageApplicationSearchCriteriaDto from "Models/BrokerageApplication/BrokerageApplicationSearchCriteriaDto";
import BrokerageApplicationSearchResultsDto from "Models/BrokerageApplication/BrokerageApplicationSearchResultsDto";
import AppCard from "Components/Controls/AppCard";
import InputState from "Components/Controls/InputState";

export default function BrokerageApplicationSearch() {
  const [criteria, setCriteria] = useState(new BrokerageApplicationSearchCriteriaDto());
  const [searchResults, setSearchResults] = useState(new BrokerageApplicationSearchResultsDto());

  useEffect(() => {
    brokerageApplicationApi.search(criteria)
      .then((results) => {
        setSearchResults(results);
      });
  }, [criteria]);

  function handleFormSubmit(values: BrokerageApplicationSearchCriteriaDto, { setSubmitting }: FormikHelpers<BrokerageApplicationSearchCriteriaDto>) {
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
            <AppCard title="Brokerage Applications">
              <Row>
                <Col md>
                  <InputText name="brokerageName" label="Name" placeholder="Name" />
                </Col>
                <Col md>
                  <InputText name="phone" label="Phone" placeholder="Phone Number" />
                </Col>
                <Col md>
                  <InputState />
                </Col>
              </Row>

              <Row>
                <Col md>
                  <Button type="submit" disabled={isSubmitting}>Search</Button>
                  <Button
                    type="reset"
                    variant="secondary"
                    className="ml-1"
                    disabled={isSubmitting}
                    onClick={handleReset}
                  >Clear
                  </Button>
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
              <th>Name</th>
              <th>Phone</th>
              <th>State</th>
            </tr>
          </thead>
          <tbody>
            {searchResults?.brokerageApplicationList?.map((item) => (
              <tr key={item.brokerageApplicationId}>
                <td>
                  <Link to={`/admin/brokerage/application/edit/${item.brokerageApplicationId}`}>
                    {item.brokerageName}
                  </Link>
                </td>
                <td>{item.phone}</td>
                <td>{item.state}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      </AppCard>
    </>
  );
}
