import InputSelect from "./InputSelect";

export default function InputState() {
  const stateList = [
    { value: "ACT", label: "ACT" },
    { value: "NSW", label: "NSW" },
    { value: "NT", label: "NT" },
    { value: "QLD", label: "QLD" },
    { value: "SA", label: "SA" },
    { value: "VIC", label: "VIC" },
    { value: "WA", label: "WA" },
  ];

  return (
    <InputSelect
      name="state"
      label="State"
      options={stateList}
    />
  );
}
