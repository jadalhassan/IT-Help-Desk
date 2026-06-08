const API_BASE = import.meta.env.VITE_API_BASE || 'https://localhost:7243/api';

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
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

export function getTickets(category = 'All') {
  const query = category && category !== 'All' ? `?category=${encodeURIComponent(category)}` : '';
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

export function deleteTicket(id) {
  return request(`/tickets/${id}`, {
    method: 'DELETE',
  });
}

export function getCategories() {
  return request('/categories');
}
