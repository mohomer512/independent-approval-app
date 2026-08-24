import type {
  WorkflowCatalogueOption,
  WorkflowLifecycle,
} from '../../../models'
import { ApiError, getSafeApiErrorMessage } from '../../../services/apiClient'

export function getWorkflowLifecycleLabel(
  lifecycle: WorkflowLifecycle,
): string {
  if (lifecycle === 'draft') {
    return 'Draft'
  }

  if (lifecycle === 'published') {
    return 'Published'
  }

  return 'Archived'
}

export function getCatalogueLabel<T extends string>(
  options: readonly WorkflowCatalogueOption<T>[],
  value: T,
): string {
  return options.find((option) => option.value === value)?.labelEnglish ?? value
}

export function getWorkflowApiErrorMessage(
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

export function isWorkflowDocumentField(fieldType: string): boolean {
  return fieldType === 'fileDocument' || fieldType === 'richDocument'
}

export function formatWorkflowDate(value: string | null): string {
  if (!value) {
    return 'Not yet'
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}
