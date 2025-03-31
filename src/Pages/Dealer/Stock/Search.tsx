import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Button, Col, Row, Table,
} from "react-bootstrap";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import InputText from "Components/Controls/InputText";
import stockApi from "Services/StockApi";
import AppCard from "Components/Controls/AppCard";
import AppPagination from "Components/Navigation/AppPagination";
import { useQuery } from "react-query";
import { getStockDescription } from "Models/Stock/IStock";
import DealerStockSearchCriteriaDto from "Models/Stock/DealerStockSearchCriteriaDto";

export default function StockSearch() {
  const [criteria, setCriteria] = useState<DealerStockSearchCriteriaDto>(new DealerStockSearchCriteriaDto());
  const query = useQuery(["DealerStockSearch", criteria], () => stockApi.dealerSearch(criteria));

  function handlePageChange(newPage: number) {
    criteria.options.pageNumber = newPage;
    setCriteria({ ...criteria });
  }

  function handleFormSubmit(values: DealerStockSearchCriteriaDto, { setSubmitting }: FormikHelpers<DealerStockSearchCriteriaDto>) {
    const newCriteria = values;
    newCriteria.options.pageNumber = 1;
    setCriteria(newCriteria);
    setSubmitting(false);
  }

  return (
    <>
      <AppCard title="Stock">
        <Formik
          initialValues={criteria}
          onSubmit={handleFormSubmit}
          onReset={handleFormSubmit}
        >
          {({ isSubmitting, handleReset }) => (
            <FormikForm autoComplete="off">
              <Row>
                <Col md>
                  <InputText
                    name="searchText"
                    label="Search"
                    placeholder="Search Stock No., Make, Model, Badge, Rego, Engine No."
                  />
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
            </FormikForm>
          )}
        </Formik>
      </AppCard>
      <AppCard>
        <Table responsive hover>
          <thead>
            <tr>
              <th className="text-center">Stock No.</th>
              <th>Rego</th>
              <th>Yard</th>
              <th>Photo</th>
              <th>Description</th>
              <th>New/Used</th>
              <th className="text-right">Images</th>
              <th className="text-right">Advertised Price</th>
            </tr>
          </thead>
          <tbody>
            {query?.data && query.data?.stockList?.map((stock) => (
              <tr key={stock.stockId}>
                <td className="text-center"><Link to={`/dealer/stock/detail/${stock.stockId}`}>{stock.stockNumber}</Link></td>
                <td>{stock.regoNum}</td>
                <td>{stock.yardCode}</td>
                <td>
                  <img
                    src={`https://nanimagestorage.blob.core.windows.net/images/${stock.imageFilenames[0]}`}
                    width={50}
                    height={50}
                    alt="Stock "
                    style={{ borderRadius: "5px" }}
                  />

                </td>
                <td>
                  <Link to={`/dealer/stock/detail/${stock.stockId}`}>
                    {getStockDescription(stock)}
                  </Link>
                </td>
                <td>{stock.isUsed ? "Used" : "New"}</td>
                <td className="text-right">{stock.imageFilenames.length}</td>
                <td className="text-right">${stock.price}</td>
              </tr>
            ))}
          </tbody>
        </Table>
        <AppPagination
          pagesInfo={query?.data?.options}
          onPageChange={handlePageChange}
        />
      </AppCard>
    </>
  );
}
