import { Outlet } from "react-router-dom";
import Menu from "./Menu";

const MainLayout = () => {
  return (
    <div className="flex w-full flex-row">
      <Menu />
      <div>
        <Outlet />
      </div>
    </div>
  );
};

export default MainLayout;
