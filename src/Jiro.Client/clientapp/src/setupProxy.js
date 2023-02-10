const { createProxyMiddleware } = require("http-proxy-middleware");
const { env } = require("process");

const onError = (err, req, resp, target) => {
  console.error(`${err.message}`);
};

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS
  ? env.ASPNETCORE_URLS.split(";")[0]
  : "http://localhost:5000";

const context = ["/api"];

module.exports = function (app) {
  console.log(target);
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
