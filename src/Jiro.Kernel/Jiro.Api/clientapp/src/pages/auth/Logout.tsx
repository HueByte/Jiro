import { useContext, useEffect, useState } from "react";
import { Navigate } from "react-router-dom";
import { AuthContext } from "../../contexts/AuthContext";

const LogoutPage = () => {
  const auth = useContext(AuthContext);
  const [redirect, setRedirect] = useState(false);

  useEffect(() => {
    (async () => {
      await auth?.signout();
      setRedirect(true);
    })();
  }, []);

  return <>{redirect ? <Navigate to="/auth/login" /> : <>logging out...</>}</>;
};

export default LogoutPage;
