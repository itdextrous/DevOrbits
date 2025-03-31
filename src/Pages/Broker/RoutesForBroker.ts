import React from "react";
import { MenuRoute } from "Routes";
import CustomerImage from "../../Images/customers.svg"
import VehiclesImage from "../../Images/Vehicles.svg"


const brokerRoutes: MenuRoute[] = [
  {
    title: "Invite Customer",
    path: "broker/customer/invite",
    menu: true,
    img: CustomerImage,
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Broker/Customer/Invite")),
  },
  {
    title: "Edit Customer",
    path: "broker/customer/edit/:customerId",
    menu: false,
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Broker/Customer/Edit")),
    img:"",
  },
  {
    title: "Vehicles",
    path: "broker/vehicle/search",
    menu: true,
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Broker/Vehicle/Search")),
    img:VehiclesImage,
  },
  {
    title: "Vehicle Detail",
    path: "broker/vehicle/detail/:stockId",
    menu: false,
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Broker/Vehicle/Detail")),
    img:"",
  },
  {
    title: "Messages",
    path: "broker/messages/conversations",
    menu: true,
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Broker/Messages/BrokerInbox")),
    img:"",
  },
  {
    title: "Customer Inquiry",
    path: "broker/messages/conversation/:conversationId",
    menu: false,
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Broker/Messages/BrokerConversation")),
    img:"",
  }
  // {
  //   title: "Mockups",
  //   path: "broker/mockups",
  //   menu: true,
  //   component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Mockup/Broker/Customer/Edit")),
  //   routes: [
  //     {
  //       title: "Add Customer",
  //       path: "broker/customer/edit",
  //       menu: true,
  //       component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Mockup/Broker/Customer/Edit")),
  //       img:"",
  //     },
  //   ],
  //   img:"",
  // }
];

export default brokerRoutes;
