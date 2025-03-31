import { useEffect, useState } from "react";
import {Col, Row} from "react-bootstrap";
import { useParams } from "react-router-dom";

type routeParamValue = {
  planId: string;
};

const plansListData = [{
  id: "dealer-lite",
  SubscriptionName: "Lite",
  Site: 20,
  AutomatedListingManagement: 20,
  QualifiedLeads: 23,
  EmailSupport: 23,
  SubscriptionPrice: 189,
  PhoneSupport: 10,
  MultipleSiteManagement: 20,
  PremiumServices: 20,
  AccountManager: 20,
  CustomPricing: 143,
},
{
  id: "dealer-silver",
  SubscriptionName: "Silver",
  Site: 20,
  AutomatedListingManagement: 20,
  QualifiedLeads: 23,
  EmailSupport: 23,
  SubscriptionPrice: 389,
  PhoneSupport: 10,
  MultipleSiteManagement: 20,
  PremiumServices: 20,
  AccountManager: 20,
  CustomPricing: 232,
},
{
  id: "dealer-gold",
  SubscriptionName: "Gold",
  Site: 20,
  AutomatedListingManagement: 20,
  QualifiedLeads: 23,
  EmailSupport: 23,
  SubscriptionPrice: 599,
  PhoneSupport: 10,
  MultipleSiteManagement: 20,
  PremiumServices: 20,
  AccountManager: 20,
  CustomPricing: 234,
}, {
  id: "dealer-platinum",
  SubscriptionName: "Platinum",
  Site: 20,
  AutomatedListingManagement: 20,
  QualifiedLeads: 23,
  EmailSupport: 23,
  SubscriptionPrice: 0,
  PhoneSupport: 10,
  MultipleSiteManagement: 20,
  PremiumServices: 20,
  AccountManager: 20,
  CustomPricing: 127,
}];

export default function Plans({ parentCallback }: any) {
  const { planId } = useParams<routeParamValue>();
  const [selectedPlan, setSelectedPlan] = useState({
    id: "",
    SubscriptionName: "",
    Site: 0,
    AutomatedListingManagement: 0,
    QualifiedLeads: 0,
    EmailSupport: 0,
    SubscriptionPrice: 0,
    PhoneSupport: 0,
    MultipleSiteManagement: 0,
    PremiumServices: 0,
    AccountManager: 0,
    CustomPricing: 0,
  });
  useEffect(() => {
    const dealerId = planId.split(":");
    const getSelectedPlan = plansListData.find((planObj) => planObj.id === dealerId[1]?.toLowerCase());
    if (getSelectedPlan) {
      setSelectedPlan(getSelectedPlan);
      parentCallback(getSelectedPlan);
    }
  }, [parentCallback, planId]);
  return (
    <>
      <Row className="Plan_table">
        <Col md>
          <div className="plan_name">
            <h3>{selectedPlan.SubscriptionName}</h3>
          </div>
        </Col>
        <Col md>
          <div className="Plan_list_Item">
            <ul>
              <li><span className="fa-fa-check">{selectedPlan.Site}  site, {selectedPlan.AutomatedListingManagement} listings</span></li>
              <li><span>Automated Listing Management</span></li>
            </ul>
          </div>
        </Col>
        <Col md>
          <div className="Plan_list_Item">
            <ul>
              <li><span> Email Supports </span></li>
              <li><span> Qualified Leads </span></li>
            </ul>
          </div>
        </Col>
        <Col md>
          <div className="Plan_list_Item">
            <ul>
              <li><span>$ {selectedPlan.SubscriptionPrice ? selectedPlan.SubscriptionPrice : "Custom Price"} </span></li>
              <li><span>Per Month</span></li>
            </ul>
          </div>
        </Col>
      </Row>
    </>
  );
}