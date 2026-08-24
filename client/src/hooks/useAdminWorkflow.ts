import { useCallback } from 'react'
import type { WorkflowDetail } from '../models'
import { workflowAdminService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdminWorkflow(id: string) {
  const loadWorkflow = useCallback(
    (signal: AbortSignal) => workflowAdminService.getWorkflow(id, signal),
    [id],
  )

  return useApiResource<WorkflowDetail>(
    loadWorkflow,
    'The workflow definition is unavailable. Please try again.',
  )
}
