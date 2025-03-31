import "./scss/theme.scss";
import {
  BrowserRouter as Router, Switch, Link, Route,
} from "react-router-dom";
import { Fragment, Suspense } from "react";
import { MenuRoute, appRoutes } from "Routes";
import ErrorBoundary from "Components/Utility/ErrorBoundary";
import configureIconLibrary from "Common/IconConfiguration";
import ScrollToTopOnPageChange from "Components/Navigation/ScrollToTopOnPageChange";
import PageTemplate from "Components/Navigation/PageTemplate";
import PrivateRoute from "Common/Authorization/PrivateRoute";
import { ProvideAuthorization, useAuthentication } from "Common/Authorization/ProvideAuthorization";
import OidcCallbackPage from "Pages/Public/OidcCallback";
import OidcSilentSignin from "Pages/Public/OidcSilentSignin";
import { QueryClient, QueryClientProvider } from "react-query";
import { ReactQueryDevtools } from "react-query/devtools";
import { Toaster } from "react-hot-toast";
import { ProvideRecentCustomers } from "Pages/Broker/RecentCustomerContext";

configureIconLibrary();

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      keepPreviousData: true,
    },
  },
});

function App() {
  return (
    <>
      <Toaster
        position="bottom-right"
        reverseOrder={false}
      />
      <ProvideAuthorization>
        <QueryClientProvider client={queryClient}>
          <ProvideRecentCustomers>
            <Router>
              <ScrollToTopOnPageChange />

              <PageTemplate>
                <ErrorBoundary>
                  <Suspense fallback={<div>Loading...</div>}>
                    <Switch>
                      <Route path="/public/openid/callback">
                        <OidcCallbackPage />
                      </Route>
                      <Route path="/public/openid/silentsignin">
                        <OidcSilentSignin />
                      </Route>
                      <Routes
                        routes={appRoutes}
                        parentPath=""
                        parentRequiresAuth={undefined}
                        parentRequiredRole={undefined}
                      />
                    </Switch>
                  </Suspense>
                </ErrorBoundary>
              </PageTemplate>

            </Router>
            <ReactQueryDevtools initialIsOpen={false} />
          </ProvideRecentCustomers>
        </QueryClientProvider>
      </ProvideAuthorization>
    </>

  );
}

interface RoutesProps {
  parentPath: string;
  parentRequiresAuth: boolean | undefined;
  parentRequiredRole: string | undefined;
  routes: MenuRoute[];
}

function Routes({
  parentPath, parentRequiresAuth, parentRequiredRole, routes,
}: RoutesProps) {
  const auth = useAuthentication();

  return (
    <>
      {routes.map((route) => {
        const requiresAuth = Boolean(parentRequiresAuth || route.requiresAuth);
        const requiredRole = parentRequiredRole || route.requiredRole;
        const isAuthorized = requiresAuth === auth.isAuthenticated;
        const isRoleCorrect = requiredRole ? requiredRole === auth.role : true;

        if (!isAuthorized || !isRoleCorrect) {
          // Don't render the route for roles that the user is not in or where authorization is different
          return null;
        }

        return (
          <Fragment key={`${parentPath}${route.path}:${route.requiredRole}`}>
            <PrivateRoute
              parentPath={parentPath}
              route={route}
              requiresAuth={requiresAuth}
              requiredRole={requiredRole}
            >
              <route.component />
            </PrivateRoute>
            {route.routes && (
              <Routes
                routes={route.routes}
                parentPath={parentPath + route.path}
                parentRequiresAuth={requiresAuth}
                parentRequiredRole={requiredRole}
              />
            )}
          </Fragment>
        );
      })}
    </>
  );
}

export function RouteLinks({ routes, parentPath }: { routes: MenuRoute[]; parentPath?: string; }) {
  function getPath(path: string) {
    return (parentPath ? `${parentPath}` : "") + path;
  }
  return (
    <>
      {routes.filter((route) => route.menu).map((route) => {
        const currentPath = getPath(route.path);

        return (
          <li className="c-sidebar-nav-item" key={currentPath}>
            <Link
              to={currentPath}
              className="c-sidebar-nav-link"
            >
              {route.title}
            </Link>
          </li>
        );
      })}
    </>
  );
}
RouteLinks.defaultProps = {
  parentPath: "",
};

export default App;
