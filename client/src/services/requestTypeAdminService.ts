import type {
  AdminRequestTypeListQuery,
  CreateRequestTypeFieldRequest,
  CreateRequestTypeRequest,
  RequestTypeDetail,
  RequestTypeFieldConcurrencyRequest,
  RequestTypeListResponse,
  RequestTypeRowVersionRequest,
  RequestTypeSystemField,
  RequestTypeVersion,
  UpdateRequestTypeFieldRequest,
  UpdateRequestTypeVersionRequest,
} from '../models'
import { apiClient } from './apiClient'

function createListPath(query: AdminRequestTypeListQuery): string {
  const parameters = new URLSearchParams({
    includeArchived: String(query.includeArchived ?? false),
    page: String(query.page),
    pageSize: String(query.pageSize),
  })
  const search = query.search?.trim()

  if (search) {
    parameters.set('search', search)
  }

  return `/api/admin/request-types?${parameters}`
}

function createRequestTypePath(id: string, action?: string): string {
  const path = `/api/admin/request-types/${encodeURIComponent(id)}`
  return action ? `${path}/${action}` : path
}

function createVersionPath(
  requestTypeId: string,
  versionId: string,
  action?: string,
): string {
  const path = `${createRequestTypePath(requestTypeId)}/versions/${encodeURIComponent(versionId)}`
  return action ? `${path}/${action}` : path
}

function createFieldPath(
  requestTypeId: string,
  versionId: string,
  fieldId?: string,
): string {
  const path = createVersionPath(requestTypeId, versionId, 'fields')
  return fieldId ? `${path}/${encodeURIComponent(fieldId)}` : path
}

export interface RequestTypeAdminService {
  readonly getRequestTypes: (
    query: AdminRequestTypeListQuery,
    signal?: AbortSignal,
  ) => Promise<RequestTypeListResponse>
  readonly getRequestType: (
    id: string,
    signal?: AbortSignal,
  ) => Promise<RequestTypeDetail>
  readonly getVersion: (
    requestTypeId: string,
    versionId: string,
    signal?: AbortSignal,
  ) => Promise<RequestTypeVersion>
  readonly getSystemFields: (
    signal?: AbortSignal,
  ) => Promise<readonly RequestTypeSystemField[]>
  readonly createRequestType: (
    request: CreateRequestTypeRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeDetail>
  readonly updateVersion: (
    requestTypeId: string,
    versionId: string,
    request: UpdateRequestTypeVersionRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeVersion>
  readonly addField: (
    requestTypeId: string,
    versionId: string,
    request: CreateRequestTypeFieldRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeVersion>
  readonly updateField: (
    requestTypeId: string,
    versionId: string,
    fieldId: string,
    request: UpdateRequestTypeFieldRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeVersion>
  readonly deleteField: (
    requestTypeId: string,
    versionId: string,
    fieldId: string,
    request: RequestTypeFieldConcurrencyRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeVersion>
  readonly publishVersion: (
    requestTypeId: string,
    versionId: string,
    request: RequestTypeRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeVersion>
  readonly cloneVersion: (
    requestTypeId: string,
    versionId: string,
    request: RequestTypeRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeVersion>
  readonly archiveVersion: (
    requestTypeId: string,
    versionId: string,
    request: RequestTypeRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeVersion>
  readonly archiveRequestType: (
    requestTypeId: string,
    request: RequestTypeRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<RequestTypeDetail>
}

export const requestTypeAdminService: RequestTypeAdminService = {
  getRequestTypes: (query, signal) =>
    apiClient.get<RequestTypeListResponse>(createListPath(query), { signal }),
  getRequestType: (id, signal) =>
    apiClient.get<RequestTypeDetail>(createRequestTypePath(id), { signal }),
  getVersion: (requestTypeId, versionId, signal) =>
    apiClient.get<RequestTypeVersion>(
      createVersionPath(requestTypeId, versionId),
      { signal },
    ),
  getSystemFields: (signal) =>
    apiClient.get<readonly RequestTypeSystemField[]>(
      '/api/admin/request-types/system-fields',
      { signal },
    ),
  createRequestType: (request, signal) =>
    apiClient.post<RequestTypeDetail>('/api/admin/request-types', {
      json: request,
      signal,
    }),
  updateVersion: (requestTypeId, versionId, request, signal) =>
    apiClient.put<RequestTypeVersion>(
      createVersionPath(requestTypeId, versionId),
      { json: request, signal },
    ),
  addField: (requestTypeId, versionId, request, signal) =>
    apiClient.post<RequestTypeVersion>(
      createFieldPath(requestTypeId, versionId),
      { json: request, signal },
    ),
  updateField: (requestTypeId, versionId, fieldId, request, signal) =>
    apiClient.put<RequestTypeVersion>(
      createFieldPath(requestTypeId, versionId, fieldId),
      { json: request, signal },
    ),
  deleteField: (requestTypeId, versionId, fieldId, request, signal) =>
    apiClient.delete<RequestTypeVersion>(
      createFieldPath(requestTypeId, versionId, fieldId),
      { json: request, signal },
    ),
  publishVersion: (requestTypeId, versionId, request, signal) =>
    apiClient.post<RequestTypeVersion>(
      createVersionPath(requestTypeId, versionId, 'publish'),
      { json: request, signal },
    ),
  cloneVersion: (requestTypeId, versionId, request, signal) =>
    apiClient.post<RequestTypeVersion>(
      createVersionPath(requestTypeId, versionId, 'clone'),
      { json: request, signal },
    ),
  archiveVersion: (requestTypeId, versionId, request, signal) =>
    apiClient.post<RequestTypeVersion>(
      createVersionPath(requestTypeId, versionId, 'archive'),
      { json: request, signal },
    ),
  archiveRequestType: (requestTypeId, request, signal) =>
    apiClient.post<RequestTypeDetail>(
      createRequestTypePath(requestTypeId, 'archive'),
      { json: request, signal },
    ),
}
