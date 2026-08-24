import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Spinner,
  Textarea,
} from '@fluentui/react-components'
import {
  Add20Regular,
  Archive20Regular,
  Copy20Regular,
  Delete20Regular,
  Edit20Regular,
  Send20Regular,
} from '@fluentui/react-icons'
import { useEffect, useMemo, useRef, useState } from 'react'
import { AdministrationResourceState } from '../../../components/administration/AdministrationResourceState'
import { StatusBadge } from '../../../components/common'
import type {
  RequestTypeDetail,
  RequestTypeField,
  RequestTypeSystemField,
  RequestTypeVersion,
} from '../../../models'
import {
  ApiError,
  requestTypeAdminService,
} from '../../../services'
import { RequestTypeFieldDialog } from './RequestTypeFieldDialog'
import {
  formatUtcDate,
  getDocumentModeLabel,
  getFieldTypeLabel,
  getLifecycleLabel,
  getRequestTypeApiErrorMessage,
} from './requestTypeUi'

interface RequestTypeVersionDesignerProps {
  readonly requestType: RequestTypeDetail
  readonly version: RequestTypeVersion
  readonly systemFields: readonly RequestTypeSystemField[]
  readonly systemFieldsLoading: boolean
  readonly systemFieldsError: string | null
  readonly onRetrySystemFields: () => void
  readonly onReload: (nextVersionId?: string) => void
}

type LifecycleAction = 'publish' | 'clone' | 'archiveVersion' | 'archiveType'

interface MetadataErrors {
  nameEnglish?: string
  nameArabic?: string
  descriptionEnglish?: string
  descriptionArabic?: string
  requestPrefix?: string
  navigationSlug?: string
  navigationOrder?: string
}

const actionCopy: Readonly<
  Record<LifecycleAction, { title: string; description: string; button: string }>
> = {
  publish: {
    title: 'Publish this version?',
    description:
      'Publishing makes this version stable and read-only. Future changes require a new draft version.',
    button: 'Publish version',
  },
  clone: {
    title: 'Clone into a new draft?',
    description:
      'A new draft version will copy this metadata and its active and inactive custom fields.',
    button: 'Clone version',
  },
  archiveVersion: {
    title: 'Archive this version?',
    description:
      'Archived versions remain available for history but cannot be used for new requests.',
    button: 'Archive version',
  },
  archiveType: {
    title: 'Archive this request type?',
    description:
      'The request type and its definitions remain available for history, but it cannot be used for new requests.',
    button: 'Archive request type',
  },
}

function formatConfigurationValue(value: unknown): string {
  if (value === null || value === undefined) {
    return 'None'
  }

  if (Array.isArray(value)) {
    return value.join(', ') || 'None'
  }

  if (typeof value === 'object') {
    return Object.entries(value)
      .map(([key, item]) =>
        `${key}: ${Array.isArray(item) ? item.join(', ') : String(item)}`,
      )
      .join(' · ')
  }

  if (typeof value === 'boolean') {
    return value ? 'Yes' : 'No'
  }

  return String(value)
}

function getSystemFieldOrder(field: RequestTypeSystemField): number {
  const key = field.key.toLowerCase().replaceAll(/[^a-z]/g, '')
  if (key === 'requestnumber') {
    return 0
  }
  if (key === 'status') {
    return 1
  }
  if (key === 'requestedby') {
    return 2
  }
  return 3
}

export function RequestTypeVersionDesigner({
  requestType,
  version,
  systemFields,
  systemFieldsLoading,
  systemFieldsError,
  onRetrySystemFields,
  onReload,
}: RequestTypeVersionDesignerProps) {
  const [nameEnglish, setNameEnglish] = useState(version.nameEnglish)
  const [nameArabic, setNameArabic] = useState(version.nameArabic)
  const [descriptionEnglish, setDescriptionEnglish] = useState(
    version.descriptionEnglish,
  )
  const [descriptionArabic, setDescriptionArabic] = useState(
    version.descriptionArabic,
  )
  const [requestPrefix, setRequestPrefix] = useState(version.requestPrefix)
  const [navigationSlug, setNavigationSlug] = useState(version.navigationSlug)
  const [navigationOrder, setNavigationOrder] = useState(
    version.navigationOrder.toString(),
  )
  const [metadataErrors, setMetadataErrors] = useState<MetadataErrors>({})
  const [operationError, setOperationError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)
  const [savingMetadata, setSavingMetadata] = useState(false)
  const [pendingAction, setPendingAction] = useState<LifecycleAction | null>(null)
  const [runningAction, setRunningAction] = useState(false)
  const [editingField, setEditingField] = useState<RequestTypeField | null>(null)
  const [addingField, setAddingField] = useState(false)
  const [deletingField, setDeletingField] = useState<RequestTypeField | null>(null)
  const [deleting, setDeleting] = useState(false)
  const requestRef = useRef<AbortController | null>(null)
  const mountedRef = useRef(true)
  const fieldTriggerRef = useRef<HTMLButtonElement | null>(null)
  const confirmationTriggerRef = useRef<HTMLButtonElement | null>(null)
  const editable = version.lifecycle === 'draft' && !requestType.isArchived
  const busy = savingMetadata || runningAction || deleting
  const orderedFields = useMemo(
    () =>
      [...version.fields].sort(
        (left, right) => left.sortOrder - right.sortOrder || left.key.localeCompare(right.key),
      ),
    [version.fields],
  )
  const orderedSystemFields = useMemo(
    () =>
      [...systemFields].sort(
        (left, right) =>
          getSystemFieldOrder(left) - getSystemFieldOrder(right) ||
          left.labelEnglish.localeCompare(right.labelEnglish),
      ),
    [systemFields],
  )

  useEffect(() => {
    mountedRef.current = true

    return () => {
      mountedRef.current = false
      requestRef.current?.abort()
      requestRef.current = null
    }
  }, [])

  const beginRequest = (): AbortController | null => {
    if (requestRef.current) {
      return null
    }

    const controller = new AbortController()
    requestRef.current = controller
    setOperationError(null)
    setConflict(false)
    return controller
  }

  const finishRequest = (controller: AbortController) => {
    if (requestRef.current === controller) {
      requestRef.current = null
    }
  }

  const recordError = (error: unknown, fallback: string) => {
    if (!mountedRef.current) {
      return
    }

    setConflict(error instanceof ApiError && error.status === 409)
    setOperationError(getRequestTypeApiErrorMessage(error, fallback))
  }

  const openLifecycleAction = (
    action: LifecycleAction,
    trigger: HTMLButtonElement,
  ) => {
    confirmationTriggerRef.current = trigger
    setOperationError(null)
    setConflict(false)
    setPendingAction(action)
  }

  const closeLifecycleAction = () => {
    if (runningAction) {
      return
    }

    setPendingAction(null)
    window.requestAnimationFrame(() => confirmationTriggerRef.current?.focus())
  }

  const closeFieldEditor = () => {
    setAddingField(false)
    setEditingField(null)
    window.requestAnimationFrame(() => fieldTriggerRef.current?.focus())
  }

  const closeDeleteField = () => {
    if (deleting) {
      return
    }

    setDeletingField(null)
    window.requestAnimationFrame(() => confirmationTriggerRef.current?.focus())
  }

  const validateMetadata = (): MetadataErrors => {
    const errors: MetadataErrors = {}

    if (!nameEnglish.trim()) {
      errors.nameEnglish = 'Enter the English request type name.'
    }
    if (!nameArabic.trim()) {
      errors.nameArabic = 'Enter the Arabic request type name.'
    }
    if (!descriptionEnglish.trim()) {
      errors.descriptionEnglish = 'Enter the English description.'
    }
    if (!descriptionArabic.trim()) {
      errors.descriptionArabic = 'Enter the Arabic description.'
    }
    if (!/^[A-Za-z][A-Za-z0-9]{0,11}$/.test(requestPrefix.trim())) {
      errors.requestPrefix =
        'Start with a letter and use up to 12 letters or numbers.'
    }
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(navigationSlug.trim())) {
      errors.navigationSlug =
        'Use lowercase letters, numbers, and single hyphens.'
    }
    if (
      !Number.isInteger(Number(navigationOrder)) ||
      Number(navigationOrder) < 0 ||
      Number(navigationOrder) > 100000
    ) {
      errors.navigationOrder = 'Enter a whole number from 0 to 100,000.'
    }

    return errors
  }

  const handleMetadataSubmit = async (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault()
    if (!editable) {
      return
    }

    const errors = validateMetadata()
    setMetadataErrors(errors)
    if (Object.keys(errors).length > 0) {
      return
    }

    const controller = beginRequest()
    if (!controller) {
      return
    }
    setSavingMetadata(true)

    try {
      const saved = await requestTypeAdminService.updateVersion(
        requestType.id,
        version.id,
        {
          nameEnglish: nameEnglish.trim(),
          nameArabic: nameArabic.trim(),
          descriptionEnglish: descriptionEnglish.trim(),
          descriptionArabic: descriptionArabic.trim(),
          requestPrefix: requestPrefix.trim().toUpperCase(),
          navigationSlug: navigationSlug.trim(),
          navigationOrder: Number(navigationOrder),
          rowVersion: version.rowVersion,
        },
        controller.signal,
      )

      if (!controller.signal.aborted && mountedRef.current) {
        onReload(saved.id)
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted) {
        recordError(error, 'The request type metadata could not be saved.')
      }
    } finally {
      finishRequest(controller)
      if (mountedRef.current) {
        setSavingMetadata(false)
      }
    }
  }

  const handleLifecycleAction = async () => {
    if (!pendingAction) {
      return
    }

    const controller = beginRequest()
    if (!controller) {
      return
    }
    const action = pendingAction
    setRunningAction(true)

    try {
      if (action === 'archiveType') {
        await requestTypeAdminService.archiveRequestType(
          requestType.id,
          { rowVersion: requestType.rowVersion },
          controller.signal,
        )
        if (!controller.signal.aborted && mountedRef.current) {
          setPendingAction(null)
          onReload(version.id)
        }
      } else {
        const actionRequest = { rowVersion: version.rowVersion }
        const saved =
          action === 'publish'
            ? await requestTypeAdminService.publishVersion(
                requestType.id,
                version.id,
                actionRequest,
                controller.signal,
              )
            : action === 'clone'
              ? await requestTypeAdminService.cloneVersion(
                  requestType.id,
                  version.id,
                  actionRequest,
                  controller.signal,
                )
              : await requestTypeAdminService.archiveVersion(
                  requestType.id,
                  version.id,
                  actionRequest,
                  controller.signal,
                )

        if (!controller.signal.aborted && mountedRef.current) {
          setPendingAction(null)
          onReload(saved.id)
        }
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted) {
        recordError(error, 'The lifecycle action could not be completed.')
      }
    } finally {
      finishRequest(controller)
      if (mountedRef.current) {
        setRunningAction(false)
      }
    }
  }

  const handleDeleteField = async () => {
    if (!deletingField) {
      return
    }

    const controller = beginRequest()
    if (!controller) {
      return
    }
    setDeleting(true)

    try {
      const saved = await requestTypeAdminService.deleteField(
        requestType.id,
        version.id,
        deletingField.id,
        {
          versionRowVersion: version.rowVersion,
          fieldRowVersion: deletingField.rowVersion,
        },
        controller.signal,
      )

      if (!controller.signal.aborted && mountedRef.current) {
        setDeletingField(null)
        onReload(saved.id)
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted) {
        recordError(error, 'The custom field could not be deleted.')
      }
    } finally {
      finishRequest(controller)
      if (mountedRef.current) {
        setDeleting(false)
      }
    }
  }

  return (
    <div className="request-type-version-designer">
      {operationError ? (
        <MessageBar intent="error" politeness="assertive">
          <MessageBarBody>
            {operationError}
            {conflict ? (
              <>
                <span className="administration-conflict-copy">
                  The definition changed or its lifecycle no longer allows this
                  operation.
                </span>
                <Button
                  type="button"
                  appearance="transparent"
                  onClick={() => onReload(version.id)}
                >
                  Reload current version
                </Button>
              </>
            ) : null}
          </MessageBarBody>
        </MessageBar>
      ) : null}

      <section className="request-type-lifecycle-panel" aria-label="Version lifecycle">
        <div>
          <StatusBadge status={getLifecycleLabel(version.lifecycle)} />
          {version.isInUse ? <StatusBadge status="In use" /> : null}
          <p>
            Version {version.versionNumber} · {version.requestCount} request
            {version.requestCount === 1 ? '' : 's'} · Updated{' '}
            {formatUtcDate(version.modifiedAtUtc ?? version.createdAtUtc)}
          </p>
        </div>
        <div className="request-type-lifecycle-actions">
          {editable ? (
            <Button
              type="button"
              appearance="primary"
              icon={<Send20Regular />}
              disabled={busy}
              onClick={(event) =>
                openLifecycleAction('publish', event.currentTarget)
              }
            >
              Publish
            </Button>
          ) : null}
          {version.lifecycle !== 'draft' && !requestType.isArchived ? (
            <Button
              type="button"
              appearance="outline"
              icon={<Copy20Regular />}
              disabled={busy}
              onClick={(event) =>
                openLifecycleAction('clone', event.currentTarget)
              }
            >
              Clone to draft
            </Button>
          ) : null}
          {version.lifecycle !== 'archived' && !requestType.isArchived ? (
            <Button
              type="button"
              appearance="subtle"
              icon={<Archive20Regular />}
              disabled={busy}
              onClick={(event) =>
                openLifecycleAction('archiveVersion', event.currentTarget)
              }
            >
              Archive version
            </Button>
          ) : null}
          {!requestType.isArchived ? (
            <Button
              type="button"
              appearance="subtle"
              className="administration-danger-button"
              icon={<Archive20Regular />}
              disabled={busy}
              onClick={(event) =>
                openLifecycleAction('archiveType', event.currentTarget)
              }
            >
              Archive request type
            </Button>
          ) : null}
        </div>
      </section>

      {!editable ? (
        <MessageBar intent="info">
          <MessageBarBody>
            {requestType.isArchived
              ? 'This request type is archived and read-only.'
              : version.isInUse
                ? 'This published version is in use and permanently read-only. Clone it to create a new draft.'
                : 'Published and archived versions are read-only. Clone this version to continue editing.'}
          </MessageBarBody>
        </MessageBar>
      ) : null}

      <form
        className="request-type-metadata-form"
        aria-busy={savingMetadata}
        noValidate
        onSubmit={handleMetadataSubmit}
      >
        <div className="request-type-editor-section__heading request-type-editor-section__heading--actions">
          <div>
            <h3>Bilingual metadata</h3>
            <p>The request-type code is fixed as {requestType.code}.</p>
          </div>
          {editable ? (
            <Button
              type="submit"
              appearance="primary"
              disabled={busy}
              icon={savingMetadata ? <Spinner size="tiny" /> : undefined}
            >
              {savingMetadata ? 'Saving' : 'Save metadata'}
            </Button>
          ) : null}
        </div>
        <div className="administration-form-grid">
          <Field
            label="English name"
            required
            validationState={metadataErrors.nameEnglish ? 'error' : 'none'}
            validationMessage={metadataErrors.nameEnglish}
          >
            <Input
              value={nameEnglish}
              maxLength={200}
              readOnly={!editable}
              disabled={savingMetadata}
              onChange={(_, data) => setNameEnglish(data.value)}
            />
          </Field>
          <Field
            label="Arabic name"
            required
            validationState={metadataErrors.nameArabic ? 'error' : 'none'}
            validationMessage={metadataErrors.nameArabic}
          >
            <Input
              dir="rtl"
              lang="ar"
              value={nameArabic}
              maxLength={200}
              readOnly={!editable}
              disabled={savingMetadata}
              onChange={(_, data) => setNameArabic(data.value)}
            />
          </Field>
          <Field
            label="English description"
            required
            validationState={metadataErrors.descriptionEnglish ? 'error' : 'none'}
            validationMessage={metadataErrors.descriptionEnglish}
          >
            <Textarea
              value={descriptionEnglish}
              maxLength={2000}
              resize="vertical"
              readOnly={!editable}
              disabled={savingMetadata}
              onChange={(_, data) => setDescriptionEnglish(data.value)}
            />
          </Field>
          <Field
            label="Arabic description"
            required
            validationState={metadataErrors.descriptionArabic ? 'error' : 'none'}
            validationMessage={metadataErrors.descriptionArabic}
          >
            <Textarea
              dir="rtl"
              lang="ar"
              value={descriptionArabic}
              maxLength={2000}
              resize="vertical"
              readOnly={!editable}
              disabled={savingMetadata}
              onChange={(_, data) => setDescriptionArabic(data.value)}
            />
          </Field>
          <Field
            label="Request-number prefix"
            required
            validationState={metadataErrors.requestPrefix ? 'error' : 'none'}
            validationMessage={metadataErrors.requestPrefix}
          >
            <Input
              value={requestPrefix}
              maxLength={12}
              readOnly={!editable}
              disabled={savingMetadata}
              onChange={(_, data) => setRequestPrefix(data.value)}
            />
          </Field>
          <Field
            label="Navigation slug"
            required
            validationState={metadataErrors.navigationSlug ? 'error' : 'none'}
            validationMessage={metadataErrors.navigationSlug}
          >
            <Input
              value={navigationSlug}
              maxLength={100}
              readOnly={!editable}
              disabled={savingMetadata}
              onChange={(_, data) => setNavigationSlug(data.value)}
            />
          </Field>
          <Field
            label="Navigation order"
            required
            validationState={metadataErrors.navigationOrder ? 'error' : 'none'}
            validationMessage={metadataErrors.navigationOrder}
          >
            <Input
              type="number"
              min={0}
              max={100000}
              step={1}
              value={navigationOrder}
              readOnly={!editable}
              disabled={savingMetadata}
              onChange={(_, data) => setNavigationOrder(data.value)}
            />
          </Field>
        </div>
      </form>

      <section className="request-type-editor-section">
        <div className="request-type-editor-section__heading">
          <h3>Protected system fields</h3>
          <p>
            Request Number is always visible and read-only. Status, Requested By,
            audit data, internal identifiers, and row versions remain under system
            control and are never included in custom-field requests.
          </p>
        </div>
        <AdministrationResourceState
          loading={systemFieldsLoading}
          error={systemFieldsError}
          empty={!systemFieldsLoading && !systemFieldsError && systemFields.length === 0}
          loadingLabel="Loading protected system fields"
          emptyTitle="No system fields returned"
          emptyDescription="Reload the protected field catalogue before publishing."
          onRetry={onRetrySystemFields}
        >
          <div className="request-type-system-fields">
            {orderedSystemFields.map((field) => (
              <article key={field.key} className="request-type-system-field">
                <div>
                  <h4>{field.labelEnglish}</h4>
                  <p dir="rtl" lang="ar">
                    {field.labelArabic}
                  </p>
                </div>
                <code>{field.key}</code>
                <span>{field.dataType}</span>
                <span>
                  {field.isAlwaysVisible ? 'Always visible' : 'System visibility'}
                  {' · '}
                  {field.isEditable ? 'System editable' : 'Read-only'}
                  {' · '}
                  {field.isRemovable ? 'System removable' : 'Protected'}
                </span>
              </article>
            ))}
          </div>
        </AdministrationResourceState>
      </section>

      <section className="request-type-editor-section">
        <div className="request-type-editor-section__heading request-type-editor-section__heading--actions">
          <div>
            <h3>Custom fields</h3>
            <p>
              {orderedFields.length} field{orderedFields.length === 1 ? '' : 's'} in
              this version. Only a draft can be changed.
            </p>
          </div>
          {editable ? (
            <Button
              type="button"
              appearance="primary"
              icon={<Add20Regular />}
              disabled={busy}
              onClick={(event) => {
                fieldTriggerRef.current = event.currentTarget
                setAddingField(true)
              }}
            >
              Add custom field
            </Button>
          ) : null}
        </div>

        {orderedFields.length === 0 ? (
          <div className="request-type-fields-empty" role="status">
            <h4>No custom fields</h4>
            <p>
              The protected system fields still apply. Add a custom field while this
              version is a draft.
            </p>
          </div>
        ) : (
          <div className="data-table-scroll" tabIndex={0}>
            <table className="data-table request-type-fields-table">
              <caption className="visually-hidden">
                Custom fields for request type version {version.versionNumber}
              </caption>
              <thead>
                <tr>
                  <th scope="col">Field</th>
                  <th scope="col">Type</th>
                  <th scope="col">Configuration</th>
                  <th scope="col">Status</th>
                  {editable ? (
                    <th scope="col">
                      <span className="visually-hidden">Actions</span>
                    </th>
                  ) : null}
                </tr>
              </thead>
              <tbody>
                {orderedFields.map((field) => {
                  const documentMode = getDocumentModeLabel(field.documentMode)
                  return (
                    <tr key={field.id}>
                      <td>
                        <span className="data-table__primary">
                          {field.labelEnglish}
                        </span>
                        <span className="data-table__secondary" dir="rtl" lang="ar">
                          {field.labelArabic}
                        </span>
                        <code>{field.key}</code>
                      </td>
                      <td>
                        {getFieldTypeLabel(field.fieldType)}
                        {documentMode ? (
                          <span className="data-table__secondary">{documentMode}</span>
                        ) : null}
                      </td>
                      <td>
                        <details className="request-type-field-configuration">
                          <summary>
                            {field.isRequired ? 'Required' : 'Optional'} · Order{' '}
                            {field.sortOrder}
                          </summary>
                          <dl>
                            <div>
                              <dt>Default</dt>
                              <dd>{formatConfigurationValue(field.defaultValue)}</dd>
                            </div>
                            <div>
                              <dt>Validation</dt>
                              <dd>
                                {formatConfigurationValue(field.validationConfig)}
                              </dd>
                            </div>
                            {field.choiceConfig ? (
                              <div>
                                <dt>Choices</dt>
                                <dd>
                                  {field.choiceConfig.options
                                    .map(
                                      (option) =>
                                        `${option.labelEnglish} (${option.value})${option.isActive ? '' : ' — inactive'}`,
                                    )
                                    .join(', ')}
                                </dd>
                              </div>
                            ) : null}
                          </dl>
                        </details>
                      </td>
                      <td>
                        <StatusBadge status={field.isActive ? 'Active' : 'Inactive'} />
                      </td>
                      {editable ? (
                        <td className="data-table__action">
                          <div className="administration-table__actions">
                            <Button
                              type="button"
                              appearance="subtle"
                              size="small"
                              icon={<Edit20Regular />}
                              disabled={busy}
                              aria-label={`Edit ${field.labelEnglish}`}
                              onClick={(event) => {
                                fieldTriggerRef.current = event.currentTarget
                                setEditingField(field)
                              }}
                            >
                              Edit
                            </Button>
                            <Button
                              type="button"
                              appearance="subtle"
                              size="small"
                              className="administration-danger-button"
                              icon={<Delete20Regular />}
                              disabled={busy}
                              aria-label={`Delete ${field.labelEnglish}`}
                              onClick={(event) => {
                                confirmationTriggerRef.current = event.currentTarget
                                setOperationError(null)
                                setConflict(false)
                                setDeletingField(field)
                              }}
                            >
                              Delete
                            </Button>
                          </div>
                        </td>
                      ) : null}
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {addingField || editingField ? (
        <RequestTypeFieldDialog
          key={editingField?.id ?? 'new-request-type-field'}
          requestTypeId={requestType.id}
          versionId={version.id}
          versionRowVersion={version.rowVersion}
          field={editingField}
          onClose={closeFieldEditor}
          onSaved={(saved) => {
            setAddingField(false)
            setEditingField(null)
            onReload(saved.id)
          }}
        />
      ) : null}

      <Dialog
        open={pendingAction !== null}
        onOpenChange={(_, data) => {
          if (!data.open && !runningAction) {
            closeLifecycleAction()
          }
        }}
      >
        <DialogSurface className="administration-dialog">
          <DialogBody>
            <DialogTitle>
              {pendingAction ? actionCopy[pendingAction].title : ''}
            </DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p>{pendingAction ? actionCopy[pendingAction].description : ''}</p>
              {operationError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>
                    {operationError}
                    {conflict ? ' Reload the designer before trying again.' : ''}
                  </MessageBarBody>
                </MessageBar>
              ) : null}
            </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                disabled={runningAction}
                onClick={closeLifecycleAction}
              >
                Cancel
              </Button>
              <Button
                type="button"
                appearance="primary"
                disabled={runningAction}
                icon={runningAction ? <Spinner size="tiny" /> : undefined}
                onClick={() => void handleLifecycleAction()}
              >
                {runningAction
                  ? 'Working'
                  : pendingAction
                    ? actionCopy[pendingAction].button
                    : 'Continue'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Dialog
        open={deletingField !== null}
        onOpenChange={(_, data) => {
          if (!data.open && !deleting) {
            closeDeleteField()
          }
        }}
      >
        <DialogSurface className="administration-dialog">
          <DialogBody>
            <DialogTitle>Delete custom field?</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p>
                {deletingField?.labelEnglish} will be removed from this draft. This
                does not affect protected system fields or published versions.
              </p>
              {operationError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>
                    {operationError}
                    {conflict ? ' Reload the designer before trying again.' : ''}
                  </MessageBarBody>
                </MessageBar>
              ) : null}
            </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                disabled={deleting}
                onClick={closeDeleteField}
              >
                Cancel
              </Button>
              <Button
                type="button"
                appearance="primary"
                disabled={deleting}
                icon={deleting ? <Spinner size="tiny" /> : undefined}
                onClick={() => void handleDeleteField()}
              >
                {deleting ? 'Deleting' : 'Delete field'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
