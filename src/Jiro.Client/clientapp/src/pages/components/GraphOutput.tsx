import { useEffect, useState } from "react";
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

export const GraphOutput = (props: { command: any }) => {
  const { command } = props;
  const [data, setData] = useState<any>(null);
  const [units, setUnits] = useState<any>(null);
  const [members, setMembers] = useState<string[]>([]);
  const colors = [
    "#c62368",
    "#7300ff",
    "#FFA69E",
    "#861657",
    "#00fa9a",
    "#fa7268",
  ];

  useEffect(() => {
    // convert date to local time
    let reqData = command.result?.data
      .slice(0, 24)
      .map((item: any, index: any) => {
        return {
          ...item,
          date: new Date(item.date + "Z").toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
          }),
        };
      });

    // get members
    let props = Object.getOwnPropertyNames(command.result?.data[0]);

    setData(reqData);
    setUnits(command.result?.units);
    setMembers(props);
  }, []);

  const getColor = () => {
    return colors[Math.floor(Math.random() * colors.length)];
  };

  const isNumeric = (val: string): boolean => {
    return !isNaN(Number(val));
  };

  return (
    <div className="my-1 break-words">
      <span className="text-accent">Jiro:</span>
      <div className="w-full">
        {data ? (
          <div>
            <ResponsiveContainer width="96%" height={500}>
              <LineChart data={data}>
                {members.map((member, index) => {
                  let instance = data[0];
                  let value = instance[member];
                  if (!isNumeric(value)) return;

                  return (
                    <Line
                      key={index}
                      type="monotone"
                      dataKey={member}
                      unit={units[member]}
                      stroke={getColor()}
                    />
                  );
                })}
                <CartesianGrid stroke="#FFF" />
                <XAxis angle={-10} dataKey={command.result.xAxis} />
                <YAxis />
                <Tooltip />
                <Legend />
              </LineChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <></>
        )}
      </div>
    </div>
  );
};
