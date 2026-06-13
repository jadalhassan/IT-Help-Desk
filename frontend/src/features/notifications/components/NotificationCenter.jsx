import { NotificationItem } from './NotificationItem';
import { useMarkAllNotificationsRead, useMarkNotificationRead, useNotifications } from '../hooks/useNotifications';

export function NotificationCenter({ open }) {
  const notifications = useNotifications();
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  if (!open) {
    return null;
  }

  return (
    <aside className="notificationCenter">
      <div className="panelHeader">
        <div>
          <p className="eyebrow">Alerts</p>
          <h2>Notifications</h2>
        </div>
        <button className="ghostButton compactButton" onClick={() => markAllRead.mutate()} type="button">
          Read all
        </button>
      </div>
      {notifications.isLoading && <div className="emptyState">Loading notifications...</div>}
      {notifications.error && <div className="emptyState">Unable to load notifications.</div>}
      {!notifications.isLoading && !notifications.error && (
        notifications.data?.length ? (
          <ul className="notificationList">
            {notifications.data.map((notification) => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                onMarkRead={(id) => markRead.mutate(id)}
              />
            ))}
          </ul>
        ) : (
          <div className="emptyState">No notifications yet.</div>
        )
      )}
    </aside>
  );
}
