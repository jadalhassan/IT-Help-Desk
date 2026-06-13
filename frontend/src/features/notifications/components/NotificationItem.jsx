function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

export function NotificationItem({ notification, onMarkRead }) {
  return (
    <li className={`notificationItem ${notification.isRead ? 'read' : 'unread'} ${notification.type}`}>
      <div>
        <strong>{notification.title}</strong>
        <span>{formatDate(notification.createdAtUtc)}</span>
      </div>
      <p>{notification.message}</p>
      {!notification.isRead && (
        <button className="ghostButton compactButton" onClick={() => onMarkRead(notification.id)} type="button">
          Mark read
        </button>
      )}
    </li>
  );
}
