import { Navigate, Route, Routes } from "react-router-dom";
import MainLayout from "../layouts/MainLayout";
import LoginPage from "../pages/auth/LoginPage";
import LogoutPage from "../pages/auth/Logout";
import Homepage from "../pages/homepage/Homepage";
import ProtectedRoute from "./ProtectedRoute";

const ClientRouter = () => {
  return (
    <Routes>
      <Route path="auth/login" element={<LoginPage />} />
      <Route path="logout" element={<LogoutPage />} />
      <Route path="/" element={<ProtectedRoute outlet={<MainLayout />} />}>
        <Route index element={<Homepage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" />} />
    </Routes>
  );
};

export default ClientRouter;
