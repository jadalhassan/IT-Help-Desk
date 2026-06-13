import { Area, AreaChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { DashboardChart } from './DashboardChart';

export function ActivityTrendChart({ data }) {
  return (
    <DashboardChart title="Activity Trends" empty={!data?.length}>
      <div className="chartBox">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data}>
            <CartesianGrid strokeDasharray="3 3" vertical={false} />
            <XAxis dataKey="date" tick={{ fontSize: 12 }} />
            <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
            <Tooltip />
            <Legend />
            <Area dataKey="completedTasks" name="Completed" stroke="#17613a" fill="#d9ebe0" />
            <Area dataKey="uploads" name="Uploads" stroke="#7a4f00" fill="#fff6d7" />
            <Area dataKey="notifications" name="Notifications" stroke="#293a91" fill="#e8ebff" />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </DashboardChart>
  );
}
