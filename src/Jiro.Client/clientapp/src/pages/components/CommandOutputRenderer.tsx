import { memo } from "react";
import { GraphOutput, TextOutput } from ".";
import { CommandType } from "../../api/CommandEnum";
import { UserCommand } from "../Models";

export const CommandOutputRenderer = memo(
  (props: { command?: UserCommand }) => {
    const { command } = props;

    if (command?.response?.commandType === CommandType.Text) {
      return (
        <>
          <div>
            <span className="text-primary">Me:</span> {command.prompt}
          </div>
          <TextOutput command={command.response} />
        </>
      );
    } else if (command?.response?.commandType === CommandType.Graph) {
      return (
        <>
          <div>
            <span className="text-primary">Me:</span> {command.prompt}
          </div>
          <GraphOutput command={command.response} />
        </>
      );
    } else {
      return (
        <>
          <div>
            <span className="text-primary">Me:</span> {command?.prompt}
            <div>
              {command?.isLoading ? (
                <div>
                  <span className="text-accent">Jiro: </span>...
                </div>
              ) : (
                <span>Something went wrong</span>
              )}
            </div>
          </div>
        </>
      );
    }
  }
);

// const TextOutput = (props: { command: CommandResponse }) => {
//   const { command } = props;

//   return (
//     <div className="my-1 break-words">
//       <span className="text-accent">Jiro:</span> {command.result?.data}
//     </div>
//   );
// };

// const GraphOutput = (props: { command: CommandResponse }) => {
//   const { command } = props;
//   const [data, setData] = useState<any>(null);
//   const [units, setUnits] = useState<any>(null);
//   const [summary, setSummary] = useState<any>(null);

//   useEffect(() => {
//     let data = JSON.parse(command.result?.data);
//     if (data && data.hourly) {
//       let hourly = data.hourly;

//       let finalData: any = hourly.time
//         .slice(0, 24)
//         .map((item: any, index: any) => {
//           return {
//             time: new Date(item + "Z").toLocaleTimeString([], {
//               hour: "2-digit",
//               minute: "2-digit",
//             }),
//             temp: hourly.temperature_2m[index],
//             wind: hourly.windspeed_10m[index],
//             rain: hourly.rain[index],
//           };
//         });

//       setUnits({
//         rain: data.hourly_units.rain,
//         wind: data.hourly_units.windspeed_10m,
//         temp: data.hourly_units.temperature_2m,
//       });

//       setData(finalData);

//       setSummary({
//         avgRain: average(hourly.rain),
//         avgTemp: average(hourly.temperature_2m),
//         avgWind: average(hourly.windspeed_10m),
//       });
//     }
//   }, []);

//   const average = (arr: any[]) => arr.reduce((a, b) => a + b) / arr.length;

//   return (
//     <div className="my-1 break-words">
//       <span className="text-accent">Jiro:</span>
//       <div className="w-full">
//         {data ? (
//           <div>
//             <ResponsiveContainer width="96%" height={500}>
//               <LineChart data={data}>
//                 <Line
//                   type="monotone"
//                   dataKey="temp"
//                   stroke="#c62368"
//                   unit={units.temp}
//                 />
//                 <Line
//                   type="monotone"
//                   dataKey="wind"
//                   stroke="#861657"
//                   unit={units.wind}
//                 />
//                 <Line
//                   type="monotone"
//                   dataKey="rain"
//                   stroke="#7300ff"
//                   unit={units.rain}
//                 />
//                 <CartesianGrid stroke="#FFF" />
//                 <XAxis dataKey="time" />
//                 <YAxis />
//                 <Tooltip />
//                 <Legend />
//               </LineChart>
//             </ResponsiveContainer>
//             <div>
//               Average:{" "}
//               <span>
//                 {" "}
//                 {summary.avgTemp.toFixed(2)} {units.temp}{" "}
//               </span>
//               <span>
//                 {" "}
//                 {summary.avgWind.toFixed(2)} {units.wind}
//               </span>
//               <span>
//                 {" "}
//                 {summary.avgRain.toFixed(2)} {units.rain}
//               </span>
//             </div>
//           </div>
//         ) : (
//           <></>
//         )}
//       </div>
//     </div>
//   );
// };
