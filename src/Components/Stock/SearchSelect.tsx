/* eslint-disable react/jsx-props-no-spreading */
/* eslint-disable react/prop-types */
import { InputSelectItem } from "Components/Controls/InputSelect";
import { Form } from "react-bootstrap";
import Select from "react-select";

export default function SearchSelect({
  name, label, placeholder, value, options, ...props
}: any) {
  function getSelectValue() {
    if (!options) { return null; }
    const foundOption = options.find((item: InputSelectItem) => item.value === String(value));
    return foundOption || null;
  }

  return (
    <Form.Group controlId={name}>
      <Form.Label>{label || placeholder}</Form.Label>
      <Select
        {...props}
        value={getSelectValue()}
        name={name}
        placeholder={placeholder}
        options={options}
      />
    </Form.Group>
  );
}
