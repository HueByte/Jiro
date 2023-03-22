import { useContext } from "react";
import { Navigate } from "react-router-dom";
import { AuthContext } from "../contexts/AuthContext";

interface PrivateRouteProps {
  roles?: string[];
  source?: string;
  outlet: JSX.Element;
}

export const ProtectedRoute = ({
  roles,
  source,
  outlet,
}: PrivateRouteProps): JSX.Element => {
  const authContext = useContext(AuthContext);

  if (!authContext?.isAuthenticated()) {
    return <Navigate to="/auth/login" replace />;
  }

  if (!authContext.isInRole(roles)) {
    return <Navigate to="/" replace />;
  }

  return outlet;
};

export default ProtectedRoute;
