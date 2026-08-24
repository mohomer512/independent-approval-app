import { useCallback } from 'react'
import type { ApplicationUser } from '../models'
import { adminUserService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdminUser(userId: string) {
  const loadUser = useCallback(
    (signal: AbortSignal) => adminUserService.getUser(userId, signal),
    [userId],
  )

  return useApiResource<ApplicationUser>(
    loadUser,
    'The application user could not be loaded. Please try again.',
  )
}
