import { Form } from "react-bootstrap";
import { useField } from "formik";
import Select, {
  ActionMeta, GroupTypeBase, OptionTypeBase, Styles,
} from "react-select";
import CreatableSelect from "react-select/creatable";
import { ThemeConfig } from "react-select/src/theme";

interface Props {
  label?: string;
  name?: string;
  placeholder?: string;
  message?: string;
  isClearable?: boolean;
  canAddItems?: boolean;
  options: (OptionTypeBase | GroupTypeBase<OptionTypeBase>)[];
  instanceId?: string | number;
  onChange?: ((value: any, action: ActionMeta<any>) => void)
}

const defaultProps = {
  label: "",
  name: "",
  placeholder: "",
  message: "",
  isClearable: false,
  canAddItems: false,
  instanceId: undefined,
  onChange: undefined,
};

export default function InputSelect({
  label, name, options, placeholder, message, isClearable, canAddItems, instanceId, onChange,
}: Props) {
  const [field, meta, helpers] = useField(name || label || "unknownfield");

  const { error } = meta;
  const { setValue } = helpers;

  function getOptionValue() {
    // Select value needs to be the object in the options list not the field value
    let foundOption = options.find((item) => item.value === field.value);

    if (foundOption) { return foundOption; }

    if (canAddItems && field.value) {
      // Add the custom value that the user entered so it shows in the control
      foundOption = { value: field.value, label: field.value };
      options.push(foundOption);
    } else {
      // Can't add items or value is empty string/null
      return null;
    }

    return foundOption;
  }

  function handleOnChange(option: InputSelectItem, actionMeta: ActionMeta<any>) {
    setValue(option?.value);
    if (onChange) { onChange(option, actionMeta); }
  }

  return (
    <>
      <Form.Group controlId={name || label}>
        <Form.Label>{label || name}</Form.Label>
        {canAddItems ? (
          <CreatableSelect
            name={field.name}
            options={options}
            value={getOptionValue()}
            isClearable={isClearable}
            placeholder={placeholder || label}
            onChange={handleOnChange}
            onBlur={field.onBlur}
            instanceId={instanceId}
            theme={selectTheme}
            styles={selectStyle}
          />
        )
          : (
            <Select
              name={field.name}
              options={options}
              value={getOptionValue()}
              isClearable={isClearable}
              placeholder={placeholder || label}
              onChange={handleOnChange}
              onBlur={field.onBlur}
              instanceId={instanceId}
              theme={selectTheme}
              styles={selectStyle}
            />
          )}
        {(message && !error) && (
          <Form.Text>{message}</Form.Text>
        )}
        {error && (
          <Form.Text className="text-error">{error}</Form.Text>
        )}

      </Form.Group>
    </>
  );
}

InputSelect.defaultProps = defaultProps;

export interface InputSelectItem {
  value: string | null;
  label: string;
}

const selectTheme: ThemeConfig = (theme) => ({
  ...theme,
  // border: "1px",
  colors: {
    ...theme.colors,
    primary: "#99caff",
  },
});

const selectStyle: Partial<Styles<any, false, GroupTypeBase<any>>> = {
  // option: (provided, state) => ({
  //   ...provided,
  //   borderBottom: "1px dotted pink",
  //   color: state.isSelected ? "red" : "blue",
  //   padding: 20,
  // }),
  control: (provided) => ({
    ...provided,
    fontSize: ".875rem",
    // fontWeight: "500", // bold
  }),
  // singleValue: (provided, state) => {
  // const opacity = state.isDisabled ? 0.5 : 1;
  // const transition = "opacity 300ms";
  //   return ({ ...provided });
  // }
};
