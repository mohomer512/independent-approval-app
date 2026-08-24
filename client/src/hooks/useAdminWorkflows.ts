import { useCallback } from 'react'
import type { WorkflowListResponse } from '../models'
import { workflowAdminService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdminWorkflows(
  search: string,
  includeArchived: boolean,
  page: number,
  pageSize: number,
) {
  const loadWorkflows = useCallback(
    (signal: AbortSignal) =>
      workflowAdminService.getWorkflows(
        { search, includeArchived, page, pageSize },
        signal,
      ),
    [includeArchived, page, pageSize, search],
  )

  return useApiResource<WorkflowListResponse>(
    loadWorkflows,
    'Approval workflows are unavailable. Please try again.',
  )
}
