import type { PagedResponse } from './administration'

export type RequestTypeLifecycle = 'draft' | 'published' | 'archived'

export type RequestFieldType =
  | 'shortText'
  | 'longText'
  | 'integer'
  | 'decimal'
  | 'date'
  | 'dateTime'
  | 'yesNo'
  | 'singleChoice'
  | 'multipleChoice'
  | 'activeDirectoryUser'
  | 'applicationRole'
  | 'fileDocument'
  | 'richDocument'

export type RequestDocumentMode =
  | 'uploadOnly'
  | 'createInEditorOnly'
  | 'uploadOrCreate'

export type RequestFieldDefaultValue =
  | string
  | number
  | boolean
  | readonly string[]
  | null

export interface RequestFieldValidationConfig {
  readonly minLength?: number
  readonly maxLength?: number
  readonly pattern?: string
  readonly minimum?: number | string
  readonly maximum?: number | string
  readonly decimalPlaces?: number
  readonly minimumSelections?: number
  readonly maximumSelections?: number
  readonly maximumDocuments?: number
  readonly allowedExtensions?: readonly string[]
}

export interface RequestFieldChoiceOption {
  readonly value: string
  readonly labelEnglish: string
  readonly labelArabic: string
  readonly sortOrder: number
  readonly isActive: boolean
}

export interface RequestFieldChoiceConfig {
  readonly options: readonly RequestFieldChoiceOption[]
}

export interface RequestTypeField {
  readonly id: string
  readonly key: string
  readonly labelEnglish: string
  readonly labelArabic: string
  readonly helpTextEnglish: string | null
  readonly helpTextArabic: string | null
  readonly fieldType: RequestFieldType
  readonly isRequired: boolean
  readonly defaultValue: RequestFieldDefaultValue
  readonly validationConfig: RequestFieldValidationConfig | null
  readonly choiceConfig: RequestFieldChoiceConfig | null
  readonly sortOrder: number
  readonly isActive: boolean
  readonly documentMode: RequestDocumentMode | null
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly rowVersion: string
}

export interface RequestTypeVersionSummary {
  readonly id: string
  readonly versionNumber: number
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string
  readonly descriptionArabic: string
  readonly requestPrefix: string
  readonly navigationSlug: string
  readonly navigationOrder: number
  readonly lifecycle: RequestTypeLifecycle
  readonly fieldCount: number
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

export interface RequestTypeVersion
  extends Omit<RequestTypeVersionSummary, 'fieldCount'> {
  readonly fields: readonly RequestTypeField[]
}

export interface RequestTypeListItem {
  readonly id: string
  readonly code: string
  readonly isArchived: boolean
  readonly displayVersion: RequestTypeVersionSummary | null
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

export interface RequestTypeDetail {
  readonly id: string
  readonly code: string
  readonly isArchived: boolean
  readonly versions: readonly RequestTypeVersion[]
  readonly createdAtUtc: string
  readonly createdByAccount: string
  readonly modifiedAtUtc: string | null
  readonly modifiedByAccount: string | null
  readonly archivedAtUtc: string | null
  readonly archivedByAccount: string | null
  readonly rowVersion: string
}

export interface RequestTypeSystemField {
  readonly key: string
  readonly labelEnglish: string
  readonly labelArabic: string
  readonly dataType: string
  readonly isAlwaysVisible: boolean
  readonly isEditable: boolean
  readonly isRemovable: boolean
}

export interface AdminRequestTypeListQuery {
  readonly search?: string
  readonly includeArchived?: boolean
  readonly page: number
  readonly pageSize: number
}

export type RequestTypeListResponse = PagedResponse<RequestTypeListItem>

export interface CreateRequestTypeRequest {
  readonly code: string
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string
  readonly descriptionArabic: string
  readonly requestPrefix: string
  readonly navigationSlug: string
  readonly navigationOrder: number
}

export interface UpdateRequestTypeVersionRequest {
  readonly nameEnglish: string
  readonly nameArabic: string
  readonly descriptionEnglish: string
  readonly descriptionArabic: string
  readonly requestPrefix: string
  readonly navigationSlug: string
  readonly navigationOrder: number
  readonly rowVersion: string
}

export interface RequestTypeFieldValues {
  readonly labelEnglish: string
  readonly labelArabic: string
  readonly helpTextEnglish: string
  readonly helpTextArabic: string
  readonly fieldType: RequestFieldType
  readonly isRequired: boolean
  readonly defaultValue: RequestFieldDefaultValue
  readonly validationConfig: RequestFieldValidationConfig | null
  readonly choiceConfig: RequestFieldChoiceConfig | null
  readonly sortOrder: number
  readonly isActive: boolean
  readonly documentMode: RequestDocumentMode | null
}

export interface CreateRequestTypeFieldRequest extends RequestTypeFieldValues {
  readonly key: string
  readonly versionRowVersion: string
}

export interface UpdateRequestTypeFieldRequest extends RequestTypeFieldValues {
  readonly versionRowVersion: string
  readonly fieldRowVersion: string
}

export interface RequestTypeRowVersionRequest {
  readonly rowVersion: string
}

export interface RequestTypeFieldConcurrencyRequest {
  readonly versionRowVersion: string
  readonly fieldRowVersion: string
}
