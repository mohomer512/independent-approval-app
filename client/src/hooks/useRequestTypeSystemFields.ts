import { useCallback } from 'react'
import type { RequestTypeSystemField } from '../models'
import { requestTypeAdminService } from '../services'
import { useApiResource } from './useApiResource'

export function useRequestTypeSystemFields() {
  const loadSystemFields = useCallback(
    (signal: AbortSignal) =>
      requestTypeAdminService.getSystemFields(signal),
    [],
  )

  return useApiResource<readonly RequestTypeSystemField[]>(
    loadSystemFields,
    'Protected system fields are unavailable. Please try again.',
  )
}
