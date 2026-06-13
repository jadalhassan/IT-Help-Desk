export function DashboardChart({ children, empty, title }) {
  return (
    <section className="panel analyticsPanel">
      <div className="panelHeader">
        <div>
          <p className="eyebrow">Analytics</p>
          <h2>{title}</h2>
        </div>
      </div>
      {empty ? <div className="emptyState">No chart data yet.</div> : children}
    </section>
  );
}
