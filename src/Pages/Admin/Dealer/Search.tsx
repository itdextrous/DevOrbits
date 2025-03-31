import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Button, Col, Row, Table,
} from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import InputText from "Components/Controls/InputText";
import dealerApi from "Services/DealerApi";
import DealerSearchCriteria from "Models/Dealer/DealerSearchCriteriaDto";
import AppCard from "Components/Controls/AppCard";
import InputState from "Components/Controls/InputState";
import { useQuery } from "react-query";

export default function DealerSearch() {
  const [criteria, setCriteria] = useState(new DealerSearchCriteria());

  const query = useQuery(["DealerSearch", criteria], () => dealerApi.search(criteria), { keepPreviousData: true });

  function handleFormSubmit(values: DealerSearchCriteria, { setSubmitting }: FormikHelpers<DealerSearchCriteria>) {
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

            <AppCard title="Dealer search">
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
                    className="mx-1"
                    disabled={isSubmitting}
                    onClick={handleReset}
                  >Clear
                  </Button>
                  <Link to="/admin/dealer/edit" className="btn btn-secondary">Add New</Link>
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
              <th>Dealership</th>
              <th>Contact</th>
              <th>E-mail</th>
              <th>Phone</th>
              <th>State</th>
            </tr>
          </thead>
          <tbody>
            {query.data && query.data?.dealerList?.map((item) => (
              <tr key={item.dealerId}>
                <td>
                  <Link to={`/admin/dealer/edit/${item.dealerId}`}>{item.dealershipName}</Link>
                </td>
                <td>{`${item.firstName} ${item.lastName}`}</td>
                <td>{item.email}</td>
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
