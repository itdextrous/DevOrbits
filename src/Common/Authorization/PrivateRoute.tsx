import { useState, useEffect, ReactNode } from "react";
import { Route } from "react-router-dom";
import { RoleRoute } from "Routes";
import AccessRestricted from "./AccessRestricted";
import { useAuthentication } from "./ProvideAuthorization";

interface Props {
  parentPath: string;
  route: RoleRoute;
  requiresAuth: boolean;
  requiredRole: string | undefined;
  exact?: boolean;
  children: ReactNode;
}

const defaultProps = {
  exact: true,
};

export default function PrivateRoute({
  parentPath, route, requiresAuth, requiredRole, exact, children,
} : Props) {
  const [isAuthorized, setIsAuthorized] = useState(false);
  const auth = useAuthentication();

  useEffect(() => {
    if (!requiresAuth && !requiredRole) {
      // Public page
      setIsAuthorized(true);
      return;
    }

    if (requiredRole) {
      setIsAuthorized(auth.isInRole(requiredRole));
    } else {
      setIsAuthorized(auth.isAuthenticated);
    }
  }, [auth, requiredRole, requiresAuth, route]);

  return (
    <Route
      path={parentPath + route.path}
      exact={exact}
      render={() => (isAuthorized ? children : <AccessRestricted />)}
    />
  );
}

PrivateRoute.defaultProps = defaultProps;
