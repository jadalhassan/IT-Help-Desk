import { useForm } from 'react-hook-form';
import { maxFileSize, allowedFileTypes } from '../types/attachmentTypes';
import { useUploadAttachment } from '../hooks/useAttachments';

export function FileUploadForm({ relatedEntityId, relatedEntityType, onUploaded }) {
  const {
    formState: { errors },
    handleSubmit,
    register,
    reset,
    watch,
  } = useForm({ defaultValues: { description: '', file: null } });
  const upload = useUploadAttachment(relatedEntityType, relatedEntityId);
  const selectedFile = watch('file')?.[0];

  const submit = async (values) => {
    await upload.mutateAsync({
      file: values.file[0],
      relatedEntityType,
      relatedEntityId,
      description: values.description,
    });
    reset();
    onUploaded?.();
  };

  return (
    <form className="attachmentForm" onSubmit={handleSubmit(submit)}>
      <label>
        Add attachment
        <input
          type="file"
          {...register('file', {
            required: 'Choose a screenshot or document.',
            validate: {
              size: (files) => !files?.[0] || files[0].size <= maxFileSize || 'File must be 10 MB or less.',
              type: (files) => !files?.[0] || allowedFileTypes.includes(files[0].type) || 'This file type is not allowed.',
            },
          })}
        />
      </label>
      <label>
        Description
        <input placeholder="Optional note" {...register('description')} />
      </label>
      {selectedFile && <p className="selectedFile">{selectedFile.name}</p>}
      {errors.file && <p className="inlineError">{errors.file.message}</p>}
      {upload.error && <p className="inlineError">{upload.error.message}</p>}
      <button className="primaryButton" disabled={upload.isPending} type="submit">
        {upload.isPending ? 'Uploading...' : 'Upload'}
      </button>
    </form>
  );
}
