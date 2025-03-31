import { ReactNode } from "react";

interface Props {
  title?: string;
  children: ReactNode;
}

const defaultProps = {
  title: "",
};

export default function AppCard({ title, children }: Props) {
  return (
    <div className="card">
      <div className="card-body">
        {title && (
          <h5 className="card-title">{title}</h5>
        )}
        {children}
      </div>
    </div>
  );
}

AppCard.defaultProps = defaultProps;
