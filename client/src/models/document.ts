export type DocumentStatus = 'Current' | 'Superseded' | 'Archived'

export type DocumentCategory =
  | 'Policy'
  | 'Contract'
  | 'Invoice'
  | 'Purchase Order'
  | 'Supporting Document'
  | 'Template'

export interface ApprovalDocument {
  readonly id: string
  readonly name: string
  readonly fileExtension: string
  readonly mimeType: string
  readonly sizeBytes: number
  readonly category: DocumentCategory
  readonly version: string
  readonly status: DocumentStatus
  readonly ownerId: string
  readonly ownerName: string
  readonly updatedAt: string
  readonly linkedRequestId: string | null
  readonly linkedRequestNumber: string | null
}
