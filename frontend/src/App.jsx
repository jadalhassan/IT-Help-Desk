import { useEffect, useMemo, useState } from 'react';
import {
  createTicket,
  deleteTicket,
  getCategories,
  getTickets,
  updateTicket,
} from './api';
import './index.css';

const emptyTicket = {
  title: '',
  description: '',
  category: 'Bug',
  priority: 'Medium',
  status: 'Open',
};

const priorities = ['Low', 'Medium', 'High', 'Urgent'];
const statuses = ['Open', 'In Progress', 'Resolved', 'Closed'];

function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function CategoryFilter({ categories, activeCategory, onChange }) {
  return (
    <div className="filterBar" aria-label="Filter tickets by category">
      {['All', ...categories].map((category) => (
        <button
          className={category === activeCategory ? 'filterButton active' : 'filterButton'}
          key={category}
          onClick={() => onChange(category)}
          type="button"
        >
          {category}
        </button>
      ))}
    </div>
  );
}

function TicketForm({ categories, editingTicket, onCancel, onSubmit, saving }) {
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
        <input
          name="title"
          onChange={handleChange}
          placeholder="Brief summary"
          required
          value={form.title}
        />
      </label>

      <label>
        Description
        <textarea
          name="description"
          onChange={handleChange}
          placeholder="Describe the issue or request"
          required
          rows="5"
          value={form.description}
        />
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

        <label>
          Status
          <select name="status" onChange={handleChange} value={form.status}>
            {statuses.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </label>
      </div>

      {formError && <p className="inlineError">{formError}</p>}

      <button className="primaryButton" disabled={saving} type="submit">
        {saving ? 'Saving...' : editingTicket ? 'Update Ticket' : 'Create Ticket'}
      </button>
    </form>
  );
}

function TicketList({ tickets, selectedTicket, onDelete, onEdit, onSelect }) {
  if (!tickets.length) {
    return <div className="emptyState">No tickets match the selected category.</div>;
  }

  return (
    <div className="ticketList">
      {tickets.map((ticket) => (
        <article
          className={selectedTicket?.id === ticket.id ? 'ticketItem selected' : 'ticketItem'}
          key={ticket.id}
        >
          <button className="ticketSummary" onClick={() => onSelect(ticket)} type="button">
            <span className="ticketTitle">{ticket.title}</span>
            <span className="ticketMeta">
              {ticket.category} · {ticket.priority} · {formatDate(ticket.createdAtUtc)}
            </span>
          </button>
          <span className={`status ${ticket.status.replace(/\s+/g, '').toLowerCase()}`}>
            {ticket.status}
          </span>
          <div className="rowActions">
            <button className="ghostButton" onClick={() => onEdit(ticket)} type="button">
              Edit
            </button>
            <button className="dangerButton" onClick={() => onDelete(ticket)} type="button">
              Delete
            </button>
          </div>
        </article>
      ))}
    </div>
  );
}

function TicketDetail({ ticket }) {
  if (!ticket) {
    return (
      <section className="panel detailPanel">
        <p className="eyebrow">Ticket details</p>
        <h2>Select a Ticket</h2>
        <p className="muted">Choose a ticket from the list to inspect its description and timeline.</p>
      </section>
    );
  }

  return (
    <section className="panel detailPanel">
      <div className="panelHeader">
        <div>
          <p className="eyebrow">Ticket #{ticket.id}</p>
          <h2>{ticket.title}</h2>
        </div>
        <span className={`status ${ticket.status.replace(/\s+/g, '').toLowerCase()}`}>
          {ticket.status}
        </span>
      </div>

      <p className="description">{ticket.description}</p>

      <dl className="detailGrid">
        <div>
          <dt>Category</dt>
          <dd>{ticket.category}</dd>
        </div>
        <div>
          <dt>Priority</dt>
          <dd>{ticket.priority}</dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd>{formatDate(ticket.createdAtUtc)}</dd>
        </div>
        <div>
          <dt>Updated</dt>
          <dd>{formatDate(ticket.updatedAtUtc)}</dd>
        </div>
      </dl>
    </section>
  );
}

export default function App() {
  const [tickets, setTickets] = useState([]);
  const [categories, setCategories] = useState([]);
  const [activeCategory, setActiveCategory] = useState('All');
  const [selectedTicket, setSelectedTicket] = useState(null);
  const [editingTicket, setEditingTicket] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const metrics = useMemo(() => ({
    total: tickets.length,
    open: tickets.filter((ticket) => ticket.status === 'Open').length,
    urgent: tickets.filter((ticket) => ticket.priority === 'Urgent').length,
  }), [tickets]);

  const loadTickets = async (category = activeCategory) => {
    setLoading(true);
    setError('');

    try {
      const data = await getTickets(category);
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
        const categoryData = await getCategories();
        setCategories(categoryData);
      } catch (categoryError) {
        setError(categoryError.message);
      }
    }

    loadInitialData();
  }, []);

  useEffect(() => {
    loadTickets(activeCategory);
  }, [activeCategory]);

  const handleSubmit = async (ticket) => {
    setSaving(true);
    setError('');
    setMessage('');

    try {
      const savedTicket = editingTicket
        ? await updateTicket(editingTicket.id, ticket)
        : await createTicket(ticket);

      setMessage(editingTicket ? 'Ticket updated successfully.' : 'Ticket created successfully.');
      setEditingTicket(null);
      await loadTickets(activeCategory);
      setSelectedTicket(savedTicket);
    } catch (saveError) {
      setError(saveError.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (ticket) => {
    const confirmed = window.confirm(`Delete ticket "${ticket.title}"?`);
    if (!confirmed) {
      return;
    }

    setError('');
    setMessage('');

    try {
      await deleteTicket(ticket.id);
      setMessage('Ticket deleted successfully.');
      if (selectedTicket?.id === ticket.id) {
        setSelectedTicket(null);
      }
      if (editingTicket?.id === ticket.id) {
        setEditingTicket(null);
      }
      await loadTickets(activeCategory);
    } catch (deleteError) {
      setError(deleteError.message);
    }
  };

  const handleEdit = (ticket) => {
    setEditingTicket(ticket);
    setSelectedTicket(ticket);
    setMessage('');
    setError('');
  };

  return (
    <main className="appShell">
      <header className="topBar">
        <div>
          <p className="eyebrow">IT Help Desk</p>
          <h1>Ticket Management</h1>
        </div>
        <div className="metrics">
          <span>{metrics.total} total</span>
          <span>{metrics.open} open</span>
          <span>{metrics.urgent} urgent</span>
        </div>
      </header>

      {(message || error) && (
        <div className={error ? 'alert error' : 'alert success'}>
          {error || message}
        </div>
      )}

      <section className="workspace">
        <TicketForm
          categories={categories}
          editingTicket={editingTicket}
          onCancel={() => setEditingTicket(null)}
          onSubmit={handleSubmit}
          saving={saving}
        />

        <section className="panel listPanel">
          <div className="panelHeader">
            <div>
              <p className="eyebrow">Queue</p>
              <h2>Tickets</h2>
            </div>
          </div>

          <CategoryFilter
            activeCategory={activeCategory}
            categories={categories}
            onChange={setActiveCategory}
          />

          {loading ? (
            <div className="emptyState">Loading tickets...</div>
          ) : (
            <TicketList
              tickets={tickets}
              selectedTicket={selectedTicket}
              onDelete={handleDelete}
              onEdit={handleEdit}
              onSelect={setSelectedTicket}
            />
          )}
        </section>

        <TicketDetail ticket={selectedTicket} />
      </section>
    </main>
  );
}
