import type {
  RequestDocumentMode,
  RequestFieldType,
  RequestTypeLifecycle,
} from '../../../models'
import { ApiError, getSafeApiErrorMessage } from '../../../services/apiClient'

export interface SelectOption<T extends string> {
  readonly value: T
  readonly label: string
}

export const requestFieldTypeOptions: readonly SelectOption<RequestFieldType>[] = [
  { value: 'shortText', label: 'Short text' },
  { value: 'longText', label: 'Long text' },
  { value: 'integer', label: 'Integer' },
  { value: 'decimal', label: 'Decimal' },
  { value: 'date', label: 'Date' },
  { value: 'dateTime', label: 'Date and time' },
  { value: 'yesNo', label: 'Yes / No' },
  { value: 'singleChoice', label: 'Single choice' },
  { value: 'multipleChoice', label: 'Multiple choice' },
  { value: 'activeDirectoryUser', label: 'Active Directory user' },
  { value: 'applicationRole', label: 'Application role' },
  { value: 'fileDocument', label: 'File / document' },
  { value: 'richDocument', label: 'Rich document' },
]

export const documentModeOptions: readonly SelectOption<RequestDocumentMode>[] = [
  { value: 'uploadOnly', label: 'Upload only' },
  { value: 'createInEditorOnly', label: 'Create in editor only' },
  { value: 'uploadOrCreate', label: 'Upload or create' },
]

export function getFieldTypeLabel(fieldType: RequestFieldType): string {
  return (
    requestFieldTypeOptions.find((option) => option.value === fieldType)?.label ??
    fieldType
  )
}

export function getDocumentModeLabel(
  documentMode: RequestDocumentMode | null,
): string | null {
  if (!documentMode) {
    return null
  }

  return (
    documentModeOptions.find((option) => option.value === documentMode)?.label ??
    documentMode
  )
}

export function getLifecycleLabel(lifecycle: RequestTypeLifecycle): string {
  if (lifecycle === 'draft') {
    return 'Draft'
  }

  if (lifecycle === 'published') {
    return 'Published'
  }

  return 'Archived'
}

export function isChoiceField(fieldType: RequestFieldType): boolean {
  return fieldType === 'singleChoice' || fieldType === 'multipleChoice'
}

export function isDocumentField(fieldType: RequestFieldType): boolean {
  return fieldType === 'fileDocument' || fieldType === 'richDocument'
}

export function formatUtcDate(value: string | null): string {
  if (!value) {
    return 'Not yet'
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}

export function getRequestTypeApiErrorMessage(
  error: unknown,
  fallback: string,
): string {
  if (error instanceof ApiError) {
    const validationMessage = Object.values(error.errors)
      .flat()
      .find((message) => message.trim().length > 0)

    if (validationMessage) {
      return validationMessage
    }
  }

  return getSafeApiErrorMessage(error, fallback)
}
