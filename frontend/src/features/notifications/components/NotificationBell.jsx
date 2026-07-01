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
        aria-expanded={open}
        aria-haspopup="dialog"
        aria-label={open ? 'Close notifications' : 'Open notifications'}
        className="notificationBell"
        id="notification-button"
        onClick={() => setOpen((current) => !current)}
        type="button"
      >
        <span aria-hidden="true" className="bellGlyph">●</span>
        {count > 0 && <span aria-label={`${count} unread notifications`}>{count}</span>}
      </button>
      <NotificationCenter open={open} />
    </div>
  );
}
