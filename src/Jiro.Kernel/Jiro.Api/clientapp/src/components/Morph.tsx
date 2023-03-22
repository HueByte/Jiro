import anime from "animejs/lib/anime.es.js";
import blob1 from "../assets/blob1.svg";

const Morph = () => {
  return (
    <div
      className="absolute top-0 left-0 -z-10 h-screen w-screen"
      style={{
        background: `url(${blob1})`,
        backgroundRepeat: "no-repeat",
        backgroundSize: "cover",
      }}
    ></div>
  );
};

export default Morph;
