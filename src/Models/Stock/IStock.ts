export interface IStockDto {
  stockId: string;
  dealerId: string;
  importSource: string;
  status: number;
  stockNumber: number;
  stockTypeId: string;
  yardCode: string | null;
  location: string ;
  make: string;
  model: string;
  yearGroup: number | null;
  series: string | null;
  badge: string | null;
  redbookCode: string | null;
  price: number;
  priceType: number;
  priceDriveAway: number | null;
  priceExGovtCharges: number | null;
  isUsed: boolean;
  isDemo: boolean;
  body: string | null;
  odometer: number | null;
  isMiles: boolean;
  colour: string | null;
  vin: string | null;
  regoNum: string | null;
  shortDescription: string | null;
  interiorColour: string | null;
  regoState: string | null;
  regoExpiry: string | null;
  buildDate: string | null;
  complianceDate: string | null;
  standardFeature: string | null;
  optionFeature: string | null;
  advDescription: string | null;
  nvic: string | null;
  grossCombinationMass: number | null;
  grossVehicleMass: number | null;
  tare: number | null;
  sleepingCapacity: number | null;
  toilet: string | null;
  shower: string | null;
  airConditioning: string | null;
  fridge: string | null;
  stereo: string | null;
  engineNumber: string | null;
  gearCount: string | null;
  enginePower: string | null;
  powerkW: string | null;
  powerHp: string | null;
  engineMake: string | null;
  gps: string | null;
  serialNumber: string | null;
  wheelSize: string | null;
  towballWeight: string | null;
  warranty: string | null;
  wheels: string | null;
  axleConfiguration: string | null;
  cylinders: string | null;
  engineSize: string | null;
  fuelType: string | null;
  transmission: string | null;
  specialPrice: string | null;
  drive: string | null;
  seats: number | null;
  doors: number | null;

  imageFilenames: string[];
  isSaved: boolean;
}

function strSpaced(value: any) {
  return value ? `${value} ` : "";
}

export function getStockDescription(stock: IStockDto) {
  return `${strSpaced(stock.yearGroup)}${strSpaced(stock.make)}${strSpaced(stock.model)}${strSpaced(stock.badge)}${strSpaced(stock.series)}`;
}
