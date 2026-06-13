function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

export function RecentActivityList({ items }) {
  return (
    <section className="panel analyticsPanel">
      <div className="panelHeader">
        <div>
          <p className="eyebrow">Timeline</p>
          <h2>Recent Activity</h2>
        </div>
      </div>
      {items?.length ? (
        <ul className="activityList">
          {items.map((item) => (
            <li key={item.id}>
              <strong>{item.actionType}</strong>
              <span>{item.actorName ?? 'System'} - Ticket #{item.ticketId} - {formatDate(item.createdAtUtc)}</span>
              <p>{item.description}</p>
            </li>
          ))}
        </ul>
      ) : (
        <div className="emptyState">No activity recorded yet.</div>
      )}
    </section>
  );
}
