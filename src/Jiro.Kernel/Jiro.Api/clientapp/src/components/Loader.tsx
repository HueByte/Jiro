const Loader = ({ isOverlay = true }) => {
  if (isOverlay)
    return (
      <div className="absolute top-0 left-0 grid h-full w-full place-items-center bg-elementLight bg-opacity-60">
        <div className="jiro-loader">
          <div></div>
          <div></div>
          <div></div>
        </div>
      </div>
    );
  else
    return (
      <div className="jiro-loader relative top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
        <div></div>
        <div></div>
        <div></div>
      </div>
    );
};

export default Loader;
