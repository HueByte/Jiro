const { createProxyMiddleware } = require("http-proxy-middleware");
const { env } = require("process");

const onError = (err, req, resp, target) => {
  console.error(`${err.message}`);
};

const target = env.API_URL ?? "https://localhost:18091";

const context = ["/api"];

module.exports = function (app) {
  const appProxy = createProxyMiddleware(context, {
    target: target,
    onError: onError,
    secure: false,
    headers: {
      Connection: "Keep-Alive",
    },
  });

  app.use(appProxy);
};
