export function AttachmentPreview({ attachment }) {
  const isImage = attachment.contentType.startsWith('image/');
  return (
    <span className={isImage ? 'attachmentPreview image' : 'attachmentPreview document'}>
      {isImage ? 'IMG' : attachment.originalFileName.split('.').pop()?.toUpperCase() ?? 'FILE'}
    </span>
  );
}
