import {
  Button, Col, Row, Form, Container,
} from "react-bootstrap";
import {
  FormEvent, Fragment, ReactNode, useEffect, useState,
} from "react";
import { useQuery } from "react-query";
import stockApi from "Services/StockApi";
import StockSearchCriteriaDto from "Models/Stock/StockSearchCriteriaDto";
import AppCard from "Components/Controls/AppCard";
import { InputSelectItem } from "Components/Controls/InputSelect";
import { useHistory, useLocation } from "react-router-dom";
import Qs from "qs";
import StockSearchResultsDto from "Models/Stock/StockSearchResultsDto";
import { IStockDto } from "Models/Stock/IStock";
import AppPagination from "Components/Navigation/AppPagination";
import { faSearch, faSpinner } from "@fortawesome/free-solid-svg-icons";
import Icon from "Components/Icon";
import SavedSearchDto from "Models/SavedSearch/SavedSearchDto";
import generateSavedSearchDescription from "Pages/Customer/SavedSearch/SavedSearch";
import StockTypeRadioButtonGroup from "./StockTypeRadioButtonGroup";
import SearchSelect from "./SearchSelect";
import { getStockSearchParameters } from "./StockSearchParams";

const cachedQueryOptions = {
  staleTime: 5 * 60000, refetchOnMount: false, refetchOnWindowFocus: false,
};

interface Props {
  children: (stock: IStockDto, onSaveStateChanged: (stock: IStockDto) => Promise<void>) => ReactNode;
  onSaveSearch: (savedSearch: SavedSearchDto) => void;
}

export default function StockSearch({ children, onSaveSearch }: Props) {
  const history = useHistory();
  const urlLocation = useLocation();

  // criteria is specified from querystring
  const [criteria, setCriteria] = useState(new StockSearchCriteriaDto());
  const [allowSaveSearch, setAllowSaveSearch] = useState(false);
  const [showBodyTypePanel, setShowBodyTypePanel] = useState(false);

  // control values
  const [stockType, setStockType] = useState<number | null>(null);
  const [make, setMake] = useState<string | null>(null);
  const [model, setModel] = useState<string | null>(null);
  const [location, setLocation] = useState<string | null>(null);
  const [priceMin, setPriceMin] = useState<number | null>(null);
  const [priceMax, setPriceMax] = useState<number | null>(null);
  const [bodyType, setBodyType] = useState<string | null>(null);
  const [keywords, setKeywords] = useState("");

  const { data: distinctMakes } = useQuery("StockMakes", () => getDistinctMakesItems(), cachedQueryOptions);
  const { data: distinctModels } = useQuery(["StockModels", make], () => getDistinctModelsItems(make), { enabled: Boolean(make), ...cachedQueryOptions });
  const { data: distinctBodyTypes } = useQuery(["StockBodyTypes", make, model], () => getDistinctBodyTypesItems(make, model), cachedQueryOptions);
  const { data: allBodyTypes } = useQuery("StockBodyTypes", () => stockApi.bodyTypes("", ""), cachedQueryOptions);
  const {
    isLoading, isFetching, refetch, data: stockData,
  } = useQuery(["StockSearch", criteria], () => stockApi.search(criteria));
  const { options: pagesInfo } = stockData || new StockSearchResultsDto();

  const isSearching = (isLoading || isFetching);

  useEffect(() => {
    const params = new URLSearchParams(urlLocation.search);

    // get search criteria from query string
    const q = new StockSearchCriteriaDto();
    getStockSearchParameters(q);
    q.options.pageNumber = params.has("page") ? Number(params.get("page")) : 1;

    // update current state from query string
    setStockType(q.stockType);
    setMake(q.make);
    setModel(q.model);
    setLocation(q.location);
    setPriceMin(q.priceMin);
    setPriceMax(q.priceMax);
    setBodyType(q.bodyType);
    setKeywords(q.keywords || "");

    const hasCriteria = Boolean(q.stockType || q.make || q.model || q.location || q.priceMin || q.priceMax || q.bodyType || q.keywords);
    setShowBodyTypePanel(!(q.options.pageNumber > 1 || hasCriteria));
    setAllowSaveSearch(hasCriteria);

    setCriteria(q);
  }, [urlLocation]);

  useEffect(() => {
    // Make sure that the current bodytype is one of the items in the filtered list of bodyTypes
    const listItem = distinctBodyTypes?.find((option) => option.value === bodyType);
    if (!listItem) {
      setBodyType(null);
    }
  }, [bodyType, distinctBodyTypes]);

  function handleFormSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    doFullSearch(1);
  }

  async function handleSaveStateChanged() {
    await refetch();
  }

  function handlePageChange(pageNumber: number | null | undefined) {
    if (!pageNumber) { return; }
    doFullSearch(pageNumber);
  }

  function handleBodyTypeSearch(bodySearch: string | null) {
    setBodyType(bodySearch);
    doSearch(1, { bodyType: bodySearch });
  }

  function doFullSearch(pageNumber: number) {
    const qsCriteria = {
      stockType, make, model, bodyType, location, priceMin, priceMax, keywords: keywords || null,
    };
    doSearch(pageNumber, qsCriteria);
  }

  function doSearch(pageNumber: number, qsCriteria: Object = {}) {
    const queryString = Qs.stringify({ ...qsCriteria, page: pageNumber }, { addQueryPrefix: true, allowDots: true, skipNulls: true });
    history.push(urlLocation.pathname + queryString);
  }

  async function handleSaveSearch() {
    const search = new SavedSearchDto();
    getStockSearchParameters(search);
    search.description = generateSavedSearchDescription(search);

    onSaveSearch(search);
  }

  return (
    <div>
      <Form autoComplete="off" onSubmit={handleFormSubmit}>
        <AppCard>
          <Row>
            <Col md><h2>Find your next car</h2></Col>
            <Col md={4}>
              <StockTypeRadioButtonGroup
                value={stockType}
                onChange={(newValue) => { setStockType(newValue); }}
              />
            </Col>
          </Row>

          <Row>
            <Col md>
              <SearchSelect
                name="Make"
                placeholder="All Makes"
                value={make}
                options={distinctMakes || []}
                onChange={(option: InputSelectItem) => { setMake(option?.value); setModel(null); }}
              />
            </Col>
            <Col md>
              <SearchSelect
                name="model"
                placeholder="All Models"
                value={model}
                options={distinctModels || []}
                onChange={(option: InputSelectItem) => { setModel(option?.value); }}
              />

            </Col>
            <Col md>
              <SearchSelect
                name="location"
                placeholder="Location"
                value={location}
                options={locationItems}
                onChange={(option: InputSelectItem) => { setLocation(option?.value); }}
              />
            </Col>
          </Row>

          <Row>
            <Col md={2} sm={6} xs={6}>
              <SearchSelect
                name="priceMin"
                label="Price Min"
                placeholder="Any"
                value={priceMin}
                options={priceItems}
                onChange={(option: InputSelectItem) => { setPriceMin(option?.value ? Number(option?.value) : null); }}
              />
            </Col>
            <Col md={2} sm={6} xs={6}>
              <SearchSelect
                name="priceMax"
                label="Price Max"
                placeholder="Any"
                value={priceMax}
                options={priceItems}
                onChange={(option: InputSelectItem) => { setPriceMax(option?.value ? Number(option?.value) : null); }}
              />
            </Col>
            <Col md={4}>
              <SearchSelect
                name="bodyType"
                label="All Body Types"
                value={bodyType}
                options={distinctBodyTypes || []}
                onChange={(option: InputSelectItem) => { setBodyType(option?.value); }}
              />
            </Col>
            <Col md={4}>
              <Form.Group controlId="keywords">
                <Form.Label>Keywords</Form.Label>
                <Form.Control
                  type="text"
                  name="keywords"
                  defaultValue={keywords || ""}
                  onChange={(e) => { setKeywords(e.target.value || ""); }}
                />
              </Form.Group>
            </Col>
          </Row>
          <Row>
            <Col md={12}>
              <Button type="submit" variant="primary" disabled={isSearching}>
                {isSearching ? (<Icon icon={faSpinner} spin />)
                  : (<Icon icon={faSearch} />)}
                {" "}Search
              </Button>
              <Button
                type="button"
                variant="secondary"
                disabled={isSearching || !allowSaveSearch}
                onClick={handleSaveSearch}
              >Save Search
              </Button>
            </Col>
          </Row>
        </AppCard>
      </Form>
      {showBodyTypePanel && (
        <AppCard title="Search by body type">
          <Container>
            <Row className="custom_search_col">
              {
                allBodyTypes?.stockList !== undefined ?
                  allBodyTypes && allBodyTypes.stockList
                    .filter((vehicle) => vehicle?.body !== null)
                    .map((vehicle) => (
                      <Col key={vehicle.body}>
                        <Button
                          type="button"
                          variant="outline-primary"
                          block
                          style={{ height: "100%" }}
                          onClick={() => { handleBodyTypeSearch(vehicle.body); }}
                        >{vehicle.body}
                        </Button>
                      </Col>
                    )) : null
              }
            </Row>
          </Container>
        </AppCard>
      )}

      {stockData && stockData?.options?.totalItems === 0 && (
        <AppCard>
          <div className="text-center">
            <b>No vehicles matched your search,<br />
              try some different search options.
            </b>
          </div>
        </AppCard>
      )}
    {
      <div className="vehicle_list_view">
      {stockData && stockData?.stockList?.map((stock) => (
            <Fragment key={stock.stockId}>
              {children(stock, handleSaveStateChanged)}
            </Fragment>
          ))}
      </div>
    }
      <AppPagination pagesInfo={pagesInfo} onPageChange={handlePageChange} />
    </div>
  );
}

function getDistinctMakesItems() {
  return new Promise((resolve: (list: InputSelectItem[]) => any) => {
    const makes: InputSelectItem[] = [{ value: null, label: "All Makes" }];

    stockApi.makes().then((data) => {
      if (data?.stockList !== undefined) {
        data.stockList.forEach((vehicle) => {
          makes.push({ value: vehicle.make, label: vehicle.make });
        });
      }
      return resolve(makes);
    });
  });
}

function getDistinctModelsItems(make: string | null) {
  return new Promise((resolve: (list: InputSelectItem[]) => any) => {
    const models: InputSelectItem[] = [{ value: null, label: "All Models" }];

    stockApi.models(make || "").then((data) => {
      data.stockList.forEach((vehicle) => {
        models.push({ value: vehicle.model, label: vehicle.model });
      });
      return resolve(models);
    });
  });
}

function getDistinctBodyTypesItems(make: string | null, model: string | null) {
  return new Promise((resolve: (list: InputSelectItem[]) => any) => {
    const bodyTypes: InputSelectItem[] = [{ value: null, label: "All Body Types" }];

    stockApi.bodyTypes(make || "", model || "").then((data) => {
      if (data?.stockList !== undefined) {
        data.stockList.forEach((vehicle) => {
          bodyTypes.push({ value: String(vehicle.body), label: String(vehicle.body) });
        });
      }
      return resolve(bodyTypes);
    });
  });
}

const locationItems: InputSelectItem[] = [
  { value: null, label: "Any Location" },
  { value: "ACT", label: "ACT" },
  { value: "NSW", label: "New South Wales" },
  { value: "NT", label: "Northern Territory" },
  { value: "QLD", label: "Queensland" },
  { value: "SA", label: "South Australia" },
  { value: "VIC", label: "Victoria" },
  { value: "WA", label: "Western Australia" },
];

const priceItems: InputSelectItem[] = [
  { value: null, label: "Any" },
  { value: "3000", label: "$3,000" },
  { value: "5000", label: "$5,000" },
  { value: "7500", label: "$7,500" },
  { value: "10000", label: "$10,000" },
  { value: "15000", label: "$15,000" },
  { value: "20000", label: "$20,000" },
  { value: "25000", label: "$25,000" },
  { value: "30000", label: "$30,000" },
  { value: "35000", label: "$35,000" },
  { value: "40000", label: "$40,000" },
  { value: "45000", label: "$45,000" },
  { value: "50000", label: "$50,000" },
  { value: "60000", label: "$60,000" },
  { value: "70000", label: "$70,000" },
  { value: "80000", label: "$80,000" },
  { value: "90000", label: "$90,000" },
  { value: "100000", label: "$100,000" },
  { value: "150000", label: "$150,000" },
];
