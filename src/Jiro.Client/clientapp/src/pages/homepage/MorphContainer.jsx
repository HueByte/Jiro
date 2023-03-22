import { useEffect } from "react";
import jiroAvatar from "../../assets/Jiro.png";
import anime from "animejs/lib/anime.es.js";

const MorphAvatar = () => {
  useEffect(() => {
    // let source = document
    //   .getElementById("path1")
    //   .attributes.getNamedItem("d").value;

    // let target = document
    //   .getElementById("path2")
    //   .attributes.getNamedItem("d").value;
    // console.log(source, target);

    anime({
      targets: "#morphing .p",
      d: [
        {
          value:
            "M171.5,-191.9C215.3,-167.6,239.1,-107,234.1,-53.5C229,0.1,195,46.5,157.5,71.5C119.9,96.5,78.8,99.9,37.7,123.4C-3.3,146.9,-44.2,190.4,-72.4,185.9C-100.7,181.3,-116.1,128.7,-133.1,84.2C-150.1,39.7,-168.6,3.3,-177.6,-46.2C-186.7,-95.8,-186.2,-158.6,-155,-185.1C-123.8,-211.6,-61.9,-201.8,1,-202.9C63.9,-204.1,127.7,-216.2,171.5,-191.9Z",
        },
        {
          value:
            "M95,-109.2C143.2,-72.7,216.4,-61.9,232.3,-32C248.2,-2.2,206.8,46.6,170.7,88.7C134.7,130.7,103.9,165.9,67.2,175C30.6,184.2,-12.1,167.3,-52.4,150C-92.8,132.7,-130.8,115.1,-141.6,86.6C-152.3,58,-135.7,18.5,-134,-29.3C-132.4,-77.1,-145.6,-133.1,-125.7,-174.6C-105.8,-216.1,-52.9,-243.1,-14.8,-225.5C23.4,-207.8,46.7,-145.7,95,-109.2Z",
        },
      ],
      easing: "easeInOutSine",
      duration: 10000,
      direction: "infinite alternate",
      loop: true,
    });
  });
  return (
    <div
      className={`relative h-fit w-[256px] flex-shrink-0 overflow-visible rounded-full transition duration-1000 lg:mx-auto lg:w-[196px] md:w-[128px]`}
    >
      <img className="rounded-full" src={jiroAvatar} alt="jiro avatar" />
      <svg
        viewBox="0 0 500 500"
        xmlns="http://www.w3.org/2000/svg"
        width="100%"
        id="blobSvg"
        className="absolute top-0 left-0 -z-[5] h-full w-full"
      >
        <path fill="#000c14">
          <animate
            attributeName="d"
            dur="9.5s"
            repeatCount="indefinite"
            values="M432,292Q380,334,357,384Q334,434,276,453.5Q218,473,181,422.5Q144,372,111,336.5Q78,301,84,252Q90,203,107,154Q124,105,175.5,99Q227,93,283,74Q339,55,382.5,96Q426,137,455,193.5Q484,250,432,292Z;
                  M423.5,302Q412,354,374,396Q336,438,282,422Q228,406,188,387Q148,368,79.5,344Q11,320,20,252.5Q29,185,87,157Q145,129,182.5,86.5Q220,44,285.5,36Q351,28,401,74.5Q451,121,443,185.5Q435,250,423.5,302Z;
                  M431.5,301.5Q410,353,368,385Q326,417,275.5,421Q225,425,184,399Q143,373,114,336Q85,299,69,245.5Q53,192,70,127Q87,62,152.5,46Q218,30,270.5,60Q323,90,351.5,128.5Q380,167,416.5,208.5Q453,250,431.5,301.5Z;
                  M420,305Q421,360,383.5,410Q346,460,281,472Q216,484,168,442Q120,400,99,350.5Q78,301,69.5,247.5Q61,194,92,149Q123,104,170.5,66Q218,28,279,40.5Q340,53,368.5,104Q397,155,408,202.5Q419,250,420,305Z;
                  M402.11059,293.2441Q384.19892,336.4882,365.61059,399.26635Q347.02225,462.0445,283.0445,462.2886Q219.06676,462.5327,173.11126,427.17734Q127.15577,391.82198,114.33378,342.83311Q101.5118,293.84423,66.43324,239.83311Q31.35469,185.82198,81.26635,149.51113Q131.17802,113.20027,175.10013,74.68914Q219.02225,36.17802,272.35536,59.92279Q325.68847,83.66756,382.81153,105.53338Q439.93459,127.39919,429.97842,188.6996Q420.02225,250,402.11059,293.2441Z;
                  M457.78183,303.83659Q418.46519,357.67317,369.1337,380.21307Q319.80221,402.75298,272.81197,410.045Q225.82173,417.33702,160.08913,423.46053Q94.35654,429.58404,55.94101,373.92063Q17.52547,318.25721,34.15875,255.0989Q50.79202,191.94058,82.4011,142.07937Q114.01019,92.21817,166.6388,63.58956Q219.2674,34.96096,272.05942,61.11418Q324.85144,87.2674,389.83149,103.00509Q454.81154,118.74279,475.955,184.37139Q497.09847,250,457.78183,303.83659Z;
                  M417.42235,298.3814Q401.02048,346.7628,363.75597,381.74744Q326.49147,416.73208,276.99488,410.37287Q227.49829,404.01365,176.48123,399.0273Q125.46417,394.04095,90.45905,350.5273Q55.45393,307.01365,44.2372,246.88481Q33.02048,186.75597,71.88652,138.12884Q110.75256,89.50171,167.49829,78.8558Q224.24403,68.2099,270.87628,85.09642Q317.50853,101.98294,382.63396,112.61348Q447.75938,123.24403,440.7918,186.62201Q433.82422,250,417.42235,298.3814Z;
                  M432,292Q380,334,357,384Q334,434,276,453.5Q218,473,181,422.5Q144,372,111,336.5Q78,301,84,252Q90,203,107,154Q124,105,175.5,99Q227,93,283,74Q339,55,382.5,96Q426,137,455,193.5Q484,250,432,292Z;
                  "
          ></animate>
        </path>
      </svg>
    </div>
  );
};

export default MorphAvatar;
