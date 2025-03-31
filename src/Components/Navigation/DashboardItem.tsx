import { Link } from "react-router-dom";

interface Props {
  title: string;
  description: string;
  linkLabel: string;
  linkTo: string;
}

export default function DashboardItem({ title, description, linkLabel, linkTo}: Props) {
  return (
    <div className="card">
      <div className="card-body">
        <h5 className="card-title">{title}</h5>
        <p className="card-text">{description}</p>
        <Link
          to={linkTo}
          className="btn btn-primary"
        >{linkLabel}
        </Link>
      </div>
    </div>
  );
}
