import React from "react";
import { MenuRoute } from "Routes";

const mockupRoutes: MenuRoute[] = [
  {
    title: "Customer Secure Vehicle",
    path: "customer/secure",
    menu: true,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Mockup/Customer/SecureVehicle")),
  },
  {
    title: "Messages",
    path: "messages/inbox",
    menu: true,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Mockup/MessageInbox")),
  },
  {
    title: "Messages",
    path: "messages/conversation/dealer",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Mockup/MessageThreadDealer")),
  },
  {
    title: "Messages",
    path: "messages/conversation/broker",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Mockup/MessageThreadBroker")),
  },
];

export default mockupRoutes;
