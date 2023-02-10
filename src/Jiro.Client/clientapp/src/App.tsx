import "./App.css";
import { BrowserRouter, Router } from "react-router-dom";
import ClientRouter from "./routes/ClientRouter";
// import Loader from "./components/Loaders/Loader";
import { ReactNode, Suspense } from "react";

function App() {
  // const history: BrowserHistory = createBrowserHistory();

  return (
    <BrowserRouter>
      <Suspense fallback={<div>Loading...</div>}>
        <ErrorBoundary>
          <ClientRouter />
        </ErrorBoundary>
      </Suspense>
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
