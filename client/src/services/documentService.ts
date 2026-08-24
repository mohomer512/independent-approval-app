import type { ApiDocument, DocumentListResponse } from '../models'
import { apiClient } from './apiClient'

export interface DocumentService {
  readonly getDocuments: (
    page: number,
    pageSize: number,
    signal?: AbortSignal,
  ) => Promise<DocumentListResponse>
  readonly uploadDocument: (
    file: File,
    description: string,
    signal?: AbortSignal,
  ) => Promise<ApiDocument>
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
  uploadDocument: (file, description, signal) => {
    const formData = new FormData()
    const normalizedDescription = description.trim()
    formData.append('File', file, file.name)

    if (normalizedDescription.length > 0) {
      formData.append('Description', normalizedDescription)
    }

    return apiClient.post<ApiDocument>('/api/documents', {
      body: formData,
      signal,
    })
  },
  downloadDocument: (downloadUrl, signal) =>
    apiClient.getBlob(downloadUrl, { signal }),
}
