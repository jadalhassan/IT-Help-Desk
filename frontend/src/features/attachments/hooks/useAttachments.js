import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { deleteAttachment, getAttachments, uploadAttachment } from '../api/attachmentsApi';

export function useAttachments(relatedEntityType, relatedEntityId) {
  return useQuery({
    queryKey: ['attachments', relatedEntityType, relatedEntityId],
    queryFn: () => getAttachments(relatedEntityType, relatedEntityId),
    enabled: Boolean(relatedEntityType && relatedEntityId),
  });
}

export function useUploadAttachment(relatedEntityType, relatedEntityId) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: uploadAttachment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['attachments', relatedEntityType, relatedEntityId] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['tickets'] });
    },
  });
}

export function useDeleteAttachment(relatedEntityType, relatedEntityId) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteAttachment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['attachments', relatedEntityType, relatedEntityId] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
}
