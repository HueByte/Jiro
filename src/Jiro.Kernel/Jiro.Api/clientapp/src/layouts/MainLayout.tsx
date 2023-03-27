import { useContext, useEffect } from "react";
import { Outlet } from "react-router-dom";
import { AuthContext } from "../contexts/AuthContext";
import { infoToast } from "../lib/notifications";
import Menu from "./Menu";

const MainLayout = () => {
  const auth = useContext(AuthContext);
  useEffect(() => {
    infoToast(`Welcome ${auth?.authState?.username}!`);
  }, []);

  return (
    <div className="z-50 flex h-screen w-full max-w-full flex-row overflow-y-scroll">
      <Menu />
      <Outlet />
    </div>
  );
};

export default MainLayout;
