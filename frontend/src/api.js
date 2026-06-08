const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5088/api';

async function request(path, options = {}) {
  const token = localStorage.getItem('helpdesk_token');
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
    ...options,
  });

  if (response.status === 204) {
    return null;
  }

  const data = await response.json().catch(() => null);

  if (!response.ok) {
    throw new Error(data?.message ?? `${response.status} ${response.statusText}`);
  }

  return data;
}

export function login(credentials) {
  return request('/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials),
  });
}

export function getTickets(filters = {}) {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value && value !== 'All') {
      params.set(key, value);
    }
  });
  const query = params.toString() ? `?${params}` : '';
  return request(`/tickets${query}`);
}

export function getTicket(id) {
  return request(`/tickets/${id}`);
}

export function createTicket(ticket) {
  return request('/tickets', {
    method: 'POST',
    body: JSON.stringify(ticket),
  });
}

export function updateTicket(id, ticket) {
  return request(`/tickets/${id}`, {
    method: 'PUT',
    body: JSON.stringify(ticket),
  });
}

export function assignTicket(id, agentUserId) {
  return request(`/tickets/${id}/assign`, {
    method: 'POST',
    body: JSON.stringify({ agentUserId }),
  });
}

export function updateTicketStatus(id, status) {
  return request(`/tickets/${id}/status`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  });
}

export function addTicketComment(id, comment) {
  return request(`/tickets/${id}/comments`, {
    method: 'POST',
    body: JSON.stringify(comment),
  });
}

export function deleteTicket(id) {
  return request(`/tickets/${id}`, {
    method: 'DELETE',
  });
}

export function getCategories() {
  return request('/categories');
}

export function getStatuses() {
  return request('/statuses');
}

export function getAgents() {
  return request('/users/agents');
}
