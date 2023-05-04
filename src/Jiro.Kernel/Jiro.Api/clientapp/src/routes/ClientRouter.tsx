import { Navigate, Route, Routes } from "react-router-dom";
import { Roles } from "../api/Roles";
import MainLayout from "../layouts/MainLayout";
import AdminPage from "../pages/admin/AdminPage";
import UsersPage from "../pages/admin/pages/UsersPage";
import LoginPage from "../pages/auth/LoginPage";
import LogoutPage from "../pages/auth/Logout";
import HomePage from "../pages/homepage/Homepage";
import ServerPage from "../pages/server/ServerPage";
import ProtectedRoute from "./ProtectedRoute";
import WhiteListPage from "../pages/admin/pages/WhitelistPage";

const ClientRouter = () => {
  return (
    <Routes>
      <Route path="auth/login" element={<LoginPage />} />
      <Route path="logout" element={<LogoutPage />} />
      <Route path="/" element={<ProtectedRoute outlet={<MainLayout />} />}>
        <Route index element={<HomePage />} />
        <Route
          path="/admin/*"
          element={
            <ProtectedRoute roles={[Roles.ADMIN]} outlet={<AdminPage />} />
          }
        >
          <Route path="*" element={<Navigate to="users" replace />} />
          <Route path="users" element={<UsersPage />} />
          <Route path="whitelist" element={<WhiteListPage />} />
        </Route>
        <Route
          path="/server"
          element={
            <ProtectedRoute roles={[Roles.SERVER]} outlet={<ServerPage />} />
          }
        />
      </Route>
      <Route path="*" element={<Navigate to="/" />} />
    </Routes>
  );
};

export default ClientRouter;
