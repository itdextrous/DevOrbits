import { IStockDto } from "./IStock";

export default class StockDto implements IStockDto {
  stockId: string = "";

  dealerId: string = "";

  importSource: string = "";

  status: number = 0;

  stockNumber: number = 0;

  stockTypeId: string = "";

  yardCode: string | null = null;

  location: string = "";

  make: string = "";

  model: string = "";

  yearGroup: number | null = null;

  series: string | null = null;

  badge: string | null = null;

  redbookCode: string | null = null;

  price: number = 0;

  priceType: number = 0;

  priceDriveAway: number | null = null;

  priceExGovtCharges: number | null = null;

  isUsed: boolean = false;

  isDemo: boolean = false;

  body: string | null = null;

  odometer: number | null = null;

  isMiles: boolean = false;

  colour: string | null = null;

  vin: string | null = null;

  regoNum: string | null = null;

  shortDescription: string | null = null;

  interiorColour: string | null = null;

  regoState: string | null = null;

  regoExpiry: string | null = null;

  buildDate: string | null = null;

  complianceDate: string | null = null;

  standardFeature: string | null = null;

  optionFeature: string | null = null;

  advDescription: string | null = null;

  nvic: string | null = null;

  grossCombinationMass: number | null = null;

  grossVehicleMass: number | null = null;

  tare: number | null = null;

  sleepingCapacity: number | null = null;

  toilet: string | null = null;

  shower: string | null = null;

  airConditioning: string | null = null;

  fridge: string | null = null;

  stereo: string | null = null;

  engineNumber: string | null = null;

  gearCount: string | null = null;

  enginePower: string | null = null;

  powerkW: string | null = null;

  powerHp: string | null = null;

  engineMake: string | null = null;

  gps: string | null = null;

  serialNumber: string | null = null;

  wheelSize: string | null = null;

  towballWeight: string | null = null;

  warranty: string | null = null;

  wheels: string | null = null;

  axleConfiguration: string | null = null;

  cylinders: string | null = null;

  engineSize: string | null = null;

  fuelType: string | null = null;

  transmission: string | null = null;

  specialPrice: string | null = null;

  drive: string | null = null;

  seats: number | null = null;

  doors: number | null = null;

  imageFilenames: string[] = [];

  isSaved: boolean = false;
}
