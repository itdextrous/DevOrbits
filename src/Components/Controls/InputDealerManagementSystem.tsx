import InputSelect from "./InputSelect";

export default function InputDealerManagementSystem() {
  return (
    <InputSelect
      name="dealerManagementSystem"
      label="Dealer Management System"
      canAddItems
      placeholder="Your DMS"
      message="Select or type your dealer management system"
      options={[
        { value: "Dealer Solutions", label: "Dealer Solutions" },
        { value: "Easy Cars", label: "Easy Cars" },
        { value: "Pentana Solutions", label: "Pentana Solutions" },
        { value: "Ultimate Business Systems", label: "Ultimate Business Systems" },
      ]}
    />
  );
}
