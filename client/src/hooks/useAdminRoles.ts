import { useCallback } from 'react'
import type { ApplicationRole, PagedResponse } from '../models'
import { adminRoleService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdminRoles(
  search: string,
  includeArchived: boolean,
  page: number,
  pageSize: number,
) {
  const loadRoles = useCallback(
    (signal: AbortSignal) =>
      adminRoleService.getRoles(
        { search, includeArchived, page, pageSize },
        signal,
      ),
    [includeArchived, page, pageSize, search],
  )

  return useApiResource<PagedResponse<ApplicationRole>>(
    loadRoles,
    'Application roles are unavailable. Please try again.',
  )
}
