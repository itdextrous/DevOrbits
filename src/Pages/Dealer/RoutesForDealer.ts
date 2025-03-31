import React from "react";
import { MenuRoute } from "Routes";

const dealerRoutes: MenuRoute[] = [
  {
    title: "Stock",
    path: "dealer/stock/search",
    menu: true,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "dealer" */ "Pages/Dealer/Stock/Search")),
  },
  {
    title: "Stock Detail",
    path: "dealer/stock/detail/:stockId",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "dealer" */ "Pages/Dealer/Stock/Detail")),
  },
  {
    title: "Messages",
    path: "dealer/messages/conversations",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "dealer" */ "Pages/Dealer/Messaging/DealerInbox")),
  },
  {
    title: "Customer Inquiry",
    path: "dealer/messages/conversation/:conversationId",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "dealer" */ "Pages/Dealer/Messaging/CustomerInquiryConversation")),
  }
];

export default dealerRoutes;
