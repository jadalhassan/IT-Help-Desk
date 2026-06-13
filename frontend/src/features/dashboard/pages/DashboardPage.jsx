import { ActivityTrendChart } from '../components/ActivityTrendChart';
import { KpiCard } from '../components/KpiCard';
import { RecentActivityList } from '../components/RecentActivityList';
import { TasksByStatusChart } from '../components/TasksByStatusChart';
import { useActivityTrends, useDashboardStats, useRecentActivity, useTasksByStatus } from '../hooks/useDashboardStats';

export function DashboardPage() {
  const stats = useDashboardStats();
  const tasksByStatus = useTasksByStatus();
  const activityTrends = useActivityTrends();
  const recentActivity = useRecentActivity();
  const loading = stats.isLoading || tasksByStatus.isLoading || activityTrends.isLoading || recentActivity.isLoading;
  const error = stats.error || tasksByStatus.error || activityTrends.error || recentActivity.error;

  if (loading) {
    return <div className="emptyState">Loading dashboard analytics...</div>;
  }

  if (error) {
    return <div className="emptyState">Unable to load dashboard analytics.</div>;
  }

  const data = stats.data ?? {};

  return (
    <section className="dashboardShell">
      <div className="kpiGrid">
        <KpiCard label="Workstreams" value={data.totalProjects} />
        <KpiCard label="Total Tasks" value={data.totalTasks} />
        <KpiCard label="Completed" value={data.completedTasks} tone="success" />
        <KpiCard label="Pending" value={data.pendingTasks} tone="warning" />
        <KpiCard label="Overdue" value={data.overdueTasks} tone="danger" />
        <KpiCard label="Active Users" value={data.activeUsers} />
        <KpiCard label="Uploaded Files" value={data.uploadedFiles} />
        <KpiCard label="Unread Alerts" value={data.unreadNotifications} tone="info" />
      </div>

      <div className="dashboardGrid">
        <TasksByStatusChart data={tasksByStatus.data} />
        <ActivityTrendChart data={activityTrends.data} />
        <RecentActivityList items={recentActivity.data} />
      </div>
    </section>
  );
}
