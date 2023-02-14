import { Outlet } from "react-router-dom";
import Menu from "./Menu";

const MainLayout = () => {
  return (
    <div className="flex h-screen w-full max-w-full flex-row">
      <Menu />
      <Outlet />
    </div>
  );
};

export default MainLayout;