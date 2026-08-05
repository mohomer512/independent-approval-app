import type { DashboardData, DashboardMetric, QuickAction } from '../models'
import { approvalRequests } from './requests'
import { approvalTasks } from './tasks'

export const dashboardMetrics: readonly DashboardMetric[] = [
  {
    id: 'my-requests',
    label: 'My Requests',
    value: 12,
    description: 'Requests submitted by you',
    route: '/requests',
    tone: 'brand',
  },
  {
    id: 'pending-tasks',
    label: 'Pending Tasks',
    value: 4,
    description: 'Tasks waiting for your action',
    route: '/tasks',
    tone: 'warning',
  },
  {
    id: 'documents',
    label: 'Documents',
    value: 28,
    description: 'Documents available to you',
    route: '/documents',
    tone: 'informative',
  },
  {
    id: 'completed',
    label: 'Completed',
    value: 8,
    description: 'Requests approved this period',
    route: '/requests',
    tone: 'success',
  },
]

export const quickActions: readonly QuickAction[] = [
  {
    id: 'create-request',
    label: 'Create a request',
    description: 'Start a new approval request',
    route: '/requests/new',
    icon: 'add',
    appearance: 'primary',
  },
  {
    id: 'review-tasks',
    label: 'Review my tasks',
    description: 'Open approvals assigned to you',
    route: '/tasks',
    icon: 'tasks',
    appearance: 'secondary',
  },
  {
    id: 'browse-documents',
    label: 'Browse documents',
    description: 'Find request files and policies',
    route: '/documents',
    icon: 'documents',
    appearance: 'secondary',
  },
]

export const dashboardData: DashboardData = {
  metrics: dashboardMetrics,
  recentRequests: approvalRequests.slice(0, 5),
  pendingTasks: approvalTasks,
  quickActions,
}
