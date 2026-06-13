import { useDeleteAttachment, useAttachments } from '../hooks/useAttachments';
import { AttachmentItem } from './AttachmentItem';
import { FileUploadForm } from './FileUploadForm';

export function AttachmentList({ relatedEntityId, relatedEntityType, userRole }) {
  const attachments = useAttachments(relatedEntityType, relatedEntityId);
  const deleteAttachment = useDeleteAttachment(relatedEntityType, relatedEntityId);

  return (
    <div className="historySection attachmentsSection">
      <h3>Attachments</h3>
      <FileUploadForm relatedEntityId={relatedEntityId} relatedEntityType={relatedEntityType} />
      {attachments.isLoading && <p className="muted">Loading attachments...</p>}
      {attachments.error && <p className="inlineError">{attachments.error.message}</p>}
      {!attachments.isLoading && !attachments.error && (
        attachments.data?.length ? (
          <ul className="attachmentList">
            {attachments.data.map((attachment) => (
              <AttachmentItem
                attachment={attachment}
                canDelete={userRole === 'Admin'}
                key={attachment.id}
                onDelete={(id) => deleteAttachment.mutate(id)}
              />
            ))}
          </ul>
        ) : (
          <p className="muted">No attachments yet.</p>
        )
      )}
    </div>
  );
}
