import SearchOptions from "Common/SearchOptions";
import { IStockSearchParams } from "Components/Stock/StockSearchParams";

export default class StockSearchCriteriaDto implements IStockSearchParams {
  stockType: number | null = null;

  make: string | null = null;

  model: string | null = null;

  location: string | null = null;

  priceMin: number | null = null;

  priceMax: number | null = null;

  bodyType: string | null = null;

  keywords: string | null = null;

  options: SearchOptions = new SearchOptions(10, 1);
}
