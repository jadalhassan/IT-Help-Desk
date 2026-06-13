export function KpiCard({ label, value, tone = 'default' }) {
  return (
    <article className={`kpiCard ${tone}`}>
      <span>{label}</span>
      <strong>{value ?? '-'}</strong>
    </article>
  );
}
