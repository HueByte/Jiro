import { useEffect } from "react";
import logo from "./logo.svg";
import "./App.css";
import { JiroService } from "./api";

function App() {
  useEffect(() => {
    (async () => {
      let test = await fetch("/api");
      console.log(test);

      let result: any = await JiroService.postApiJiro({
        requestBody: { prompt: "Hello World!" },
      });
      console.log(result);
    })();
  }, []);

  return (
    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
        <p>
          Edit <code>src/App.tsx</code> and save to reload.
        </p>
        <a
          className="App-link"
          href="https://reactjs.org"
          target="_blank"
          rel="noopener noreferrer"
        >
          Learn React
        </a>
      </header>
    </div>
  );
}

export default App;
