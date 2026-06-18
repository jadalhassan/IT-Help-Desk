import { useEffect, useState } from 'react';
import {
  categorizeTicket,
  getAiStatus,
  getTroubleshootingSuggestions,
  recommendTicketPriority,
  sendAiChat,
  summarizeTicket,
} from '../../../api';

export function AiAssistantPanel({ ticket }) {
  const [status, setStatus] = useState(null);
  const [result, setResult] = useState(null);
  const [chatMessage, setChatMessage] = useState('');
  const [chatHistory, setChatHistory] = useState([]);
  const [loading, setLoading] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    getAiStatus()
      .then(setStatus)
      .catch((statusError) => setError(statusError.message));
  }, []);

  useEffect(() => {
    setResult(null);
    setChatHistory([]);
    setChatMessage('');
    setError('');
  }, [ticket?.id]);

  if (!ticket) {
    return null;
  }

  const runAction = async (action) => {
    setLoading(action);
    setError('');
    try {
      const actions = {
        category: () => categorizeTicket(ticket.id),
        priority: () => recommendTicketPriority(ticket.id),
        summary: () => summarizeTicket(ticket.id),
        troubleshooting: () => getTroubleshootingSuggestions(ticket.id),
      };
      setResult({ type: action, data: await actions[action]() });
    } catch (actionError) {
      setError(actionError.message);
    } finally {
      setLoading('');
    }
  };

  const submitChat = async (event) => {
    event.preventDefault();
    const message = chatMessage.trim();
    if (!message) {
      return;
    }

    setLoading('chat');
    setError('');
    setChatHistory((current) => [...current, { role: 'user', content: message }]);
    setChatMessage('');

    try {
      const response = await sendAiChat(message, ticket.id);
      setChatHistory((current) => [...current, { role: 'assistant', content: response.answer }]);
    } catch (chatError) {
      setError(chatError.message);
    } finally {
      setLoading('');
    }
  };

  return (
    <section className="aiPanel">
      <div className="panelHeader">
        <div>
          <p className="eyebrow">AI Assistant</p>
          <h3>Ticket Intelligence</h3>
        </div>
        {status && (
          <span className={status.configured ? 'aiStatus ready' : 'aiStatus'}>
            {status.provider}
          </span>
        )}
      </div>

      <div className="aiActionGrid">
        <button className="ghostButton" disabled={Boolean(loading)} onClick={() => runAction('category')} type="button">
          {loading === 'category' ? 'Thinking...' : 'Categorize'}
        </button>
        <button className="ghostButton" disabled={Boolean(loading)} onClick={() => runAction('priority')} type="button">
          {loading === 'priority' ? 'Thinking...' : 'Priority'}
        </button>
        <button className="ghostButton" disabled={Boolean(loading)} onClick={() => runAction('summary')} type="button">
          {loading === 'summary' ? 'Thinking...' : 'Summary'}
        </button>
        <button className="ghostButton" disabled={Boolean(loading)} onClick={() => runAction('troubleshooting')} type="button">
          {loading === 'troubleshooting' ? 'Thinking...' : 'Troubleshoot'}
        </button>
      </div>

      {error && <p className="inlineError">{error}</p>}
      {result && <AiResult result={result} />}

      <form className="aiChatForm" onSubmit={submitChat}>
        <div className="aiChatHistory">
          {chatHistory.length ? chatHistory.map((item, index) => (
            <p className={item.role === 'user' ? 'chatBubble user' : 'chatBubble'} key={`${item.role}-${index}`}>
              {item.content}
            </p>
          )) : <p className="muted compact">Ask about this ticket, next steps, or report context.</p>}
        </div>
        <label>
          Assistant message
          <textarea
            onChange={(event) => setChatMessage(event.target.value)}
            rows="2"
            value={chatMessage}
          />
        </label>
        <button className="primaryButton" disabled={loading === 'chat'} type="submit">
          {loading === 'chat' ? 'Sending...' : 'Send'}
        </button>
      </form>
    </section>
  );
}

function AiResult({ result }) {
  const { type, data } = result;

  if (type === 'category') {
    return (
      <div className="aiResult">
        <strong>Suggested category: {data.category}</strong>
        <span>{Math.round((data.confidence ?? 0) * 100)}% confidence</span>
        <p>{data.reason}</p>
      </div>
    );
  }

  if (type === 'priority') {
    return (
      <div className="aiResult">
        <strong>Recommended priority: {data.priority}</strong>
        <span>{Math.round((data.confidence ?? 0) * 100)}% confidence</span>
        <p>{data.reason}</p>
      </div>
    );
  }

  if (type === 'summary') {
    return (
      <div className="aiResult">
        <strong>Summary</strong>
        <p>{data.summary}</p>
      </div>
    );
  }

  return (
    <div className="aiResult">
      <strong>Troubleshooting suggestions</strong>
      <ol>
        {(data.suggestions ?? []).map((suggestion) => <li key={suggestion}>{suggestion}</li>)}
      </ol>
    </div>
  );
}
