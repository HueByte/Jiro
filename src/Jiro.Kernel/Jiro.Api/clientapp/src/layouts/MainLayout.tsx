import { Outlet } from "react-router-dom";
import Menu from "./Menu";

const MainLayout = () => {
  return (
    <div className="z-50 flex h-screen w-full max-w-full flex-row overflow-y-scroll">
      <Menu />
      <Outlet />
    </div>
  );
};

export default MainLayout;
