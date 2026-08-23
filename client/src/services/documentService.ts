import type { DocumentListResponse } from '../models'
import { apiClient } from './apiClient'

export interface DocumentService {
  readonly getDocuments: (
    page: number,
    pageSize: number,
    signal?: AbortSignal,
  ) => Promise<DocumentListResponse>
  readonly downloadDocument: (
    downloadUrl: string,
    signal?: AbortSignal,
  ) => Promise<Blob>
}

export const documentService: DocumentService = {
  getDocuments: (page, pageSize, signal) => {
    const query = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    })

    return apiClient.get<DocumentListResponse>(`/api/documents?${query}`, {
      signal,
    })
  },
  downloadDocument: (downloadUrl, signal) =>
    apiClient.getBlob(downloadUrl, { signal }),
}
