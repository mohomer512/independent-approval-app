import { useCallback } from 'react'
import type { ApplicationUser, PagedResponse } from '../models'
import { adminUserService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdminUsers(
  search: string,
  includeRemoved: boolean,
  page: number,
  pageSize: number,
) {
  const loadUsers = useCallback(
    (signal: AbortSignal) =>
      adminUserService.getUsers(
        { search, includeRemoved, page, pageSize },
        signal,
      ),
    [includeRemoved, page, pageSize, search],
  )

  return useApiResource<PagedResponse<ApplicationUser>>(
    loadUsers,
    'Application users are unavailable. Please try again.',
  )
}
