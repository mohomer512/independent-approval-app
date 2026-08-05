import type { ApprovalRequest } from './approvalRequest'
import type { ApprovalTask } from './approvalTask'
import type { AppRoute } from './navigation'

export type DashboardMetricId =
  | 'my-requests'
  | 'pending-tasks'
  | 'documents'
  | 'completed'

export type DashboardMetricTone = 'brand' | 'warning' | 'informative' | 'success'

export interface DashboardMetric {
  readonly id: DashboardMetricId
  readonly label: string
  readonly value: number
  readonly description: string
  readonly route: AppRoute
  readonly tone: DashboardMetricTone
}

export type QuickActionIconName = 'add' | 'tasks' | 'documents'

export interface QuickAction {
  readonly id: string
  readonly label: string
  readonly description: string
  readonly route: AppRoute
  readonly icon: QuickActionIconName
  readonly appearance: 'primary' | 'secondary'
}

export interface DashboardData {
  readonly metrics: readonly DashboardMetric[]
  readonly recentRequests: readonly ApprovalRequest[]
  readonly pendingTasks: readonly ApprovalTask[]
  readonly quickActions: readonly QuickAction[]
}
