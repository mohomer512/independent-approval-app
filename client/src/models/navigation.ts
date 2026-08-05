export type AppRoute =
  | '/'
  | '/requests'
  | '/requests/new'
  | '/tasks'
  | '/documents'
  | '/administration'

export type NavigationIconName =
  | 'dashboard'
  | 'requests'
  | 'newRequest'
  | 'tasks'
  | 'documents'
  | 'administration'

export interface NavigationItem {
  readonly id: string
  readonly label: string
  readonly path: AppRoute
  readonly icon: NavigationIconName
  readonly end?: boolean
}
