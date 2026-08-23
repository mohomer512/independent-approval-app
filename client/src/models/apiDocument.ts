export interface ApiDocument {
  readonly id: string
  readonly originalFileName: string
  readonly contentType: string
  readonly fileSize: number
  readonly description: string | null
  readonly status: string
  readonly uploadedBy: string
  readonly uploadedAtUtc: string
  readonly downloadUrl: string
  readonly rowVersion: string
}

export interface DocumentListResponse {
  readonly items: readonly ApiDocument[]
  readonly page: number
  readonly pageSize: number
  readonly totalCount: number
}
