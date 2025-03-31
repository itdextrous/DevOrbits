import { formatCurrencyWithoutCents } from "Common/Utility/Formatting";
import { IStockSearchParams } from "Components/Stock/StockSearchParams";

export default function generateSavedSearchDescription(search: IStockSearchParams) {
  let desc = "";

  if (search.stockType) {
    desc += `${search.stockType === 1 ? "New" : "Used"} `;
  }

  desc += `${search.make ? `${search.make} ` : ""}`
    + `${search.model ? `${search.model} ` : ""}`
    + `${search.bodyType ? `${search.bodyType} ` : ""}`
    + `${search.location ? `in ${search.location} ` : ""}`
    + `${search.keywords ? `keywords '${search.keywords}' ` : ""}`;

  if (search.priceMin && !search.priceMax) {
    desc += `more than ${formatCurrencyWithoutCents(search.priceMin)}`;
  }
  if (search.priceMin && search.priceMax) {
    desc += `between ${formatCurrencyWithoutCents(search.priceMin)} and ${formatCurrencyWithoutCents(search.priceMax)}`;
  }
  if (!search.priceMin && search.priceMax) {
    desc += `less than ${formatCurrencyWithoutCents(search.priceMax)}`;
  }

  return desc;
}
