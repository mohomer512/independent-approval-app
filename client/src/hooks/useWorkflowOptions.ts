import { useCallback } from 'react'
import type { WorkflowOptions } from '../models'
import { workflowAdminService } from '../services'
import { useApiResource } from './useApiResource'

export function useWorkflowOptions() {
  const loadOptions = useCallback(
    (signal: AbortSignal) => workflowAdminService.getOptions(signal),
    [],
  )

  return useApiResource<WorkflowOptions>(
    loadOptions,
    'Workflow designer options are unavailable. Please try again.',
  )
}
