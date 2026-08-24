import type { FluentIcon } from '@fluentui/react-icons'
import {
  AddSquare24Regular,
  ClipboardTaskListLtr24Regular,
  DocumentFolder24Regular,
  Home24Regular,
  Settings24Regular,
  TaskListSquareLtr24Regular,
} from '@fluentui/react-icons'

export interface NavigationItem {
  readonly label: string
  readonly path: string
  readonly icon: FluentIcon
  readonly end?: boolean
  readonly systemAdministratorOnly?: boolean
}

export const navigationItems: readonly NavigationItem[] = [
  { label: 'Dashboard', path: '/', icon: Home24Regular, end: true },
  {
    label: 'My Requests',
    path: '/requests',
    icon: ClipboardTaskListLtr24Regular,
    end: true,
  },
  {
    label: 'New Request',
    path: '/requests/new',
    icon: AddSquare24Regular,
  },
  { label: 'My Tasks', path: '/tasks', icon: TaskListSquareLtr24Regular },
  {
    label: 'Document Library',
    path: '/documents',
    icon: DocumentFolder24Regular,
  },
  {
    label: 'Administration',
    path: '/administration',
    icon: Settings24Regular,
    systemAdministratorOnly: true,
  },
]

export const getPageTitle = (pathname: string): string => {
  if (pathname === '/requests/new') {
    return 'New Request'
  }

  if (pathname.startsWith('/administration/users')) {
    return 'Users'
  }

  if (pathname.startsWith('/administration/roles')) {
    return 'Roles and permissions'
  }

  if (pathname.startsWith('/administration/request-types')) {
    return 'Request types'
  }

  if (pathname.startsWith('/administration/workflows')) {
    return 'Approval workflows'
  }

  if (pathname.startsWith('/administration/settings')) {
    return 'System settings'
  }

  const matchingItem = navigationItems.find((item) => item.path === pathname)
  return matchingItem?.label ?? 'Independent Approval'
}
