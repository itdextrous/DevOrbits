import AuditableEntity from "Models/AuditableEntity";

export default class SavedSearchSearchDto extends AuditableEntity {
  savedSearchId: string = "";

  description: string = "";

  stockType: number | null = null;

  make: string | null = null;

  model: string | null = null;

  location: string | null = null;

  priceMin: number | null = null;

  priceMax: number | null = null;

  bodyType: string | null = null;

  keywords: string | null = null;
}
