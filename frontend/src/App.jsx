import { useEffect, useMemo, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import {
  addTicketComment,
  assignTicket,
  createTicket,
  deleteTicket,
  getAgents,
  getCategories,
  getStatuses,
  getTicket,
  getTickets,
  login,
  updateTicket,
  updateTicketStatus,
} from './api';
import { AttachmentList } from './features/attachments/components/AttachmentList';
import { DashboardPage } from './features/dashboard/pages/DashboardPage';
import { NotificationBell } from './features/notifications/components/NotificationBell';
import { useSignalRNotifications } from './features/notifications/hooks/useSignalRNotifications';
import './index.css';

const emptyTicket = {
  title: '',
  description: '',
  category: 'Bug',
  priority: 'Medium',
  status: 'Open',
};

const priorities = ['Low', 'Medium', 'High', 'Urgent'];

function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function statusClass(status) {
  return status.replace(/\s+/g, '').toLowerCase();
}

function LoginPanel({ onLogin }) {
  const [form, setForm] = useState({ email: 'admin@helpdesk.local', password: 'Admin@123' });
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');

    try {
      const session = await login(form);
      localStorage.setItem('helpdesk_token', session.token);
      localStorage.setItem('helpdesk_user', JSON.stringify(session));
      onLogin(session);
    } catch (loginError) {
      setError(loginError.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <main className="loginShell">
      <form className="panel loginPanel" onSubmit={handleSubmit}>
        <p className="eyebrow">IT Help Desk</p>
        <h1>Ticket Management</h1>
        <label>
          Email
          <input
            name="email"
            onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
            required
            type="email"
            value={form.email}
          />
        </label>
        <label>
          Password
          <input
            name="password"
            onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
            required
            type="password"
            value={form.password}
          />
        </label>
        {error && <p className="inlineError">{error}</p>}
        <button className="primaryButton" disabled={saving} type="submit">
          {saving ? 'Signing in...' : 'Sign In'}
        </button>
        <p className="muted compact">
          Admin: admin@helpdesk.local / Admin@123
          <br />
          Agent: agent@helpdesk.local / Agent@123
          <br />
          User: user@helpdesk.local / User@123
        </p>
      </form>
    </main>
  );
}

function TicketForm({ categories, editingTicket, onCancel, onSubmit, saving, userRole }) {
  const [form, setForm] = useState(emptyTicket);
  const [formError, setFormError] = useState('');

  useEffect(() => {
    setForm(editingTicket ?? { ...emptyTicket, category: categories[0] ?? emptyTicket.category });
    setFormError('');
  }, [categories, editingTicket]);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!form.title.trim() || !form.description.trim()) {
      setFormError('Title and description are required.');
      return;
    }

    setFormError('');
    await onSubmit(form);
    if (!editingTicket) {
      setForm({ ...emptyTicket, category: categories[0] ?? emptyTicket.category });
    }
  };

  return (
    <form className="panel formPanel" onSubmit={handleSubmit}>
      <div className="panelHeader">
        <div>
          <p className="eyebrow">{editingTicket ? `Ticket #${editingTicket.id}` : 'New ticket'}</p>
          <h2>{editingTicket ? 'Edit Ticket' : 'Create Ticket'}</h2>
        </div>
        {editingTicket && (
          <button className="ghostButton" onClick={onCancel} type="button">
            Cancel
          </button>
        )}
      </div>

      <label>
        Title
        <input name="title" onChange={handleChange} required value={form.title} />
      </label>

      <label>
        Description
        <textarea name="description" onChange={handleChange} required rows="5" value={form.description} />
      </label>

      <div className="fieldGrid">
        <label>
          Category
          <select name="category" onChange={handleChange} value={form.category}>
            {categories.map((category) => (
              <option key={category} value={category}>
                {category}
              </option>
            ))}
          </select>
        </label>

        <label>
          Priority
          <select name="priority" onChange={handleChange} value={form.priority}>
            {priorities.map((priority) => (
              <option key={priority} value={priority}>
                {priority}
              </option>
            ))}
          </select>
        </label>
      </div>

      {formError && <p className="inlineError">{formError}</p>}

      <button className="primaryButton" disabled={saving || (userRole === 'Agent' && !editingTicket)} type="submit">
        {saving ? 'Saving...' : editingTicket ? 'Update Ticket' : 'Create Ticket'}
      </button>
    </form>
  );
}

function TicketFilters({ categories, statuses, filters, onChange }) {
  return (
    <div className="filterGrid">
      <label>
        Category
        <select value={filters.category} onChange={(event) => onChange({ ...filters, category: event.target.value })}>
          {['All', ...categories].map((category) => (
            <option key={category} value={category}>{category}</option>
          ))}
        </select>
      </label>
      <label>
        Status
        <select value={filters.status} onChange={(event) => onChange({ ...filters, status: event.target.value })}>
          {['All', ...statuses].map((status) => (
            <option key={status} value={status}>{status}</option>
          ))}
        </select>
      </label>
      <label>
        Priority
        <select value={filters.priority} onChange={(event) => onChange({ ...filters, priority: event.target.value })}>
          {['All', ...priorities].map((priority) => (
            <option key={priority} value={priority}>{priority}</option>
          ))}
        </select>
      </label>
    </div>
  );
}

function TicketList({ tickets, selectedTicket, onDelete, onEdit, onSelect, userRole }) {
  if (!tickets.length) {
    return <div className="emptyState">No tickets match the selected filters.</div>;
  }

  return (
    <div className="ticketList">
      {tickets.map((ticket) => (
        <article className={selectedTicket?.id === ticket.id ? 'ticketItem selected' : 'ticketItem'} key={ticket.id}>
          <button className="ticketSummary" onClick={() => onSelect(ticket)} type="button">
            <span className="ticketTitle">{ticket.title}</span>
            <span className="ticketMeta">
              {ticket.category} - {ticket.priority} - {ticket.assignedAgentName ?? 'Unassigned'}
            </span>
          </button>
          <span className={`status ${statusClass(ticket.status)}`}>{ticket.status}</span>
          <div className="rowActions">
            <button className="ghostButton" onClick={() => onEdit(ticket)} type="button">
              Edit
            </button>
            {userRole === 'Admin' && (
              <button className="dangerButton" onClick={() => onDelete(ticket)} type="button">
                Delete
              </button>
            )}
          </div>
        </article>
      ))}
    </div>
  );
}

function TicketDetail({ agents, onAssign, onComment, onStatusChange, statuses, ticket, userRole }) {
  const [agentId, setAgentId] = useState('');
  const [status, setStatus] = useState('');
  const [comment, setComment] = useState({ content: '', visibility: 'Public' });

  useEffect(() => {
    setAgentId(ticket?.assignedAgentId?.toString() ?? '');
    setStatus(ticket?.status ?? '');
    setComment({ content: '', visibility: 'Public' });
  }, [ticket]);

  if (!ticket) {
    return (
      <section className="panel detailPanel">
        <p className="eyebrow">Ticket details</p>
        <h2>Select a Ticket</h2>
        <p className="muted">Choose a ticket from the queue to inspect its workflow and history.</p>
      </section>
    );
  }

  const submitComment = async (event) => {
    event.preventDefault();
    if (!comment.content.trim()) {
      return;
    }
    await onComment(ticket.id, comment);
    setComment({ content: '', visibility: 'Public' });
  };

  return (
    <section className="panel detailPanel">
      <div className="panelHeader">
        <div>
          <p className="eyebrow">Ticket #{ticket.id}</p>
          <h2>{ticket.title}</h2>
        </div>
        <span className={`status ${statusClass(ticket.status)}`}>{ticket.status}</span>
      </div>

      <p className="description">{ticket.description}</p>

      <dl className="detailGrid">
        <div><dt>Requester</dt><dd>{ticket.creatorName ?? 'Unknown'}</dd></div>
        <div><dt>Agent</dt><dd>{ticket.assignedAgentName ?? 'Unassigned'}</dd></div>
        <div><dt>Category</dt><dd>{ticket.category}</dd></div>
        <div><dt>Priority</dt><dd>{ticket.priority}</dd></div>
        <div><dt>Created</dt><dd>{formatDate(ticket.createdAtUtc)}</dd></div>
        <div><dt>Updated</dt><dd>{formatDate(ticket.updatedAtUtc)}</dd></div>
      </dl>

      {userRole === 'Admin' && (
        <div className="actionPanel">
          <label>
            Assign agent
            <select value={agentId} onChange={(event) => setAgentId(event.target.value)}>
              <option value="">Unassigned</option>
              {agents.map((agent) => (
                <option key={agent.id} value={agent.id}>{agent.fullName}</option>
              ))}
            </select>
          </label>
          <button className="primaryButton" disabled={!agentId} onClick={() => onAssign(ticket.id, Number(agentId))} type="button">
            Assign
          </button>
        </div>
      )}

      {(userRole === 'Admin' || userRole === 'Agent') && (
        <div className="actionPanel">
          <label>
            Update status
            <select value={status} onChange={(event) => setStatus(event.target.value)}>
              {statuses.map((item) => (
                <option key={item} value={item}>{item}</option>
              ))}
            </select>
          </label>
          <button className="primaryButton" onClick={() => onStatusChange(ticket.id, status)} type="button">
            Update
          </button>
        </div>
      )}

      <form className="commentForm" onSubmit={submitComment}>
        <label>
          Comment
          <textarea
            onChange={(event) => setComment((current) => ({ ...current, content: event.target.value }))}
            rows="3"
            value={comment.content}
          />
        </label>
        {(userRole === 'Admin' || userRole === 'Agent') && (
          <label>
            Visibility
            <select
              onChange={(event) => setComment((current) => ({ ...current, visibility: event.target.value }))}
              value={comment.visibility}
            >
              <option value="Public">Public</option>
              <option value="Internal">Internal</option>
            </select>
          </label>
        )}
        <button className="primaryButton" type="submit">Add Comment</button>
      </form>

      <HistorySection title="Comments" items={ticket.comments} empty="No comments yet." render={(item) => (
        <>
          <strong>{item.authorName ?? 'Unknown'}</strong>
          <span className="historyMeta">{item.visibility} - {formatDate(item.createdAtUtc)}</span>
          <p>{item.content}</p>
        </>
      )} />

      <AttachmentList relatedEntityId={ticket.id} relatedEntityType="ticket" userRole={userRole} />

      <HistorySection title="Status Timeline" items={ticket.statusHistory} empty="No status changes yet." render={(item) => (
        <>
          <strong>{item.oldStatus} to {item.newStatus}</strong>
          <span className="historyMeta">{item.changedByName ?? 'Unknown'} - {formatDate(item.changedAtUtc)}</span>
        </>
      )} />

      <HistorySection title="Audit Trail" items={ticket.activityLogs} empty="No activity recorded yet." render={(item) => (
        <>
          <strong>{item.actionType}</strong>
          <span className="historyMeta">{item.actorName ?? 'Unknown'} - {formatDate(item.createdAtUtc)}</span>
          <p>{item.description}</p>
        </>
      )} />
    </section>
  );
}

function HistorySection({ empty, items, render, title }) {
  return (
    <div className="historySection">
      <h3>{title}</h3>
      {items?.length ? (
        <ul>
          {items.map((item) => (
            <li key={item.id}>{render(item)}</li>
          ))}
        </ul>
      ) : (
        <p className="muted">{empty}</p>
      )}
    </div>
  );
}

export default function App() {
  const queryClient = useQueryClient();
  const [session, setSession] = useState(() => {
    const stored = localStorage.getItem('helpdesk_user');
    return stored ? JSON.parse(stored) : null;
  });
  const [activeView, setActiveView] = useState('tickets');
  const [tickets, setTickets] = useState([]);
  const [categories, setCategories] = useState([]);
  const [statuses, setStatuses] = useState([]);
  const [agents, setAgents] = useState([]);
  const [filters, setFilters] = useState({ category: 'All', status: 'All', priority: 'All' });
  const [selectedTicket, setSelectedTicket] = useState(null);
  const [editingTicket, setEditingTicket] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  useSignalRNotifications(Boolean(session));

  const metrics = useMemo(() => ({
    total: tickets.length,
    open: tickets.filter((ticket) => ticket.status === 'Open').length,
    assigned: tickets.filter((ticket) => ticket.assignedAgentId).length,
    urgent: tickets.filter((ticket) => ticket.priority === 'Urgent').length,
  }), [tickets]);

  const loadTickets = async () => {
    if (!session) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const data = await getTickets(filters);
      setTickets(data);
      setSelectedTicket((current) => data.find((ticket) => ticket.id === current?.id) ?? data[0] ?? null);
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    async function loadInitialData() {
      try {
        const [categoryData, statusData] = await Promise.all([getCategories(), getStatuses()]);
        setCategories(categoryData);
        setStatuses(statusData);
      } catch (loadError) {
        setError(loadError.message);
      }
    }

    loadInitialData();
  }, []);

  useEffect(() => {
    async function loadAgents() {
      if (session?.role !== 'Admin') {
        setAgents([]);
        return;
      }
      setAgents(await getAgents());
    }

    loadAgents().catch((agentError) => setError(agentError.message));
  }, [session]);

  useEffect(() => {
    loadTickets();
  }, [filters, session]);

  const refreshAfterChange = async (updatedTicket, successMessage) => {
    setMessage(successMessage);
    queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
    queryClient.invalidateQueries({ queryKey: ['notifications', 'unreadCount'] });
    await loadTickets();
    setSelectedTicket(updatedTicket);
  };

  const handleSubmit = async (ticket) => {
    setSaving(true);
    setError('');
    setMessage('');

    try {
      const savedTicket = editingTicket
        ? await updateTicket(editingTicket.id, ticket)
        : await createTicket(ticket);

      setEditingTicket(null);
      await refreshAfterChange(savedTicket, editingTicket ? 'Ticket updated successfully.' : 'Ticket created successfully.');
    } catch (saveError) {
      setError(saveError.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (ticket) => {
    if (!window.confirm(`Delete ticket "${ticket.title}"?`)) {
      return;
    }

    try {
      await deleteTicket(ticket.id);
      setMessage('Ticket deleted successfully.');
      setSelectedTicket(null);
      setEditingTicket(null);
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await loadTickets();
    } catch (deleteError) {
      setError(deleteError.message);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('helpdesk_token');
    localStorage.removeItem('helpdesk_user');
    queryClient.clear();
    setSession(null);
    setTickets([]);
    setSelectedTicket(null);
    setActiveView('tickets');
  };

  if (!session) {
    return <LoginPanel onLogin={setSession} />;
  }

  return (
    <main className="appShell">
      <header className="topBar">
        <div>
          <p className="eyebrow">{session.role} workspace</p>
          <h1>Ticket Management</h1>
        </div>
        <div className="metrics">
          <span>{metrics.total} total</span>
          <span>{metrics.open} open</span>
          <span>{metrics.assigned} assigned</span>
          <span>{metrics.urgent} urgent</span>
          <NotificationBell />
          <button className="ghostButton" onClick={handleLogout} type="button">Sign Out</button>
        </div>
      </header>

      {(message || error) && <div className={error ? 'alert error' : 'alert success'}>{error || message}</div>}

      <nav className="viewTabs" aria-label="Workspace views">
        <button className={activeView === 'tickets' ? 'active' : ''} onClick={() => setActiveView('tickets')} type="button">
          Tickets
        </button>
        <button className={activeView === 'dashboard' ? 'active' : ''} onClick={() => setActiveView('dashboard')} type="button">
          Dashboard
        </button>
      </nav>

      {activeView === 'dashboard' ? (
        <DashboardPage />
      ) : (
        <section className="workspace">
          <TicketForm
            categories={categories}
            editingTicket={editingTicket}
            onCancel={() => setEditingTicket(null)}
            onSubmit={handleSubmit}
            saving={saving}
            userRole={session.role}
          />

          <section className="panel listPanel">
            <div className="panelHeader">
              <div>
                <p className="eyebrow">Queue</p>
                <h2>Tickets</h2>
              </div>
            </div>

            <TicketFilters categories={categories} filters={filters} onChange={setFilters} statuses={statuses} />

            {loading ? (
              <div className="emptyState">Loading tickets...</div>
            ) : (
              <TicketList
                tickets={tickets}
                selectedTicket={selectedTicket}
                onDelete={handleDelete}
                onEdit={(ticket) => {
                  setEditingTicket(ticket);
                  setSelectedTicket(ticket);
                }}
                onSelect={setSelectedTicket}
                userRole={session.role}
              />
            )}
          </section>

          <TicketDetail
            agents={agents}
            onAssign={async (ticketId, agentId) => refreshAfterChange(await assignTicket(ticketId, agentId), 'Ticket assigned successfully.')}
            onComment={async (ticketId, comment) => {
              await addTicketComment(ticketId, comment);
              return refreshAfterChange(await getTicket(ticketId), 'Comment added successfully.');
            }}
            onStatusChange={async (ticketId, status) => refreshAfterChange(await updateTicketStatus(ticketId, status), 'Status updated successfully.')}
            statuses={statuses}
            ticket={selectedTicket}
            userRole={session.role}
          />
        </section>
      )}
    </main>
  );
}
