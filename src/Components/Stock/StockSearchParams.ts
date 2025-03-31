export interface IStockSearchParams {
  stockType: number | null;
  make: string | null;
  model: string | null;
  location: string | null;
  priceMin: number | null;
  priceMax: number | null;
  bodyType: string | null;
  keywords: string | null;
}

export function getStockSearchParameters(stockSearch: IStockSearchParams) {
  const params = new URLSearchParams(window.location.search);

  const search = stockSearch;
  search.stockType = params.has("stockType") ? Number(params.get("stockType")) : null;
  search.make = params.get("make");
  search.model = params.get("model");
  search.location = params.get("location");
  search.priceMin = params.has("priceMin") ? Number(params.get("priceMin")) : null;
  search.priceMax = params.has("priceMax") ? Number(params.get("priceMax")) : null;
  search.bodyType = params.get("bodyType");
  search.keywords = params.get("keywords") || "";
}
