import { useEffect } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { API_ROOT } from '../../../api';

export function useSignalRNotifications(enabled) {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!enabled) {
      return undefined;
    }

    const token = localStorage.getItem('helpdesk_token');
    if (!token) {
      return undefined;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_ROOT}/hubs/notifications`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('ReceiveNotification', (notification) => {
      queryClient.setQueryData(['notifications'], (current = []) => [notification, ...current]);
      queryClient.setQueryData(['notifications', 'unreadCount'], (current) => ({
        count: (current?.count ?? 0) + 1,
      }));
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    });

    connection.start().catch(() => {});

    return () => {
      connection.stop().catch(() => {});
    };
  }, [enabled, queryClient]);
}
