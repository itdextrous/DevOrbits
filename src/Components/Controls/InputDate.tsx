/* eslint-disable react/jsx-props-no-spreading */
import { Form } from "react-bootstrap";
import { useField } from "formik";
// import { ChangeEventHandler } from "react";

interface Props {
  label: string;
  name?: string;
}

const defaultProps = {
  name: "",
};

function InputDate({ label, name, ...props }: Props) {
  const [field, meta] = useField({ name: name || label, ...props });

  if (field.value) {
    if (typeof field.value === "string") {
      // api sends dates with time and timezone but native date control only wants date part
      // approvedDate: "2021-04-28T00:00:00+10:00" -> 2021-04-28
      field.value = field.value.substring(0, 10);
    }
  }

  return (
    <>
      <Form.Group controlId={name || label}>
        <Form.Label>{label || name}</Form.Label>
        <Form.Control
          type="date"
          className="input"
          autoComplete="off"
          {...props}
          {...field}
        />
        {(meta && meta.touched && meta.error) && (
          <Form.Text className="text-error">{meta.error}</Form.Text>
        )}
      </Form.Group>
    </>
  );
}

InputDate.defaultProps = defaultProps;

export default InputDate;
