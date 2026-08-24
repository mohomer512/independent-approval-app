export type ApplicationAccessState =
  | 'granted'
  | 'unknown'
  | 'inactive'
  | 'locked'
  | 'removed'
  | 'missingAccessPermission'

export type ApplicationLanguage = 'en' | 'ar'
export type ApplicationTheme = 'light' | 'dark'

export interface CurrentUser {
  readonly isAuthenticated: boolean
  readonly accountName: string
  readonly domain: string
  readonly userName: string
  readonly authenticationType: string
  readonly displayName: string
  readonly applicationUserId: string | null
  readonly accessState: ApplicationAccessState
  readonly hasApplicationAccess: boolean
  readonly isSystemAdministrator: boolean
  readonly roleCodes: readonly string[]
  readonly permissions: readonly string[]
  readonly language: ApplicationLanguage
  readonly theme: ApplicationTheme
}
