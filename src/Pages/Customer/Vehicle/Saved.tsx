import { Col, Row } from "react-bootstrap";
import savedStockApi from "Services/SavedStockApi";
import StockSearchCriteriaDto from "Models/Stock/StockSearchCriteriaDto";
import { useQuery } from "react-query";
import CustomerStockCard from "./CustomerStockCard";
import AppPagination from "Components/Navigation/AppPagination";
import { useState } from "react";

export default function SavedStockList() {
  // const criteria = new StockSearchCriteriaDto();
  // const query = useQuery(["StockSaved", criteria], () => savedStockApi.list(criteria));
  const [criteria, setCriteria] = useState<StockSearchCriteriaDto>(new StockSearchCriteriaDto());
  const query = useQuery(["StockSaved", criteria], () => savedStockApi.list(criteria));

  function handlePageChange(newPage: number) {
    criteria.options.pageNumber = newPage;
    setCriteria({ ...criteria });
  }

  return (
    <>
      <h2>Saved Vehicles</h2>
      <Row>
        {query?.data && query.data?.stockList?.map((stock) => (
          <Col key={stock.stockId} md={6} sm={12} xs={12} >
            <CustomerStockCard
              stock={stock}
              imageWidth={600}
              imageHeight={450}
              viewButtonIsVisible
              onSaveStateChanged={async () => { await query.refetch(); }}
            />
          </Col>
        ))}

      </Row>
      <AppPagination pagesInfo={query?.data?.options} onPageChange={handlePageChange} />
    </>
  );
}
