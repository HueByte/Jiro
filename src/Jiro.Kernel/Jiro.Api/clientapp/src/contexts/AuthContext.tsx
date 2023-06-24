import { createContext, useEffect, useState } from "react";
import { AuthService, VerifiedUser } from "../api";

interface AuthContextType {
  authState: VerifiedUser | null;
  setAuthState: (state: VerifiedUser) => void;
  signout: () => Promise<void>;
  isAuthenticated: () => boolean;
  isInRole: (roles?: string[]) => Boolean;
}

interface AuthContextProps {
  children: React.ReactNode;
}

const AuthContext = createContext<AuthContextType | null>(null);

const AuthProvider = ({ children }: AuthContextProps) => {
  const user: VerifiedUser | null = JSON.parse(
    localStorage.getItem("user") || "{}"
  );
  const [authState, setAuthState] = useState<VerifiedUser | null>(user);

  useEffect(() => {
    window.addEventListener("refreshUser", () => {
      setAuthState(JSON.parse(localStorage.getItem("user") || ""));
    });

    return () => {
      window.removeEventListener("refreshUser", () => {
        setAuthState(JSON.parse(localStorage.getItem("user") || ""));
      });
    };
  }, []);

  const setAuthInfo = (userData: VerifiedUser) => {
    localStorage.setItem("user", JSON.stringify(userData));

    setAuthState(userData);
  };

  const signout = async () => {
    try {
      await AuthService.postApiAuthLogout();
    } catch (ex) {}

    localStorage.clear();
    setAuthState(null);
  };

  const isAuthenticated = () => {
    if (
      authState === null ||
      authState === undefined ||
      Object.keys(authState).length === 0
    )
      return false;

    return true;
  };

  const isInRole = (roles?: string[]) => {
    if (!roles || roles === undefined || roles.length == 0) return true;

    let isInRole = roles.every((role) => authState?.roles?.includes(role));
    return isInRole;
  };

  const value: AuthContextType = {
    authState,
    setAuthState: (authInfo: VerifiedUser) => setAuthInfo(authInfo),
    signout,
    isAuthenticated,
    isInRole,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export { AuthContext, AuthProvider };
