import React from "react";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

interface Props {
  icon: IconProp;
  size?: "xs" | "lg" | "sm" | "1x" | "2x" | "3x" | "4x" | "5x" | "6x" | "7x" | "8x" | "9x" | "10x" | undefined;
  title?: string | undefined;
  className?: string | undefined;
  color?: string | undefined;
  spin?: boolean | undefined;
  onClick?: React.MouseEventHandler<SVGSVGElement> | undefined;
}

const defaultProps = {
  size: undefined,
  title: undefined,
  className: undefined,
  color: undefined,
  spin: undefined,
  onClick: undefined,
};

export default function Icon({icon, size, title, className, color, spin,onClick}: Props) {
  const iconStyle = (onClick ? { cursor: "pointer" } : undefined);

  return (
    <FontAwesomeIcon
      icon={icon}
      size={size}
      title={title}
      className={className}
      color={color}
      onClick={onClick}
      spin={spin}
      style={iconStyle}
    />
  );
}

Icon.defaultProps = defaultProps;