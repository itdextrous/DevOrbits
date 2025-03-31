import { User } from "oidc-client";
import {
  createContext, ReactNode, useContext, useEffect, useState,
} from "react";
import { oidcUserManager } from "Services/Security/OidcUserManager";

interface AuthorizationProvider {
  user: User | null;
  isAuthenticated: boolean;
  setIsAuthenticated: (newState: boolean) => void,
  role: string;
  setRole: (newState: string) => void,
  isInRole: (requiredRole: string) => boolean;
  signIn: () => Promise<void>
  signOut: () => Promise<void>
  refreshUser: () => Promise<void>
}

const authorizationContext = createContext<AuthorizationProvider>({
  // Empty implementation of AuthorizationProvider
  user: null,
  role: "",
  isAuthenticated: false,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  setRole(newState: string) { },
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  setIsAuthenticated(newState: boolean) { },
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  isInRole: (requiredRole: string) => false,
  signIn: () => Promise.reject(new Error("stub function")),
  signOut: () => Promise.reject(new Error("stub function")),
  refreshUser: () => Promise.reject(new Error("stub function")),
});

interface Props {
  children: ReactNode
}

export function ProvideAuthorization({ children }: Props) {
  const [user, setUser] = useState<User|null>(null);
  const [role, setRole] = useState("");
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    async function handleUserLoaded(loadedUser: User | null) {
      updateAuthorizationState(loadedUser);
    }

    // User will be loaded when the oidcUserManager
    // silently renews the token
    // so this event will keep the user state up to date
    oidcUserManager.events.addUserLoaded(handleUserLoaded);

    oidcUserManager.getUser()
      .then((currentUser) => {
      
        updateAuthorizationState(currentUser);
      });

    return () => {
      oidcUserManager.events.removeUserLoaded(handleUserLoaded);
    };
  }, []);

  function updateAuthorizationState(updatedUser: User | null) {
    setUser(updatedUser);
    setIsAuthenticated(Boolean(updatedUser));
    setRole(updatedUser?.profile.role);
  }

  function isInRole(requiredRole: string) {
    return role === requiredRole;
  }

  async function signIn() {
    await oidcUserManager.signinRedirect();
  }

  async function signOut() {
    await oidcUserManager.signoutRedirect();
  }

  async function refreshUser() {
    const updatedUser = await oidcUserManager.getUser();
    updateAuthorizationState(updatedUser);
  }

  return (
    <authorizationContext.Provider
      value={{
        user,
        role,
        setRole,
        isAuthenticated,
        setIsAuthenticated,
        isInRole,
        signIn,
        signOut,
        refreshUser,
      }}
    >
      {children}
    </authorizationContext.Provider>
  );
}

export function useAuthentication() {
  return useContext(authorizationContext);
}
