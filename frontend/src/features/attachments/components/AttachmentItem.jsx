import { downloadAttachment } from '../api/attachmentsApi';
import { AttachmentPreview } from './AttachmentPreview';

function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function formatSize(size) {
  if (size < 1024 * 1024) {
    return `${Math.max(1, Math.round(size / 1024))} KB`;
  }

  return `${(size / 1024 / 1024).toFixed(1)} MB`;
}

export function AttachmentItem({ attachment, canDelete, onDelete }) {
  const handleDownload = async () => {
    await downloadAttachment(attachment);
  };

  return (
    <li className="attachmentItem">
      <AttachmentPreview attachment={attachment} />
      <div>
        <strong>{attachment.originalFileName}</strong>
        <span>{formatSize(attachment.size)} - {attachment.uploadedBy} - {formatDate(attachment.uploadedAt)}</span>
        {attachment.description && <p>{attachment.description}</p>}
      </div>
      <div className="rowActions">
        <button className="ghostButton compactButton" onClick={handleDownload} type="button">
          Download
        </button>
        {canDelete && (
          <button className="dangerButton compactButton" onClick={() => onDelete(attachment.id)} type="button">
            Delete
          </button>
        )}
      </div>
    </li>
  );
}
