import { useEffect, useMemo, useState } from 'react';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import {
  downloadTicketReportExcel,
  downloadTicketReportPdf,
  getReportFilters,
  getTicketReport,
} from '../../../api';
import { KpiCard } from '../../dashboard/components/KpiCard';

const defaultFilters = {
  startDate: '',
  endDate: '',
  status: 'All',
  priority: 'All',
  category: 'All',
  assignedAgentId: '',
  creatorUserId: '',
  search: '',
};

function cleanFilters(filters) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value && value !== 'All'));
}

function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
}

function formatHours(value) {
  if (value === null || value === undefined) {
    return 'n/a';
  }
  return `${Number(value).toFixed(1)}h`;
}

function BreakdownPanel({ items = [], title }) {
  return (
    <section className="panel analyticsPanel">
      <div className="panelHeader">
        <div>
          <p className="eyebrow">Breakdown</p>
          <h2>{title}</h2>
        </div>
      </div>
      {items.length ? (
        <div className="chartBox compactChart">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={items}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="count" fill="#2f6f9f" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      ) : (
        <div className="emptyState">No report data yet.</div>
      )}
    </section>
  );
}

export function ReportsPage() {
  const [filterOptions, setFilterOptions] = useState({ statuses: [], priorities: [], categories: [], agents: [], creators: [] });
  const [filters, setFilters] = useState(defaultFilters);
  const [report, setReport] = useState(null);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState('');
  const [error, setError] = useState('');

  const queryFilters = useMemo(() => cleanFilters(filters), [filters]);

  const loadReport = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await getTicketReport(queryFilters);
      setReport(data);
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    getReportFilters()
      .then(setFilterOptions)
      .catch((loadError) => setError(loadError.message));
  }, []);

  useEffect(() => {
    loadReport();
  }, [queryFilters]);

  const handleExport = async (type) => {
    setExporting(type);
    setError('');
    try {
      if (type === 'pdf') {
        await downloadTicketReportPdf(queryFilters);
      } else {
        await downloadTicketReportExcel(queryFilters);
      }
    } catch (exportError) {
      setError(exportError.message);
    } finally {
      setExporting('');
    }
  };

  const summary = report?.summary;

  return (
    <section className="dashboardShell">
      <section className="panel reportFilters">
        <div className="panelHeader">
          <div>
            <p className="eyebrow">Reports</p>
            <h2>Ticket Reports</h2>
          </div>
          <div className="rowActions">
            <button className="ghostButton" onClick={() => setFilters(defaultFilters)} type="button">
              Clear
            </button>
            <button className="primaryButton" disabled={exporting === 'pdf'} onClick={() => handleExport('pdf')} type="button">
              {exporting === 'pdf' ? 'Exporting...' : 'PDF'}
            </button>
            <button className="primaryButton" disabled={exporting === 'excel'} onClick={() => handleExport('excel')} type="button">
              {exporting === 'excel' ? 'Exporting...' : 'Excel'}
            </button>
          </div>
        </div>

        <div className="reportFilterGrid">
          <label>
            Start date
            <input type="date" value={filters.startDate} onChange={(event) => setFilters((current) => ({ ...current, startDate: event.target.value }))} />
          </label>
          <label>
            End date
            <input type="date" value={filters.endDate} onChange={(event) => setFilters((current) => ({ ...current, endDate: event.target.value }))} />
          </label>
          <label>
            Status
            <select value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value }))}>
              {['All', ...filterOptions.statuses].map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
          </label>
          <label>
            Priority
            <select value={filters.priority} onChange={(event) => setFilters((current) => ({ ...current, priority: event.target.value }))}>
              {['All', ...filterOptions.priorities].map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
          </label>
          <label>
            Category
            <select value={filters.category} onChange={(event) => setFilters((current) => ({ ...current, category: event.target.value }))}>
              {['All', ...filterOptions.categories].map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
          </label>
          <label>
            Agent
            <select value={filters.assignedAgentId} onChange={(event) => setFilters((current) => ({ ...current, assignedAgentId: event.target.value }))}>
              <option value="">All</option>
              {filterOptions.agents.map((agent) => <option key={agent.id} value={agent.id}>{agent.fullName}</option>)}
            </select>
          </label>
          <label>
            Requester
            <select value={filters.creatorUserId} onChange={(event) => setFilters((current) => ({ ...current, creatorUserId: event.target.value }))}>
              <option value="">All</option>
              {filterOptions.creators.map((creator) => <option key={creator.id} value={creator.id}>{creator.fullName}</option>)}
            </select>
          </label>
          <label>
            Search
            <input value={filters.search} onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value }))} placeholder="Title or description" />
          </label>
        </div>
      </section>

      {error && <div className="alert error">{error}</div>}
      {loading ? (
        <div className="emptyState">Loading report...</div>
      ) : (
        <>
          <div className="kpiGrid">
            <KpiCard label="Total Tickets" value={summary?.totalTickets ?? 0} />
            <KpiCard label="Open" value={summary?.openTickets ?? 0} tone="info" />
            <KpiCard label="Pending" value={summary?.pendingTickets ?? 0} tone="warning" />
            <KpiCard label="Resolved" value={summary?.resolvedTickets ?? 0} tone="success" />
            <KpiCard label="Closed" value={summary?.closedTickets ?? 0} />
            <KpiCard label="Overdue" value={summary?.overdueTickets ?? 0} tone="danger" />
            <KpiCard label="Avg Resolution" value={formatHours(summary?.averageResolutionHours)} />
          </div>

          <div className="dashboardGrid">
            <BreakdownPanel title="By Status" items={summary?.byStatus} />
            <BreakdownPanel title="By Priority" items={summary?.byPriority} />
            <BreakdownPanel title="By Category" items={summary?.byCategory} />
            <BreakdownPanel title="By Agent" items={summary?.byAssignedAgent} />
          </div>

          <section className="panel reportTablePanel">
            <div className="panelHeader">
              <div>
                <p className="eyebrow">Report Data</p>
                <h2>Tickets</h2>
              </div>
            </div>
            {report?.tickets?.length ? (
              <div className="reportTableWrap">
                <table className="reportTable">
                  <thead>
                    <tr>
                      <th>ID</th>
                      <th>Title</th>
                      <th>Status</th>
                      <th>Priority</th>
                      <th>Category</th>
                      <th>Agent</th>
                      <th>Requester</th>
                      <th>Created</th>
                      <th>Resolved</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.tickets.map((ticket) => (
                      <tr key={ticket.id}>
                        <td>#{ticket.id}</td>
                        <td>{ticket.title}</td>
                        <td>{ticket.status}</td>
                        <td>{ticket.priority}</td>
                        <td>{ticket.category}</td>
                        <td>{ticket.assignedAgentName}</td>
                        <td>{ticket.creatorName}</td>
                        <td>{formatDate(ticket.createdAtUtc)}</td>
                        <td>{ticket.resolvedAtUtc ? formatDate(ticket.resolvedAtUtc) : '-'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="emptyState">No tickets match the selected report filters.</div>
            )}
          </section>
        </>
      )}
    </section>
  );
}
