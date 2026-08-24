import type {
  AdminUserListQuery,
  ApplicationUser,
  LockUserRequest,
  PagedResponse,
  RowVersionRequest,
  UpdateUserRolesRequest,
} from '../models'
import { apiClient } from './apiClient'

function createUserListPath(query: AdminUserListQuery): string {
  const searchParameters = new URLSearchParams({
    includeRemoved: String(query.includeRemoved ?? false),
    page: String(query.page),
    pageSize: String(query.pageSize),
  })
  const normalizedSearch = query.search?.trim()

  if (normalizedSearch) {
    searchParameters.set('search', normalizedSearch)
  }

  return `/api/admin/users?${searchParameters}`
}

function createUserPath(id: string, action?: string): string {
  const basePath = `/api/admin/users/${encodeURIComponent(id)}`
  return action ? `${basePath}/${action}` : basePath
}

export interface AdminUserService {
  readonly getUsers: (
    query: AdminUserListQuery,
    signal?: AbortSignal,
  ) => Promise<PagedResponse<ApplicationUser>>
  readonly getUser: (
    id: string,
    signal?: AbortSignal,
  ) => Promise<ApplicationUser>
  readonly updateRoles: (
    id: string,
    request: UpdateUserRolesRequest,
    signal?: AbortSignal,
  ) => Promise<ApplicationUser>
  readonly lock: (
    id: string,
    request: LockUserRequest,
    signal?: AbortSignal,
  ) => Promise<ApplicationUser>
  readonly unlock: (
    id: string,
    request: RowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<ApplicationUser>
  readonly remove: (
    id: string,
    request: RowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<ApplicationUser>
  readonly restore: (
    id: string,
    request: RowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<ApplicationUser>
}

export const adminUserService: AdminUserService = {
  getUsers: (query, signal) =>
    apiClient.get<PagedResponse<ApplicationUser>>(createUserListPath(query), {
      signal,
    }),
  getUser: (id, signal) =>
    apiClient.get<ApplicationUser>(createUserPath(id), { signal }),
  updateRoles: (id, request, signal) =>
    apiClient.put<ApplicationUser>(createUserPath(id, 'roles'), {
      json: request,
      signal,
    }),
  lock: (id, request, signal) =>
    apiClient.post<ApplicationUser>(createUserPath(id, 'lock'), {
      json: request,
      signal,
    }),
  unlock: (id, request, signal) =>
    apiClient.post<ApplicationUser>(createUserPath(id, 'unlock'), {
      json: request,
      signal,
    }),
  remove: (id, request, signal) =>
    apiClient.post<ApplicationUser>(createUserPath(id, 'remove'), {
      json: request,
      signal,
    }),
  restore: (id, request, signal) =>
    apiClient.post<ApplicationUser>(createUserPath(id, 'restore'), {
      json: request,
      signal,
    }),
}
