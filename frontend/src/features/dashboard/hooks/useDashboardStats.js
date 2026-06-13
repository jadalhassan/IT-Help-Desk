import { useQuery } from '@tanstack/react-query';
import { getActivityTrends, getDashboardStats, getRecentActivity, getTasksByStatus } from '../../../api';

export function useDashboardStats() {
  return useQuery({
    queryKey: ['dashboard', 'stats'],
    queryFn: getDashboardStats,
  });
}

export function useTasksByStatus() {
  return useQuery({
    queryKey: ['dashboard', 'tasksByStatus'],
    queryFn: getTasksByStatus,
  });
}

export function useActivityTrends() {
  return useQuery({
    queryKey: ['dashboard', 'activityTrends'],
    queryFn: getActivityTrends,
  });
}

export function useRecentActivity() {
  return useQuery({
    queryKey: ['dashboard', 'recentActivity'],
    queryFn: getRecentActivity,
  });
}
