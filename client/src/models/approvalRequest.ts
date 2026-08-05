export type RequestStatus =
  | 'Draft'
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'Returned'
  | 'Cancelled'

export type RequestPriority = 'Low' | 'Normal' | 'High' | 'Urgent'

export type RequestCategory =
  | 'Procurement'
  | 'Finance'
  | 'Human Resources'
  | 'Information Technology'
  | 'Facilities'
  | 'Legal'
  | 'General'

export interface ApprovalRequest {
  readonly id: string
  readonly requestNumber: string
  readonly title: string
  readonly description: string
  readonly category: RequestCategory
  readonly status: RequestStatus
  readonly priority: RequestPriority
  readonly requesterId: string
  readonly requesterName: string
  readonly submittedAt: string | null
  readonly updatedAt: string
  readonly currentStep: string
  readonly currentApprover: string | null
  readonly amount: number | null
  readonly currency: string | null
}
