import { useCallback } from 'react'
import type { RequestTypeListResponse } from '../models'
import { requestTypeAdminService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdminRequestTypes(
  search: string,
  includeArchived: boolean,
  page: number,
  pageSize: number,
) {
  const loadRequestTypes = useCallback(
    (signal: AbortSignal) =>
      requestTypeAdminService.getRequestTypes(
        { search, includeArchived, page, pageSize },
        signal,
      ),
    [includeArchived, page, pageSize, search],
  )

  return useApiResource<RequestTypeListResponse>(
    loadRequestTypes,
    'Request types are unavailable. Please try again.',
  )
}
