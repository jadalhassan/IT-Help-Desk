import { useState } from 'react';
import { useUnreadNotificationCount } from '../hooks/useNotifications';
import { NotificationCenter } from './NotificationCenter';

export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const unread = useUnreadNotificationCount();
  const count = unread.data?.count ?? 0;

  return (
    <div className="notificationWrap">
      <button
        aria-label="Open notifications"
        className="notificationBell"
        onClick={() => setOpen((current) => !current)}
        type="button"
      >
        !
        {count > 0 && <span>{count}</span>}
      </button>
      <NotificationCenter open={open} />
    </div>
  );
}
