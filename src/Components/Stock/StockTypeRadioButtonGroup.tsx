import { ButtonGroup, ToggleButton } from "react-bootstrap";

interface Props {
  value: number | null;
  onChange: (newValue: number | null) => void;
}

export default function StockTypeRadioButtonGroup({ value, onChange }: Props) {
  const radios = [
    { name: "All", value: null },
    { name: "New", value: 1 },
    { name: "Used", value: 2 },
  ];

  return (
    <>
      <ButtonGroup toggle>
        {radios?.map((radio) => (
          <ToggleButton
            key={radio.value}
            type="radio"
            variant={value === radio.value ? "primary" : "outline-secondary"}
            name="radio"
            value={radio.value || ""}
            checked={value === radio.value}
            onChange={(e) => {
              const newValue = e.currentTarget.value ? Number(e.currentTarget.value) : null;
              onChange(newValue);
            }}
          >
            {radio.name}
          </ToggleButton>
        ))}
      </ButtonGroup>
    </>
  );
}
