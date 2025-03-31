import InputSelect from "./InputSelect";

export default function InputStockType() {
  return (
    <InputSelect
      name="stockType"
      label="Stock type"
      options={[
        { value: 1, label: "New" },
        { value: 2, label: "Used" },
        { value: 3, label: "New and Used" },
      ]}
    />
  );
}
