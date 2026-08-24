import { useCallback } from 'react'
import type { PermissionDefinition } from '../models'
import { adminRoleService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdminPermissions() {
  const loadPermissions = useCallback(
    (signal: AbortSignal) => adminRoleService.getPermissions(signal),
    [],
  )

  return useApiResource<readonly PermissionDefinition[]>(
    loadPermissions,
    'The permission catalogue is unavailable. Please try again.',
  )
}
