export type AdministrationAreaId =
  | 'users'
  | 'roles'
  | 'workflows'
  | 'request-types'
  | 'settings'

export type AdministrationIconName =
  | 'people'
  | 'permissions'
  | 'workflow'
  | 'form'
  | 'settings'

export interface PagedResponse<T> {
  readonly items: readonly T[]
  readonly page: number
  readonly pageSize: number
  readonly totalCount: number
}

export interface AdministrationSummary {
  readonly activeUserCount: number
  readonly activeRoleCount: number
  readonly publishedWorkflowCount: number
  readonly activeRequestTypeCount: number
}

export interface RoleReference {
  readonly id: string
  readonly code: string
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly isArchived: boolean
}

export interface ApplicationUser {
  readonly id: string
  readonly accountName: string
  readonly domain: string
  readonly userName: string
  readonly userPrincipalName: string | null
  readonly email: string | null
  readonly displayName: string
  readonly isActive: boolean
  readonly isLocked: boolean
  readonly lockReason: string | null
  readonly isRemoved: boolean
  readonly isProtectedSystemAdministrator: boolean
  readonly roles: readonly RoleReference[]
  readonly lastSuccessfulAccessAtUtc: string | null
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly lockedAtUtc: string | null
  readonly lockedByAccount: string | null
  readonly removedAtUtc: string | null
  readonly removedByAccount: string | null
  readonly rowVersion: string
}

export interface AdminUserListQuery {
  readonly search?: string
  readonly includeRemoved?: boolean
  readonly page: number
  readonly pageSize: number
}

export interface DirectoryUserSearchResult {
  readonly selectionToken: string
  readonly accountName: string
  readonly domain: string
  readonly userName: string
  readonly userPrincipalName: string | null
  readonly displayName: string | null
  readonly email: string | null
}

export interface DirectoryUserSearchResponse {
  readonly items: readonly DirectoryUserSearchResult[]
  readonly page: number
  readonly pageSize: number
  readonly hasMore: boolean
}

export interface DirectoryUserSearchQuery {
  readonly query: string
  readonly page: number
  readonly pageSize: number
}

export interface AddApplicationUserRequest {
  readonly selectionToken: string
  readonly roleIds: readonly string[]
}

export interface UpdateUserRolesRequest {
  readonly roleIds: readonly string[]
  readonly rowVersion: string
}

export interface LockUserRequest {
  readonly reason: string
  readonly rowVersion: string
}

export interface RowVersionRequest {
  readonly rowVersion: string
}

export interface PermissionDefinition {
  readonly code: string
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string
  readonly descriptionArabic: string
}

export interface ApplicationRole {
  readonly id: string
  readonly code: string
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string | null
  readonly descriptionArabic: string | null
  readonly isActive: boolean
  readonly isArchived: boolean
  readonly permissionCodes: readonly string[]
  readonly userCount: number
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly rowVersion: string
}

export interface AdminRoleListQuery {
  readonly search?: string
  readonly includeArchived?: boolean
  readonly page: number
  readonly pageSize: number
}

export interface CreateRoleRequest {
  readonly code: string
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string
  readonly descriptionArabic: string
  readonly permissionCodes: readonly string[]
}

export interface UpdateRoleRequest {
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string
  readonly descriptionArabic: string
  readonly isActive: boolean
  readonly permissionCodes: readonly string[]
  readonly rowVersion: string
}
