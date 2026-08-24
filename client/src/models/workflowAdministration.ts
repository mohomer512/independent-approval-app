import type { PagedResponse } from './administration'

export type WorkflowLifecycle = 'draft' | 'published' | 'archived'

export type WorkflowActionType =
  | 'submit'
  | 'approve'
  | 'reject'
  | 'putOnHold'
  | 'resume'
  | 'requestMoreInformation'
  | 'return'
  | 'forward'
  | 'complete'

export type WorkflowCommentPolicy = 'none' | 'optional' | 'required'

export type WorkflowFieldAccess =
  | 'hidden'
  | 'readOnly'
  | 'editable'
  | 'editableRequired'

export type WorkflowDocumentAccess = 'hidden' | 'view' | 'edit'

export type WorkflowTerminalOutcome = 'approved' | 'rejected' | 'completed'

export type WorkflowResultingStatus =
  | 'draft'
  | 'submitted'
  | 'inProgress'
  | 'onHold'
  | 'moreInformationRequired'
  | 'approved'
  | 'rejected'
  | 'completed'
  | 'cancelled'

export interface WorkflowCatalogueOption<T extends string = string> {
  readonly value: T
  readonly labelEnglish: string
  readonly labelArabic: string
}

export interface WorkflowRoleOption {
  readonly id: string
  readonly code: string
  readonly nameEnglish: string
  readonly nameArabic: string
}

export interface WorkflowRequestFieldOption {
  readonly id: string
  readonly key: string
  readonly labelEnglish: string
  readonly labelArabic: string
  readonly helpTextEnglish: string | null
  readonly helpTextArabic: string | null
  readonly fieldType: string
  readonly isRequired: boolean
  readonly isActive: boolean
  readonly documentMode: string | null
}

export interface WorkflowRequestTypeVersionOption {
  readonly requestTypeId: string
  readonly requestTypeCode: string
  readonly requestTypeVersionId: string
  readonly versionNumber: number
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly fields: readonly WorkflowRequestFieldOption[]
}

export interface WorkflowSystemFieldOption {
  readonly key: string
  readonly labelEnglish: string
  readonly labelArabic: string
  readonly dataType: string
  readonly access: WorkflowFieldAccess
  readonly documentAccess: WorkflowDocumentAccess | null
  readonly canSelectForForwarding: boolean
}

export interface WorkflowFieldPermission {
  readonly id: string
  readonly requestFieldDefinitionId: string
  readonly fieldKey: string
  readonly fieldType: string
  readonly access: WorkflowFieldAccess
  readonly documentAccess: WorkflowDocumentAccess | null
  readonly canSelectForForwarding: boolean
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly rowVersion: string
}

export interface WorkflowStep {
  readonly id: string
  readonly key: string
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly sortOrder: number
  readonly diagramX: number
  readonly diagramY: number
  readonly isStartStep: boolean
  readonly isActive: boolean
  readonly canEditRequestData: boolean
  readonly canOpenDocuments: boolean
  readonly canEditDocuments: boolean
  readonly canForwardDocuments: boolean
  readonly commentPolicy: WorkflowCommentPolicy
  readonly roles: readonly WorkflowRoleOption[]
  readonly fieldPermissions: readonly WorkflowFieldPermission[]
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly rowVersion: string
}

export interface WorkflowTransition {
  readonly id: string
  readonly key: string
  readonly sourceStepId: string
  readonly targetStepId: string | null
  readonly actionLabelEnglish: string
  readonly actionLabelArabic: string
  readonly actionType: WorkflowActionType
  readonly resultingStatus: WorkflowResultingStatus
  readonly requiresComment: boolean
  readonly terminalOutcome: WorkflowTerminalOutcome | null
  readonly sortOrder: number
  readonly isActive: boolean
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly rowVersion: string
}

export interface WorkflowVersionSummary {
  readonly id: string
  readonly versionNumber: number
  readonly requestTypeId: string
  readonly requestTypeVersionId: string
  readonly requestTypeCode: string
  readonly requestTypeVersionNumber: number
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string
  readonly descriptionArabic: string
  readonly navigationLabelEnglish: string
  readonly navigationLabelArabic: string
  readonly navigationSlug: string
  readonly navigationOrder: number
  readonly lifecycle: WorkflowLifecycle
  readonly starterRoleCount: number
  readonly activeStepCount: number
  readonly activeTransitionCount: number
  readonly isInUse: boolean
  readonly requestCount: number
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly publishedAtUtc: string | null
  readonly publishedByAccount: string | null
  readonly archivedAtUtc: string | null
  readonly archivedByAccount: string | null
  readonly rowVersion: string
}

export interface WorkflowVersion
  extends Omit<
    WorkflowVersionSummary,
    'starterRoleCount' | 'activeStepCount' | 'activeTransitionCount'
  > {
  readonly requestFields: readonly WorkflowRequestFieldOption[]
  readonly starterRoles: readonly WorkflowRoleOption[]
  readonly steps: readonly WorkflowStep[]
  readonly transitions: readonly WorkflowTransition[]
}

export interface WorkflowListItem {
  readonly id: string
  readonly code: string
  readonly isArchived: boolean
  readonly displayVersion: WorkflowVersionSummary | null
  readonly draftVersionId: string | null
  readonly latestPublishedVersionId: string | null
  readonly versionCount: number
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly archivedAtUtc: string | null
  readonly archivedByAccount: string | null
  readonly rowVersion: string
}

export interface WorkflowDetail {
  readonly id: string
  readonly code: string
  readonly isArchived: boolean
  readonly versions: readonly WorkflowVersion[]
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly archivedAtUtc: string | null
  readonly archivedByAccount: string | null
  readonly rowVersion: string
}

export interface WorkflowOptions {
  readonly roles: readonly WorkflowRoleOption[]
  readonly requestTypeVersions: readonly WorkflowRequestTypeVersionOption[]
  readonly systemFields: readonly WorkflowSystemFieldOption[]
  readonly actionTypes: readonly WorkflowCatalogueOption<WorkflowActionType>[]
  readonly resultingStatuses: readonly WorkflowCatalogueOption<WorkflowResultingStatus>[]
  readonly commentPolicies: readonly WorkflowCatalogueOption<WorkflowCommentPolicy>[]
  readonly fieldAccessModes: readonly WorkflowCatalogueOption<WorkflowFieldAccess>[]
  readonly documentAccessModes: readonly WorkflowCatalogueOption<WorkflowDocumentAccess>[]
  readonly terminalOutcomes: readonly WorkflowCatalogueOption<WorkflowTerminalOutcome>[]
}

export interface AdminWorkflowListQuery {
  readonly search?: string
  readonly includeArchived?: boolean
  readonly page: number
  readonly pageSize: number
}

export type WorkflowListResponse = PagedResponse<WorkflowListItem>

export interface CreateWorkflowRequest {
  readonly code: string
  readonly requestTypeVersionId: string
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string
  readonly descriptionArabic: string
  readonly navigationLabelEnglish: string
  readonly navigationLabelArabic: string
  readonly navigationSlug: string
  readonly navigationOrder: number
}

export interface UpdateWorkflowVersionRequest
  extends Omit<CreateWorkflowRequest, 'code'> {
  readonly rowVersion: string
}

export interface UpdateWorkflowStarterRolesRequest {
  readonly roleIds: readonly string[]
  readonly versionRowVersion: string
}

export interface WorkflowStepValues {
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly sortOrder: number
  readonly diagramX: number
  readonly diagramY: number
  readonly isStartStep: boolean
  readonly isActive: boolean
  readonly canEditRequestData: boolean
  readonly canOpenDocuments: boolean
  readonly canEditDocuments: boolean
  readonly canForwardDocuments: boolean
  readonly commentPolicy: WorkflowCommentPolicy
}

export interface CreateWorkflowStepRequest extends WorkflowStepValues {
  readonly key: string
  readonly roleIds: readonly string[]
  readonly versionRowVersion: string
}

export interface UpdateWorkflowStepRequest extends WorkflowStepValues {
  readonly versionRowVersion: string
  readonly stepRowVersion: string
}

export interface UpdateWorkflowStepRolesRequest {
  readonly roleIds: readonly string[]
  readonly versionRowVersion: string
  readonly stepRowVersion: string
}

export interface WorkflowStepConcurrencyRequest {
  readonly versionRowVersion: string
  readonly stepRowVersion: string
}

export interface WorkflowTransitionValues {
  readonly sourceStepId: string
  readonly targetStepId: string | null
  readonly actionLabelEnglish: string
  readonly actionLabelArabic: string
  readonly actionType: WorkflowActionType
  readonly resultingStatus: WorkflowResultingStatus
  readonly requiresComment: boolean
  readonly terminalOutcome: WorkflowTerminalOutcome | null
  readonly sortOrder: number
  readonly isActive: boolean
}

export interface CreateWorkflowTransitionRequest
  extends WorkflowTransitionValues {
  readonly key: string
  readonly versionRowVersion: string
}

export interface UpdateWorkflowTransitionRequest
  extends WorkflowTransitionValues {
  readonly versionRowVersion: string
  readonly transitionRowVersion: string
}

export interface WorkflowTransitionConcurrencyRequest {
  readonly versionRowVersion: string
  readonly transitionRowVersion: string
}

export interface WorkflowFieldPermissionValue {
  readonly requestFieldDefinitionId: string
  readonly access: WorkflowFieldAccess
  readonly documentAccess: WorkflowDocumentAccess | null
  readonly canSelectForForwarding: boolean
  readonly rowVersion: string | null
}

export interface UpdateWorkflowFieldPermissionsRequest {
  readonly permissions: readonly WorkflowFieldPermissionValue[]
  readonly versionRowVersion: string
  readonly stepRowVersion: string
}

export interface WorkflowRowVersionRequest {
  readonly rowVersion: string
}

export interface WorkflowVersionRowVersionRequest {
  readonly versionRowVersion: string
}

export interface WorkflowValidationIssue {
  readonly code: string
  readonly path: string
  readonly message: string
}

export interface WorkflowValidationResult {
  readonly isValid: boolean
  readonly issues: readonly WorkflowValidationIssue[]
}
