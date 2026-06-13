export const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5088/api';
export const API_ROOT = API_BASE.replace(/\/api\/?$/, '');

async function request(path, options = {}) {
  const token = localStorage.getItem('helpdesk_token');
  const isFormData = options.body instanceof FormData;
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
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

export function getDashboardStats() {
  return request('/dashboard/stats');
}

export function getTasksByStatus() {
  return request('/dashboard/charts/tasks-by-status');
}

export function getActivityTrends() {
  return request('/dashboard/charts/activity-trends');
}

export function getRecentActivity() {
  return request('/dashboard/recent-activity');
}

export function getNotifications() {
  return request('/notifications');
}

export function getUnreadNotificationCount() {
  return request('/notifications/unread-count');
}

export function markNotificationRead(id) {
  return request(`/notifications/${id}/read`, {
    method: 'PATCH',
  });
}

export function markAllNotificationsRead() {
  return request('/notifications/read-all', {
    method: 'PATCH',
  });
}

export function getAttachments(relatedEntityType, relatedEntityId) {
  const params = new URLSearchParams({ relatedEntityType, relatedEntityId: String(relatedEntityId) });
  return request(`/attachments?${params}`);
}

export function uploadAttachment({ file, relatedEntityType, relatedEntityId, description }) {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('relatedEntityType', relatedEntityType);
  formData.append('relatedEntityId', String(relatedEntityId));
  if (description) {
    formData.append('description', description);
  }

  return request('/attachments/upload', {
    method: 'POST',
    body: formData,
  });
}

export function deleteAttachment(id) {
  return request(`/attachments/${id}`, {
    method: 'DELETE',
  });
}

export async function downloadAttachment(attachment) {
  const token = localStorage.getItem('helpdesk_token');
  const response = await fetch(`${API_BASE}/attachments/${attachment.id}/download`, {
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new Error(data?.message ?? 'Download failed.');
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = attachment.originalFileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
