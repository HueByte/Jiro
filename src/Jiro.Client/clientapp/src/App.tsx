import "./App.css";
import { BrowserRouter } from "react-router-dom";
import ClientRouter from "./routes/ClientRouter";
import { ReactNode, Suspense, useEffect } from "react";
import "./api/AxiosClient";
import { OpenAPI } from "./api";
import { AuthProvider } from "./contexts/AuthContext";
import Morph from "./components/Morph";

function App() {
  useEffect(() => {
    (async () => {
      let response = await fetch("/api/target");
      let result = await response.json();
      OpenAPI.BASE = result.apiUrl;
    })();
  }, []);

  return (
    <BrowserRouter>
      <ErrorBoundary>
        <AuthProvider>
          <Suspense fallback={<div>Loading...</div>}>
            <ClientRouter />
            <Morph />
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
