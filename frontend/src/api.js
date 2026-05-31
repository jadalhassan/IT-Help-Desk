const API_BASE = 'https://localhost:7243/api';

export async function login(email, password) {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    throw new Error('Invalid email or password');
  }

  return response.json();
}

export async function callEndpoint(path, token) {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  const response = await fetch(`${API_BASE}${path}`, { headers });
  const data = await response.json();

  if (!response.ok) {
    throw new Error(data?.message ?? `${response.status} ${response.statusText}`);
  }

  return data;
}