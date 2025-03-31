import InputSelect from "./InputSelect";

export default function InputVehicleCount() {
  return (
    <InputSelect
      name="vehicleCount"
      label="Vehicles"
      options={[
        { value: "10-50", label: "10-50" },
        { value: "51-100", label: "51-100" },
        { value: "100+", label: "100+" },
      ]}
    />
  );
}
