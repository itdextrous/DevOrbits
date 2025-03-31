import CustomerApplicationStatus from "Models/Customer/CustomerApplicationStatus";
import InputSelect from "./InputSelect";

export default function InputApplicationStatus() {
  return (
    <InputSelect
      name="applicationStatus"
      label="Application Status"
      options={[
        { value: CustomerApplicationStatus.Approved, label: "Approved" },
        { value: CustomerApplicationStatus.DecisionPending, label: "Decision Pending" },
        { value: CustomerApplicationStatus.ApplicationToBeSubmitted, label: "Application to be submitted" },
        { value: CustomerApplicationStatus.Declined, label: "Declined / Re-work needed" },
      ]}
    />
  );
}
