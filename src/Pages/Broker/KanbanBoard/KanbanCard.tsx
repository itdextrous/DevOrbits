/* eslint-disable jsx-a11y/control-has-associated-label */
import CustomerApplicationStatus from "Models/Customer/CustomerApplicationStatus";
import CustomerSearchDto from "Models/Customer/CustomerSearchDto";
import { Link } from "react-router-dom";
import  edit  from "../../../Images/edit.svg";
import  approved  from "../../../Images/approved.svg";
import  info  from "../../../Images/info.svg";
import exclamationCircleSolid from  "../../../Images/exclamationCircleSolid.svg";
const applicationStatusParts: { status: CustomerApplicationStatus; text: string; cssClass: string; icon:any ; iconClass: string; }[] = [
  {
    status: CustomerApplicationStatus.Approved, text: "Approved", cssClass: "bg-success approved", icon: approved, iconClass: "text-success",
  },
  {
    status: CustomerApplicationStatus.Declined, text: "Declined", cssClass: "bg-danger declined", icon: info, iconClass: "text-danger",
  },
  {
    status: CustomerApplicationStatus.DecisionPending, text: "Pending", cssClass: "bg-warning pending", icon: exclamationCircleSolid, iconClass: "text-warning",
  },
  {
    status: CustomerApplicationStatus.ApplicationToBeSubmitted, text: "To be submitted", cssClass: "bg-warning", icon:edit, iconClass: "text-warning",
  },
];

interface Props {
  customer: CustomerSearchDto;
  compactView?: boolean;
  onClick: (customer: CustomerSearchDto) => void;
}

const defaultProps = {
  compactView: undefined,
};

export default function KanbanCard({ customer, compactView, onClick }: Props) {
  const applicationStatusInfo = applicationStatusParts.find((part) => part.status === customer.applicationStatus);

  if (!applicationStatusInfo) { return null; }

  return (
    <div className={`card ${compactView ? "" : `card-kanban ${applicationStatusInfo.cssClass}`}`} style={{ width: "230px" }}>

      <div className="progress">
        <div
          className={`progress-bar ${applicationStatusInfo.cssClass}`}
          role="progressbar"
          style={{ width: "100%" }}
          aria-valuenow={100}
          aria-valuemin={0}
          aria-valuemax={100}
        />
      </div>

      <div className="card-body">
        <div className="card-title">
          <Link
            to={`/broker/customer/edit/${customer.customerId}`}
            onClick={(e) => {
              e.preventDefault();
              onClick(customer);
            }}
          >
            <h6>{`${customer.firstName} ${customer.lastName}`}</h6>
          </Link>
        </div>
        {!compactView && (
          <p>{customer.vehicleRequirements}</p>
        )}

        <div className="card-meta d-flex justify-content-between">
          <div className="d-flex align-items-center">
            <img src={applicationStatusInfo.icon} alt='status' />
          </div>
          <span className="text-small">{applicationStatusInfo.text}</span>
        </div>

      </div>
    </div>

  );
}
KanbanCard.defaultProps = defaultProps;
