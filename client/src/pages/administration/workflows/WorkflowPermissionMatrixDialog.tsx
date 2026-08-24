import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  MessageBar,
  MessageBarBody,
  Spinner,
} from '@fluentui/react-components'
import { useEffect, useMemo, useRef, useState } from 'react'
import type {
  WorkflowCatalogueOption,
  WorkflowDocumentAccess,
  WorkflowFieldAccess,
  WorkflowFieldPermissionValue,
  WorkflowRequestFieldOption,
  WorkflowStep,
  WorkflowVersion,
} from '../../../models'
import { ApiError, workflowAdminService } from '../../../services'
import {
  getWorkflowApiErrorMessage,
  isWorkflowDocumentField,
} from './workflowUi'

interface WorkflowPermissionMatrixDialogProps {
  readonly workflowId: string
  readonly version: WorkflowVersion
  readonly step: WorkflowStep
  readonly requestFields: readonly WorkflowRequestFieldOption[]
  readonly fieldAccessModes: readonly WorkflowCatalogueOption<WorkflowFieldAccess>[]
  readonly documentAccessModes: readonly WorkflowCatalogueOption<WorkflowDocumentAccess>[]
  readonly readOnly: boolean
  readonly onClose: () => void
  readonly onSaved: (version: WorkflowVersion) => void
}

interface PermissionDraft extends WorkflowFieldPermissionValue {
  readonly fieldKey: string
  readonly fieldType: string
  readonly labelEnglish: string
  readonly labelArabic: string
  readonly helpTextEnglish: string | null
  readonly helpTextArabic: string | null
  readonly isRequired: boolean
  readonly isActive: boolean
}

export function WorkflowPermissionMatrixDialog({
  workflowId,
  version,
  step,
  requestFields,
  fieldAccessModes,
  documentAccessModes,
  readOnly,
  onClose,
  onSaved,
}: WorkflowPermissionMatrixDialogProps) {
  const initialPermissions = useMemo<readonly PermissionDraft[]>(
    () =>
      requestFields.map((field) => {
        const existing = step.fieldPermissions.find(
          (permission) => permission.requestFieldDefinitionId === field.id,
        )
        const document = isWorkflowDocumentField(field.fieldType)
        return {
          requestFieldDefinitionId: field.id,
          fieldKey: field.key,
          fieldType: field.fieldType,
          labelEnglish: field.labelEnglish,
          labelArabic: field.labelArabic,
          helpTextEnglish: field.helpTextEnglish,
          helpTextArabic: field.helpTextArabic,
          isRequired: field.isRequired,
          isActive: field.isActive,
          access: existing?.access ?? 'hidden',
          documentAccess: document
            ? (existing?.documentAccess ?? 'hidden')
            : null,
          canSelectForForwarding:
            document && (existing?.canSelectForForwarding ?? false),
          rowVersion: existing?.rowVersion ?? null,
        }
      }),
    [requestFields, step.fieldPermissions],
  )
  const [permissions, setPermissions] =
    useState<readonly PermissionDraft[]>(initialPermissions)
  const [submissionError, setSubmissionError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const requestRef = useRef<AbortController | null>(null)
  const mountedRef = useRef(true)

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      requestRef.current?.abort()
      requestRef.current = null
    }
  }, [])

  const updatePermission = (
    fieldId: string,
    values: Partial<PermissionDraft>,
  ) => {
    setPermissions((current) =>
      current.map((permission) =>
        permission.requestFieldDefinitionId === fieldId
          ? { ...permission, ...values }
          : permission,
      ),
    )
  }

  const handleSave = async () => {
    if (requestRef.current) return
    const controller = new AbortController()
    requestRef.current = controller
    setSubmitting(true)
    setSubmissionError(null)
    setConflict(false)

    try {
      const saved = await workflowAdminService.updateFieldPermissions(
        workflowId,
        version.id,
        step.id,
        {
          permissions: permissions.map((permission) => ({
            requestFieldDefinitionId: permission.requestFieldDefinitionId,
            access: permission.access,
            documentAccess: permission.documentAccess,
            canSelectForForwarding: permission.canSelectForForwarding,
            rowVersion: permission.rowVersion,
          })),
          versionRowVersion: version.rowVersion,
          stepRowVersion: step.rowVersion,
        },
        controller.signal,
      )
      if (!controller.signal.aborted && mountedRef.current) onSaved(saved)
    } catch (error: unknown) {
      if (!controller.signal.aborted && mountedRef.current) {
        setConflict(error instanceof ApiError && error.status === 409)
        setSubmissionError(
          getWorkflowApiErrorMessage(
            error,
            'The field-permission matrix could not be saved.',
          ),
        )
      }
    } finally {
      if (requestRef.current === controller) requestRef.current = null
      if (mountedRef.current) setSubmitting(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => !data.open && !submitting && onClose()}>
      <DialogSurface className="administration-dialog workflow-matrix-dialog">
        <DialogBody>
          <DialogTitle>Field permissions · {step.nameEnglish}</DialogTitle>
          <DialogContent className="administration-dialog__content">
            <p className="administration-dialog__introduction">
              {readOnly ? 'Review' : 'Configure'} every available custom field for
              this step. Protected system fields retain their fixed behavior and are
              shown separately in the designer.
            </p>
            {!step.canEditRequestData ? (
              <MessageBar intent="warning">
                <MessageBarBody>
                  This step cannot edit request data. Editable access modes may be
                  rejected until that step capability is enabled.
                </MessageBarBody>
              </MessageBar>
            ) : null}
            {submissionError ? (
              <MessageBar intent="error" politeness="assertive">
                <MessageBarBody>
                  {submissionError}
                  {conflict ? ' Reload the designer before trying again.' : ''}
                </MessageBarBody>
              </MessageBar>
            ) : null}
            {permissions.length === 0 ? (
              <div className="request-type-fields-empty" role="status">
                <h4>No custom fields</h4>
                <p>The request-type version contains only protected system fields.</p>
              </div>
            ) : (
              <div className="data-table-scroll" tabIndex={0}>
                <table className="data-table workflow-matrix-table">
                  <caption className="visually-hidden">
                    Custom field permissions for {step.nameEnglish}
                  </caption>
                  <thead>
                    <tr>
                      <th scope="col">Custom field</th>
                      <th scope="col">Data access</th>
                      <th scope="col">Document access</th>
                      <th scope="col">Forwarding selection</th>
                    </tr>
                  </thead>
                  <tbody>
                    {permissions.map((permission) => {
                      const document = isWorkflowDocumentField(
                        permission.fieldType,
                      )
                      return (
                        <tr key={permission.requestFieldDefinitionId}>
                          <td>
                            <span className="data-table__primary">
                              {permission.labelEnglish}
                            </span>
                            <span className="data-table__secondary" dir="rtl" lang="ar">
                              {permission.labelArabic}
                            </span>
                            {permission.helpTextEnglish ? (
                              <span className="data-table__secondary">
                                {permission.helpTextEnglish}
                              </span>
                            ) : null}
                            {permission.helpTextArabic ? (
                              <span className="data-table__secondary" dir="rtl" lang="ar">
                                {permission.helpTextArabic}
                              </span>
                            ) : null}
                            <code>{permission.fieldKey}</code>
                            <span className="data-table__secondary">
                              {permission.fieldType}
                              {permission.isRequired ? ' · form required' : ''}
                              {!permission.isActive ? ' · inactive' : ''}
                            </span>
                          </td>
                          <td>
                            <select
                              className="request-type-select"
                              aria-label={`Data access for ${permission.labelEnglish}`}
                              value={permission.access}
                              disabled={submitting || readOnly}
                              onChange={(event) => {
                                const access = event.target.value as WorkflowFieldAccess
                                updatePermission(permission.requestFieldDefinitionId, {
                                  access,
                                  ...(access === 'hidden' && document
                                    ? {
                                        documentAccess: 'hidden' as const,
                                        canSelectForForwarding: false,
                                      }
                                    : {}),
                                })
                              }}
                            >
                              {fieldAccessModes.map((option) => (
                                <option key={option.value} value={option.value}>
                                  {option.labelEnglish}
                                </option>
                              ))}
                            </select>
                          </td>
                          <td>
                            {document ? (
                              <select
                                className="request-type-select"
                                aria-label={`Document access for ${permission.labelEnglish}`}
                                value={permission.documentAccess ?? 'hidden'}
                                disabled={submitting || readOnly || permission.access === 'hidden'}
                                onChange={(event) => {
                                  const documentAccess = event.target
                                    .value as WorkflowDocumentAccess
                                  updatePermission(
                                    permission.requestFieldDefinitionId,
                                    {
                                      documentAccess,
                                      canSelectForForwarding:
                                        documentAccess === 'hidden'
                                          ? false
                                          : permission.canSelectForForwarding,
                                    },
                                  )
                                }}
                              >
                                {documentAccessModes.map((option) => (
                                  <option key={option.value} value={option.value}>
                                    {option.labelEnglish}
                                  </option>
                                ))}
                              </select>
                            ) : (
                              <span className="data-table__secondary">Not a document field</span>
                            )}
                          </td>
                          <td>
                            {document ? (
                              <Checkbox
                                aria-label={`Allow forwarding selection for ${permission.labelEnglish}`}
                                label="May select"
                                checked={permission.canSelectForForwarding}
                                disabled={
                                  submitting ||
                                  readOnly ||
                                  !step.canForwardDocuments ||
                                  permission.documentAccess === 'hidden'
                                }
                                onChange={(_, data) =>
                                  updatePermission(
                                    permission.requestFieldDefinitionId,
                                    {
                                      canSelectForForwarding:
                                        data.checked === true,
                                    },
                                  )
                                }
                              />
                            ) : (
                              <span className="data-table__secondary">Not applicable</span>
                            )}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <Button type="button" appearance="secondary" disabled={submitting} onClick={onClose}>{readOnly ? 'Close' : 'Cancel'}</Button>
            {!readOnly ? <Button type="button" appearance="primary" disabled={submitting} icon={submitting ? <Spinner size="tiny" /> : undefined} onClick={() => void handleSave()}>{submitting ? 'Saving' : 'Save matrix'}</Button> : null}
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
