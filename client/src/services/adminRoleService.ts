import type {
  AdminRoleListQuery,
  ApplicationRole,
  CreateRoleRequest,
  PagedResponse,
  PermissionDefinition,
  RowVersionRequest,
  UpdateRoleRequest,
} from '../models'
import { apiClient } from './apiClient'

function createRoleListPath(query: AdminRoleListQuery): string {
  const searchParameters = new URLSearchParams({
    includeArchived: String(query.includeArchived ?? false),
    page: String(query.page),
    pageSize: String(query.pageSize),
  })
  const normalizedSearch = query.search?.trim()

  if (normalizedSearch) {
    searchParameters.set('search', normalizedSearch)
  }

  return `/api/admin/roles?${searchParameters}`
}

function createRolePath(id: string, action?: string): string {
  const basePath = `/api/admin/roles/${encodeURIComponent(id)}`
  return action ? `${basePath}/${action}` : basePath
}

export interface AdminRoleService {
  readonly getRoles: (
    query: AdminRoleListQuery,
    signal?: AbortSignal,
  ) => Promise<PagedResponse<ApplicationRole>>
  readonly getRole: (
    id: string,
    signal?: AbortSignal,
  ) => Promise<ApplicationRole>
  readonly getPermissions: (
    signal?: AbortSignal,
  ) => Promise<readonly PermissionDefinition[]>
  readonly createRole: (
    request: CreateRoleRequest,
    signal?: AbortSignal,
  ) => Promise<ApplicationRole>
  readonly updateRole: (
    id: string,
    request: UpdateRoleRequest,
    signal?: AbortSignal,
  ) => Promise<ApplicationRole>
  readonly archiveRole: (
    id: string,
    request: RowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<ApplicationRole>
}

export const adminRoleService: AdminRoleService = {
  getRoles: (query, signal) =>
    apiClient.get<PagedResponse<ApplicationRole>>(createRoleListPath(query), {
      signal,
    }),
  getRole: (id, signal) =>
    apiClient.get<ApplicationRole>(createRolePath(id), { signal }),
  getPermissions: (signal) =>
    apiClient.get<readonly PermissionDefinition[]>('/api/admin/permissions', {
      signal,
    }),
  createRole: (request, signal) =>
    apiClient.post<ApplicationRole>('/api/admin/roles', {
      json: request,
      signal,
    }),
  updateRole: (id, request, signal) =>
    apiClient.put<ApplicationRole>(createRolePath(id), {
      json: request,
      signal,
    }),
  archiveRole: (id, request, signal) =>
    apiClient.post<ApplicationRole>(createRolePath(id, 'archive'), {
      json: request,
      signal,
    }),
}
