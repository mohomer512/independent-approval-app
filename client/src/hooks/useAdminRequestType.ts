import { useCallback } from 'react'
import type { RequestTypeDetail } from '../models'
import { requestTypeAdminService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdminRequestType(id: string) {
  const loadRequestType = useCallback(
    (signal: AbortSignal) =>
      requestTypeAdminService.getRequestType(id, signal),
    [id],
  )

  return useApiResource<RequestTypeDetail>(
    loadRequestType,
    'The request type is unavailable. Please try again.',
  )
}
