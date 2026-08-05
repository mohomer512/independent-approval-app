import type { NavigationItem } from '../models'

export const navigationItems: readonly NavigationItem[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    path: '/',
    icon: 'dashboard',
    end: true,
  },
  {
    id: 'requests',
    label: 'My Requests',
    path: '/requests',
    icon: 'requests',
  },
  {
    id: 'new-request',
    label: 'New Request',
    path: '/requests/new',
    icon: 'newRequest',
  },
  {
    id: 'tasks',
    label: 'My Tasks',
    path: '/tasks',
    icon: 'tasks',
  },
  {
    id: 'documents',
    label: 'Document Library',
    path: '/documents',
    icon: 'documents',
  },
  {
    id: 'administration',
    label: 'Administration',
    path: '/administration',
    icon: 'administration',
  },
]
