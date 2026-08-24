import type {
  AdminWorkflowListQuery,
  CreateWorkflowRequest,
  CreateWorkflowStepRequest,
  CreateWorkflowTransitionRequest,
  UpdateWorkflowFieldPermissionsRequest,
  UpdateWorkflowStarterRolesRequest,
  UpdateWorkflowStepRequest,
  UpdateWorkflowStepRolesRequest,
  UpdateWorkflowTransitionRequest,
  UpdateWorkflowVersionRequest,
  WorkflowDetail,
  WorkflowListResponse,
  WorkflowOptions,
  WorkflowRowVersionRequest,
  WorkflowStepConcurrencyRequest,
  WorkflowTransitionConcurrencyRequest,
  WorkflowValidationResult,
  WorkflowVersion,
  WorkflowVersionRowVersionRequest,
} from '../models'
import { apiClient } from './apiClient'

function createListPath(query: AdminWorkflowListQuery): string {
  const parameters = new URLSearchParams({
    includeArchived: String(query.includeArchived ?? false),
    page: String(query.page),
    pageSize: String(query.pageSize),
  })
  const search = query.search?.trim()

  if (search) {
    parameters.set('search', search)
  }

  return `/api/admin/workflows?${parameters}`
}

function createWorkflowPath(id: string, action?: string): string {
  const path = `/api/admin/workflows/${encodeURIComponent(id)}`
  return action ? `${path}/${action}` : path
}

function createVersionPath(
  workflowId: string,
  versionId: string,
  action?: string,
): string {
  const path = `${createWorkflowPath(workflowId)}/versions/${encodeURIComponent(versionId)}`
  return action ? `${path}/${action}` : path
}

function createStepPath(
  workflowId: string,
  versionId: string,
  stepId?: string,
  action?: string,
): string {
  const stepsPath = createVersionPath(workflowId, versionId, 'steps')
  const path = stepId ? `${stepsPath}/${encodeURIComponent(stepId)}` : stepsPath
  return action ? `${path}/${action}` : path
}

function createTransitionPath(
  workflowId: string,
  versionId: string,
  transitionId?: string,
): string {
  const path = createVersionPath(workflowId, versionId, 'transitions')
  return transitionId ? `${path}/${encodeURIComponent(transitionId)}` : path
}

export interface WorkflowAdminService {
  readonly getWorkflows: (
    query: AdminWorkflowListQuery,
    signal?: AbortSignal,
  ) => Promise<WorkflowListResponse>
  readonly getWorkflow: (
    id: string,
    signal?: AbortSignal,
  ) => Promise<WorkflowDetail>
  readonly getVersion: (
    workflowId: string,
    versionId: string,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly getOptions: (signal?: AbortSignal) => Promise<WorkflowOptions>
  readonly createWorkflow: (
    request: CreateWorkflowRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowDetail>
  readonly updateVersion: (
    workflowId: string,
    versionId: string,
    request: UpdateWorkflowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly updateStarterRoles: (
    workflowId: string,
    versionId: string,
    request: UpdateWorkflowStarterRolesRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly addStep: (
    workflowId: string,
    versionId: string,
    request: CreateWorkflowStepRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly updateStep: (
    workflowId: string,
    versionId: string,
    stepId: string,
    request: UpdateWorkflowStepRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly deleteStep: (
    workflowId: string,
    versionId: string,
    stepId: string,
    request: WorkflowStepConcurrencyRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly updateStepRoles: (
    workflowId: string,
    versionId: string,
    stepId: string,
    request: UpdateWorkflowStepRolesRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly updateFieldPermissions: (
    workflowId: string,
    versionId: string,
    stepId: string,
    request: UpdateWorkflowFieldPermissionsRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly addTransition: (
    workflowId: string,
    versionId: string,
    request: CreateWorkflowTransitionRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly updateTransition: (
    workflowId: string,
    versionId: string,
    transitionId: string,
    request: UpdateWorkflowTransitionRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly deleteTransition: (
    workflowId: string,
    versionId: string,
    transitionId: string,
    request: WorkflowTransitionConcurrencyRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly validateVersion: (
    workflowId: string,
    versionId: string,
    request: WorkflowVersionRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowValidationResult>
  readonly publishVersion: (
    workflowId: string,
    versionId: string,
    request: WorkflowRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly cloneVersion: (
    workflowId: string,
    versionId: string,
    request: WorkflowRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly archiveVersion: (
    workflowId: string,
    versionId: string,
    request: WorkflowRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowVersion>
  readonly archiveWorkflow: (
    workflowId: string,
    request: WorkflowRowVersionRequest,
    signal?: AbortSignal,
  ) => Promise<WorkflowDetail>
}

export const workflowAdminService: WorkflowAdminService = {
  getWorkflows: (query, signal) =>
    apiClient.get<WorkflowListResponse>(createListPath(query), { signal }),
  getWorkflow: (id, signal) =>
    apiClient.get<WorkflowDetail>(createWorkflowPath(id), { signal }),
  getVersion: (workflowId, versionId, signal) =>
    apiClient.get<WorkflowVersion>(createVersionPath(workflowId, versionId), {
      signal,
    }),
  getOptions: (signal) =>
    apiClient.get<WorkflowOptions>('/api/admin/workflows/options', { signal }),
  createWorkflow: (request, signal) =>
    apiClient.post<WorkflowDetail>('/api/admin/workflows', {
      json: request,
      signal,
    }),
  updateVersion: (workflowId, versionId, request, signal) =>
    apiClient.put<WorkflowVersion>(
      createVersionPath(workflowId, versionId),
      { json: request, signal },
    ),
  updateStarterRoles: (workflowId, versionId, request, signal) =>
    apiClient.put<WorkflowVersion>(
      createVersionPath(workflowId, versionId, 'starter-roles'),
      { json: request, signal },
    ),
  addStep: (workflowId, versionId, request, signal) =>
    apiClient.post<WorkflowVersion>(createStepPath(workflowId, versionId), {
      json: request,
      signal,
    }),
  updateStep: (workflowId, versionId, stepId, request, signal) =>
    apiClient.put<WorkflowVersion>(
      createStepPath(workflowId, versionId, stepId),
      { json: request, signal },
    ),
  deleteStep: (workflowId, versionId, stepId, request, signal) =>
    apiClient.delete<WorkflowVersion>(
      createStepPath(workflowId, versionId, stepId),
      { json: request, signal },
    ),
  updateStepRoles: (workflowId, versionId, stepId, request, signal) =>
    apiClient.put<WorkflowVersion>(
      createStepPath(workflowId, versionId, stepId, 'roles'),
      { json: request, signal },
    ),
  updateFieldPermissions: (workflowId, versionId, stepId, request, signal) =>
    apiClient.put<WorkflowVersion>(
      createStepPath(workflowId, versionId, stepId, 'field-permissions'),
      { json: request, signal },
    ),
  addTransition: (workflowId, versionId, request, signal) =>
    apiClient.post<WorkflowVersion>(
      createTransitionPath(workflowId, versionId),
      { json: request, signal },
    ),
  updateTransition: (
    workflowId,
    versionId,
    transitionId,
    request,
    signal,
  ) =>
    apiClient.put<WorkflowVersion>(
      createTransitionPath(workflowId, versionId, transitionId),
      { json: request, signal },
    ),
  deleteTransition: (
    workflowId,
    versionId,
    transitionId,
    request,
    signal,
  ) =>
    apiClient.delete<WorkflowVersion>(
      createTransitionPath(workflowId, versionId, transitionId),
      { json: request, signal },
    ),
  validateVersion: (workflowId, versionId, request, signal) =>
    apiClient.post<WorkflowValidationResult>(
      createVersionPath(workflowId, versionId, 'validate'),
      { json: request, signal },
    ),
  publishVersion: (workflowId, versionId, request, signal) =>
    apiClient.post<WorkflowVersion>(
      createVersionPath(workflowId, versionId, 'publish'),
      { json: request, signal },
    ),
  cloneVersion: (workflowId, versionId, request, signal) =>
    apiClient.post<WorkflowVersion>(
      createVersionPath(workflowId, versionId, 'clone'),
      { json: request, signal },
    ),
  archiveVersion: (workflowId, versionId, request, signal) =>
    apiClient.post<WorkflowVersion>(
      createVersionPath(workflowId, versionId, 'archive'),
      { json: request, signal },
    ),
  archiveWorkflow: (workflowId, request, signal) =>
    apiClient.post<WorkflowDetail>(createWorkflowPath(workflowId, 'archive'), {
      json: request,
      signal,
    }),
}
