import { useMemo, useState } from 'react';
import { callEndpoint, login } from './api';
import './index.css';

const savedAuth = localStorage.getItem('auth');

export default function App() {
  const [email, setEmail] = useState('admin@helpdesk.local');
  const [password, setPassword] = useState('Admin@123');
  const [auth, setAuth] = useState(savedAuth ? JSON.parse(savedAuth) : null);
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const role = auth?.role ?? 'Guest';
  const token = auth?.token;

  const endpointButtons = useMemo(() => [
    { path: '/tickets/public', label: 'Public endpoint' },
    { path: '/tickets/user', label: 'User endpoint (any login)' },
    { path: '/tickets/agent', label: 'Agent endpoint' },
    { path: '/tickets/admin', label: 'Admin endpoint' },
  ], []);

  const onLogin = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage('');

    try {
      const result = await login(email, password);
      const authPayload = {
        token: result.token,
        fullName: result.fullName,
        email: result.email,
        role: result.role,
        expiresAtUtc: result.expiresAtUtc,
      };
      localStorage.setItem('auth', JSON.stringify(authPayload));
      setAuth(authPayload);
      setMessage(`Welcome ${result.fullName} (${result.role})`);
    } catch (error) {
      setMessage(error.message);
    } finally {
      setLoading(false);
    }
  };

  const onLogout = () => {
    localStorage.removeItem('auth');
    setAuth(null);
    setMessage('Logged out');
  };

  const testEndpoint = async (path) => {
    setLoading(true);
    setMessage('');

    try {
      const result = await callEndpoint(path, token);
      setMessage(result.message ?? 'Success');
    } catch (error) {
      setMessage(error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="page">
      <section className="card">
        <h1>Help Desk Auth Demo</h1>
        <p>Role-based authorization with React + ASP.NET Core JWT.</p>

        <form className="form" onSubmit={onLogin}>
          <label>
            Email
            <input value={email} onChange={(e) => setEmail(e.target.value)} required />
          </label>
          <label>
            Password
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </label>
          <button disabled={loading} type="submit">Login</button>
        </form>

        <div className="session">
          <strong>Current role:</strong> {role}
          {auth && <div>{auth.fullName} - {auth.email}</div>}
          {auth && <button onClick={onLogout}>Logout</button>}
        </div>

        <div className="actions">
          {endpointButtons.map((item) => (
            <button key={item.path} disabled={loading} onClick={() => testEndpoint(item.path)}>
              {item.label}
            </button>
          ))}
        </div>

        {message && <pre className="message">{message}</pre>}

        <small>
          Seed users: admin@helpdesk.local / Admin@123, agent@helpdesk.local / Agent@123, user@helpdesk.local / User@123
        </small>
      </section>
    </main>
  );
}