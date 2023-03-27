import "./App.css";
import { BrowserRouter } from "react-router-dom";
import ClientRouter from "./routes/ClientRouter";
import { ReactNode, Suspense } from "react";
import "./api/AxiosClient";
import { AuthProvider } from "./contexts/AuthContext";
import Morph from "./components/Morph";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

function App() {
  return (
    <BrowserRouter>
      <ErrorBoundary>
        <AuthProvider>
          <Suspense fallback={<div>Loading...</div>}>
            <ClientRouter />
            <Morph />
            <ToastContainer
              position="top-right"
              autoClose={5000}
              hideProgressBar={false}
              newestOnTop={false}
              closeOnClick
              rtl={false}
              pauseOnFocusLoss
              draggable
              pauseOnHover
              theme="dark"
            />
          </Suspense>
        </AuthProvider>
      </ErrorBoundary>
    </BrowserRouter>
  );
}

interface Props {
  children?: ReactNode;
}

const ErrorBoundary = ({ children }: Props) => {
  return <>{children}</>;
};

export default App;
