import type { RequestPriority } from './approvalRequest'

export type TaskStatus = 'Pending' | 'In Review' | 'Completed' | 'Escalated'

export type TaskAction = 'Review' | 'Approve' | 'Acknowledge'

export interface ApprovalTask {
  readonly id: string
  readonly requestId: string
  readonly requestNumber: string
  readonly title: string
  readonly requesterName: string
  readonly assignedToId: string
  readonly assignedToName: string
  readonly stepName: string
  readonly action: TaskAction
  readonly status: TaskStatus
  readonly priority: RequestPriority
  readonly receivedAt: string
  readonly dueAt: string
}
