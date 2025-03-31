import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Button, Col, Row, Table,
} from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import InputText from "Components/Controls/InputText";
import dealerApplicationApi from "Services/DealerApplicationApi";
import DealerApplicationSearchCriteriaDto from "Models/DealerApplication/DealerApplicationSearchCriteriaDto";
import DealerApplicationSearchResultsDto from "Models/DealerApplication/DealerApplicationSearchResultsDto";
import AppCard from "Components/Controls/AppCard";

export default function DealerApplicationSearch() {
  const [criteria, setCriteria] = useState(new DealerApplicationSearchCriteriaDto());
  const [searchResults, setSearchResults] = useState(new DealerApplicationSearchResultsDto());

  useEffect(() => {
    dealerApplicationApi.search(criteria)
      .then((results) => {
        setSearchResults(results);
      });
  }, [criteria]);

  function handleFormSubmit(values: DealerApplicationSearchCriteriaDto, { setSubmitting }: FormikHelpers<DealerApplicationSearchCriteriaDto>) {
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
            <AppCard title="Dealer application search">
              <Row>
                <Col md>
                  <InputText name="dealershipName" label="Dealership" />
                </Col>
                <Col md>
                  <InputText name="contactName" label="Contact" />
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
                    className="mx-1"
                    disabled={isSubmitting}
                    onClick={handleReset}
                  >Clear
                  </Button>
                  <Link to="/admin/dealer/application/edit" className="btn btn-secondary">Add New</Link>
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
              <th>Dealership Name</th>
              <th>Contact Name</th>
              <th>E-mail</th>
              <th>Phone</th>
              <th>State</th>
              <th>Postcode</th>
            </tr>
          </thead>
          <tbody>
            {searchResults?.dealerApplicationList?.map((item) => (
              <tr key={item.dealerApplicationId}>
                <td>
                  <Link to={`/admin/dealer/application/edit/${item.dealerApplicationId}`}>
                    {item.dealershipName}
                  </Link>
                </td>
                <td>{`${item.firstName} ${item.lastName}`}</td>
                <td>{item.email}</td>
                <td>{item.phone}</td>
                <td>{item.state}</td>
                <td>{item.postcode}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      </AppCard>
    </>
  );
}
