export interface CurrentUser {
  readonly isAuthenticated: boolean
  readonly accountName: string
  readonly domain: string
  readonly userName: string
  readonly authenticationType: string
}
