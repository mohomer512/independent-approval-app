import {
  Button,
  Checkbox,
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
  CheckmarkCircle20Regular,
  Copy20Regular,
  Delete20Regular,
  Edit20Regular,
  Grid20Regular,
  Send20Regular,
} from '@fluentui/react-icons'
import { useEffect, useMemo, useRef, useState } from 'react'
import { StatusBadge } from '../../../components/common'
import type {
  WorkflowDetail,
  WorkflowOptions,
  WorkflowStep,
  WorkflowTransition,
  WorkflowValidationResult,
  WorkflowVersion,
} from '../../../models'
import { ApiError, workflowAdminService } from '../../../services'
import { WorkflowPermissionMatrixDialog } from './WorkflowPermissionMatrixDialog'
import { WorkflowStepDialog } from './WorkflowStepDialog'
import { WorkflowTransitionDialog } from './WorkflowTransitionDialog'
import {
  formatWorkflowDate,
  getCatalogueLabel,
  getWorkflowApiErrorMessage,
  getWorkflowLifecycleLabel,
} from './workflowUi'

interface WorkflowVersionDesignerProps {
  readonly workflow: WorkflowDetail
  readonly version: WorkflowVersion
  readonly options: WorkflowOptions | null
  readonly optionsLoading: boolean
  readonly optionsError: string | null
  readonly onRetryOptions: () => void
  readonly onReload: (nextVersionId?: string) => void
}

type LifecycleAction = 'publish' | 'clone' | 'archiveVersion' | 'archiveWorkflow'
type DeleteTarget =
  | { readonly kind: 'step'; readonly item: WorkflowStep }
  | { readonly kind: 'transition'; readonly item: WorkflowTransition }

interface MetadataErrors {
  requestTypeVersionId?: string
  nameEnglish?: string
  nameArabic?: string
  descriptionEnglish?: string
  descriptionArabic?: string
  navigationLabelEnglish?: string
  navigationLabelArabic?: string
  navigationSlug?: string
  navigationOrder?: string
}

const actionCopy: Readonly<
  Record<LifecycleAction, { title: string; description: string; button: string }>
> = {
  publish: {
    title: 'Publish this workflow version?',
    description:
      'Publishing locks this graph and field-permission matrix. Validation must pass before it can be used.',
    button: 'Publish version',
  },
  clone: {
    title: 'Clone into a new draft?',
    description:
      'A new draft will copy the graph, role assignments, and permission matrix. Existing requests remain on their original version.',
    button: 'Clone version',
  },
  archiveVersion: {
    title: 'Archive this workflow version?',
    description:
      'The version remains available for history and existing requests but cannot start new requests.',
    button: 'Archive version',
  },
  archiveWorkflow: {
    title: 'Archive this workflow?',
    description:
      'The workflow definition remains available for history but cannot be selected for new requests.',
    button: 'Archive workflow',
  },
}

function getDeleteTargetName(target: DeleteTarget | null): string {
  if (!target) return 'This item'
  return target.kind === 'step'
    ? target.item.nameEnglish
    : target.item.actionLabelEnglish
}

function transitionCreatesLoop(
  transition: WorkflowTransition,
  transitions: readonly WorkflowTransition[],
): boolean {
  if (!transition.targetStepId) return false
  if (transition.targetStepId === transition.sourceStepId) return true

  const visited = new Set<string>()
  const pending = [transition.targetStepId]
  while (pending.length > 0) {
    const stepId = pending.pop()
    if (!stepId || visited.has(stepId)) continue
    if (stepId === transition.sourceStepId) return true
    visited.add(stepId)
    transitions.forEach((candidate) => {
      if (candidate.sourceStepId === stepId && candidate.targetStepId) {
        pending.push(candidate.targetStepId)
      }
    })
  }

  return false
}

export function WorkflowVersionDesigner({
  workflow,
  version,
  options,
  optionsLoading,
  optionsError,
  onRetryOptions,
  onReload,
}: WorkflowVersionDesignerProps) {
  const [requestTypeVersionId, setRequestTypeVersionId] = useState(
    version.requestTypeVersionId,
  )
  const [nameEnglish, setNameEnglish] = useState(version.nameEnglish)
  const [nameArabic, setNameArabic] = useState(version.nameArabic)
  const [descriptionEnglish, setDescriptionEnglish] = useState(version.descriptionEnglish)
  const [descriptionArabic, setDescriptionArabic] = useState(version.descriptionArabic)
  const [navigationLabelEnglish, setNavigationLabelEnglish] = useState(
    version.navigationLabelEnglish,
  )
  const [navigationLabelArabic, setNavigationLabelArabic] = useState(
    version.navigationLabelArabic,
  )
  const [navigationSlug, setNavigationSlug] = useState(version.navigationSlug)
  const [navigationOrder, setNavigationOrder] = useState(
    String(version.navigationOrder),
  )
  const [starterRoleIds, setStarterRoleIds] = useState<readonly string[]>(
    version.starterRoles.map((role) => role.id),
  )
  const [rebindAcknowledged, setRebindAcknowledged] = useState(false)
  const [metadataErrors, setMetadataErrors] = useState<MetadataErrors>({})
  const [operationError, setOperationError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)
  const [operation, setOperation] = useState<string | null>(null)
  const [validationResult, setValidationResult] =
    useState<WorkflowValidationResult | null>(null)
  const [pendingAction, setPendingAction] = useState<LifecycleAction | null>(null)
  const [stepDialog, setStepDialog] = useState<WorkflowStep | 'new' | null>(null)
  const [transitionDialog, setTransitionDialog] =
    useState<WorkflowTransition | 'new' | null>(null)
  const [matrixStep, setMatrixStep] = useState<WorkflowStep | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<DeleteTarget | null>(null)
  const requestRef = useRef<AbortController | null>(null)
  const mountedRef = useRef(true)
  const editorTriggerRef = useRef<HTMLButtonElement | null>(null)
  const confirmationTriggerRef = useRef<HTMLButtonElement | null>(null)
  const editable = version.lifecycle === 'draft' && !workflow.isArchived
  const busy = operation !== null
  const rebindChanged = requestTypeVersionId !== version.requestTypeVersionId
  const sortedSteps = useMemo(
    () => [...version.steps].sort((a, b) => a.sortOrder - b.sortOrder || a.key.localeCompare(b.key)),
    [version.steps],
  )
  const sortedTransitions = useMemo(
    () => [...version.transitions].sort((a, b) => a.sortOrder - b.sortOrder || a.key.localeCompare(b.key)),
    [version.transitions],
  )

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      requestRef.current?.abort()
      requestRef.current = null
    }
  }, [])

  const begin = (name: string): AbortController | null => {
    if (requestRef.current) return null
    const controller = new AbortController()
    requestRef.current = controller
    setOperation(name)
    setOperationError(null)
    setConflict(false)
    return controller
  }

  const finish = (controller: AbortController) => {
    if (requestRef.current === controller) requestRef.current = null
    if (mountedRef.current) setOperation(null)
  }

  const recordError = (error: unknown, fallback: string) => {
    if (!mountedRef.current) return
    setConflict(error instanceof ApiError && error.status === 409)
    setOperationError(getWorkflowApiErrorMessage(error, fallback))
  }

  const validateMetadata = (): MetadataErrors => {
    const next: MetadataErrors = {}
    if (!requestTypeVersionId) {
      next.requestTypeVersionId = 'Select a published request-type version.'
    }
    if (!nameEnglish.trim()) next.nameEnglish = 'Enter the English name.'
    if (!nameArabic.trim()) next.nameArabic = 'Enter the Arabic name.'
    if (!descriptionEnglish.trim()) next.descriptionEnglish = 'Enter the English description.'
    if (!descriptionArabic.trim()) next.descriptionArabic = 'Enter the Arabic description.'
    if (!navigationLabelEnglish.trim()) {
      next.navigationLabelEnglish = 'Enter the English navigation label.'
    }
    if (!navigationLabelArabic.trim()) {
      next.navigationLabelArabic = 'Enter the Arabic navigation label.'
    }
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(navigationSlug.trim())) {
      next.navigationSlug = 'Use lowercase letters, numbers, and single hyphens.'
    }
    const order = Number(navigationOrder)
    if (!Number.isInteger(order) || order < 0 || order > 100000) {
      next.navigationOrder = 'Enter a whole number from 0 to 100,000.'
    }
    return next
  }

  const handleMetadataSave = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!editable || (rebindChanged && !rebindAcknowledged)) return
    const errors = validateMetadata()
    setMetadataErrors(errors)
    if (Object.keys(errors).length > 0) return
    const controller = begin('metadata')
    if (!controller) return
    try {
      const saved = await workflowAdminService.updateVersion(
        workflow.id,
        version.id,
        {
          requestTypeVersionId,
          nameEnglish: nameEnglish.trim(),
          nameArabic: nameArabic.trim(),
          descriptionEnglish: descriptionEnglish.trim(),
          descriptionArabic: descriptionArabic.trim(),
          navigationLabelEnglish: navigationLabelEnglish.trim(),
          navigationLabelArabic: navigationLabelArabic.trim(),
          navigationSlug: navigationSlug.trim(),
          navigationOrder: Number(navigationOrder),
          rowVersion: version.rowVersion,
        },
        controller.signal,
      )
      if (!controller.signal.aborted && mountedRef.current) onReload(saved.id)
    } catch (error: unknown) {
      if (!controller.signal.aborted) recordError(error, 'Workflow metadata could not be saved.')
    } finally {
      finish(controller)
    }
  }

  const handleStarterRolesSave = async () => {
    if (!editable) return
    const controller = begin('starterRoles')
    if (!controller) return
    try {
      const saved = await workflowAdminService.updateStarterRoles(
        workflow.id,
        version.id,
        { roleIds: starterRoleIds, versionRowVersion: version.rowVersion },
        controller.signal,
      )
      if (!controller.signal.aborted && mountedRef.current) onReload(saved.id)
    } catch (error: unknown) {
      if (!controller.signal.aborted) recordError(error, 'Starter roles could not be saved.')
    } finally {
      finish(controller)
    }
  }

  const handleValidate = async () => {
    const controller = begin('validate')
    if (!controller) return
    setValidationResult(null)
    try {
      const result = await workflowAdminService.validateVersion(
        workflow.id,
        version.id,
        { versionRowVersion: version.rowVersion },
        controller.signal,
      )
      if (!controller.signal.aborted && mountedRef.current) setValidationResult(result)
    } catch (error: unknown) {
      if (!controller.signal.aborted) recordError(error, 'Workflow validation could not be completed.')
    } finally {
      finish(controller)
    }
  }

  const handleLifecycleAction = async () => {
    if (!pendingAction) return
    const action = pendingAction
    const controller = begin('lifecycle')
    if (!controller) return
    try {
      if (action === 'archiveWorkflow') {
        await workflowAdminService.archiveWorkflow(
          workflow.id,
          { rowVersion: workflow.rowVersion },
          controller.signal,
        )
        if (!controller.signal.aborted && mountedRef.current) {
          setPendingAction(null)
          onReload(version.id)
        }
      } else {
        const request = { rowVersion: version.rowVersion }
        const saved =
          action === 'publish'
            ? await workflowAdminService.publishVersion(workflow.id, version.id, request, controller.signal)
            : action === 'clone'
              ? await workflowAdminService.cloneVersion(workflow.id, version.id, request, controller.signal)
              : await workflowAdminService.archiveVersion(workflow.id, version.id, request, controller.signal)
        if (!controller.signal.aborted && mountedRef.current) {
          setPendingAction(null)
          onReload(saved.id)
        }
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted) recordError(error, 'The workflow lifecycle action could not be completed.')
    } finally {
      finish(controller)
    }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    const controller = begin('delete')
    if (!controller) return
    try {
      const saved =
        deleteTarget.kind === 'step'
          ? await workflowAdminService.deleteStep(
              workflow.id,
              version.id,
              deleteTarget.item.id,
              {
                versionRowVersion: version.rowVersion,
                stepRowVersion: deleteTarget.item.rowVersion,
              },
              controller.signal,
            )
          : await workflowAdminService.deleteTransition(
              workflow.id,
              version.id,
              deleteTarget.item.id,
              {
                versionRowVersion: version.rowVersion,
                transitionRowVersion: deleteTarget.item.rowVersion,
              },
              controller.signal,
            )
      if (!controller.signal.aborted && mountedRef.current) {
        setDeleteTarget(null)
        onReload(saved.id)
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted) recordError(error, 'The workflow item could not be deleted.')
    } finally {
      finish(controller)
    }
  }

  const closeEditor = () => {
    setStepDialog(null)
    setTransitionDialog(null)
    setMatrixStep(null)
    window.requestAnimationFrame(() => editorTriggerRef.current?.focus())
  }
  const openAction = (action: LifecycleAction, trigger: HTMLButtonElement) => {
    confirmationTriggerRef.current = trigger
    setOperationError(null)
    setConflict(false)
    setPendingAction(action)
  }
  const closeAction = () => {
    if (operation === 'lifecycle') return
    setPendingAction(null)
    window.requestAnimationFrame(() => confirmationTriggerRef.current?.focus())
  }
  const closeDelete = () => {
    if (operation === 'delete') return
    setDeleteTarget(null)
    window.requestAnimationFrame(() => confirmationTriggerRef.current?.focus())
  }

  return (
    <div className="workflow-version-designer">
      {operationError ? (
        <MessageBar intent="error" politeness="assertive">
          <MessageBarBody>
            {operationError}
            {conflict ? <><span className="administration-conflict-copy">The graph changed or its lifecycle no longer allows this operation.</span><Button type="button" appearance="transparent" onClick={() => onReload(version.id)}>Reload current version</Button></> : null}
          </MessageBarBody>
        </MessageBar>
      ) : null}

      <section className="request-type-lifecycle-panel" aria-label="Workflow lifecycle">
        <div>
          <StatusBadge status={getWorkflowLifecycleLabel(version.lifecycle)} />
          {version.isInUse ? <StatusBadge status="In use" /> : null}
          <p>Version {version.versionNumber} · {version.requestCount} request{version.requestCount === 1 ? '' : 's'} · Updated {formatWorkflowDate(version.modifiedAtUtc ?? version.createdAtUtc)}</p>
        </div>
        <div className="request-type-lifecycle-actions">
          <Button type="button" appearance="outline" icon={<CheckmarkCircle20Regular />} disabled={busy} onClick={() => void handleValidate()}>{operation === 'validate' ? 'Validating' : 'Validate'}</Button>
          {editable ? <Button type="button" appearance="primary" icon={<Send20Regular />} disabled={busy} onClick={(event) => openAction('publish', event.currentTarget)}>Publish</Button> : null}
          {version.lifecycle !== 'draft' && !workflow.isArchived ? <Button type="button" appearance="outline" icon={<Copy20Regular />} disabled={busy} onClick={(event) => openAction('clone', event.currentTarget)}>Clone to draft</Button> : null}
          {version.lifecycle !== 'archived' && !workflow.isArchived ? <Button type="button" appearance="subtle" icon={<Archive20Regular />} disabled={busy} onClick={(event) => openAction('archiveVersion', event.currentTarget)}>Archive version</Button> : null}
          {!workflow.isArchived ? <Button type="button" appearance="subtle" className="administration-danger-button" icon={<Archive20Regular />} disabled={busy} onClick={(event) => openAction('archiveWorkflow', event.currentTarget)}>Archive workflow</Button> : null}
        </div>
      </section>

      {validationResult ? (
        <section className={`workflow-validation-results ${validationResult.isValid ? 'workflow-validation-results--valid' : ''}`} aria-live="polite">
          <h3>{validationResult.isValid ? 'Workflow is valid' : `${validationResult.issues.length} validation issue${validationResult.issues.length === 1 ? '' : 's'}`}</h3>
          {validationResult.isValid ? <p>The draft graph satisfies the current structural and permission rules.</p> : <ul>{validationResult.issues.map((issue, index) => <li key={`${issue.code}:${issue.path}:${index}`}><strong>{issue.path}</strong> — {issue.message} <code>{issue.code}</code></li>)}</ul>}
        </section>
      ) : null}

      {!editable ? <MessageBar intent="info"><MessageBarBody>{workflow.isArchived ? 'This workflow is archived and read-only.' : version.isInUse ? 'This published version is in use and permanently read-only. Clone it to create a new draft.' : 'Published and archived versions are read-only. Clone this version to continue editing.'}</MessageBarBody></MessageBar> : null}

      {optionsLoading ? <Spinner size="small" label="Loading workflow designer options" /> : optionsError ? <MessageBar intent="error"><MessageBarBody>{optionsError} <Button type="button" appearance="transparent" onClick={onRetryOptions}>Retry</Button></MessageBarBody></MessageBar> : null}

      <form className="workflow-metadata-form" aria-busy={operation === 'metadata'} noValidate onSubmit={handleMetadataSave}>
        <div className="request-type-editor-section__heading request-type-editor-section__heading--actions">
          <div><h3>Bilingual metadata and request type</h3><p>The workflow code is fixed as {workflow.code}. Only a draft may rebind its form version.</p></div>
          {editable ? <Button type="submit" appearance="primary" disabled={busy || !options || (rebindChanged && !rebindAcknowledged)} icon={operation === 'metadata' ? <Spinner size="tiny" /> : undefined}>{operation === 'metadata' ? 'Saving' : 'Save metadata'}</Button> : null}
        </div>
        <div className="administration-form-grid">
          <Field label="Published request-type version" required validationState={metadataErrors.requestTypeVersionId ? 'error' : 'none'} validationMessage={metadataErrors.requestTypeVersionId}>
            <select className="request-type-select" aria-label="Bound request-type version" value={requestTypeVersionId} disabled={!editable || busy || !options} onChange={(event) => { setRequestTypeVersionId(event.target.value); setRebindAcknowledged(false) }}>
              {!options?.requestTypeVersions.some((item) => item.requestTypeVersionId === requestTypeVersionId) ? <option value={requestTypeVersionId}>{version.requestTypeCode} · v{version.requestTypeVersionNumber} (current)</option> : null}
              {options?.requestTypeVersions.map((item) => <option key={item.requestTypeVersionId} value={item.requestTypeVersionId}>{item.nameEnglish} ({item.requestTypeCode} · v{item.versionNumber})</option>)}
            </select>
          </Field>
          <Field label="Navigation order" required validationState={metadataErrors.navigationOrder ? 'error' : 'none'} validationMessage={metadataErrors.navigationOrder}><Input type="number" min={0} max={100000} step={1} value={navigationOrder} readOnly={!editable} disabled={busy} onChange={(_, data) => setNavigationOrder(data.value)} /></Field>
          <Field label="English name" required validationState={metadataErrors.nameEnglish ? 'error' : 'none'} validationMessage={metadataErrors.nameEnglish}><Input value={nameEnglish} maxLength={200} readOnly={!editable} disabled={busy} onChange={(_, data) => setNameEnglish(data.value)} /></Field>
          <Field label="Arabic name" required validationState={metadataErrors.nameArabic ? 'error' : 'none'} validationMessage={metadataErrors.nameArabic}><Input dir="rtl" lang="ar" value={nameArabic} maxLength={200} readOnly={!editable} disabled={busy} onChange={(_, data) => setNameArabic(data.value)} /></Field>
          <Field label="English description" required validationState={metadataErrors.descriptionEnglish ? 'error' : 'none'} validationMessage={metadataErrors.descriptionEnglish}><Textarea value={descriptionEnglish} maxLength={2000} resize="vertical" readOnly={!editable} disabled={busy} onChange={(_, data) => setDescriptionEnglish(data.value)} /></Field>
          <Field label="Arabic description" required validationState={metadataErrors.descriptionArabic ? 'error' : 'none'} validationMessage={metadataErrors.descriptionArabic}><Textarea dir="rtl" lang="ar" value={descriptionArabic} maxLength={2000} resize="vertical" readOnly={!editable} disabled={busy} onChange={(_, data) => setDescriptionArabic(data.value)} /></Field>
          <Field label="English navigation label" required validationState={metadataErrors.navigationLabelEnglish ? 'error' : 'none'} validationMessage={metadataErrors.navigationLabelEnglish}><Input value={navigationLabelEnglish} maxLength={200} readOnly={!editable} disabled={busy} onChange={(_, data) => setNavigationLabelEnglish(data.value)} /></Field>
          <Field label="Arabic navigation label" required validationState={metadataErrors.navigationLabelArabic ? 'error' : 'none'} validationMessage={metadataErrors.navigationLabelArabic}><Input dir="rtl" lang="ar" value={navigationLabelArabic} maxLength={200} readOnly={!editable} disabled={busy} onChange={(_, data) => setNavigationLabelArabic(data.value)} /></Field>
          <Field label="Navigation slug" required validationState={metadataErrors.navigationSlug ? 'error' : 'none'} validationMessage={metadataErrors.navigationSlug}><Input value={navigationSlug} maxLength={200} readOnly={!editable} disabled={busy} onChange={(_, data) => setNavigationSlug(data.value)} /></Field>
        </div>
        {editable && rebindChanged ? <MessageBar intent="warning"><MessageBarBody>Changing the request-type version clears the existing custom-field permission matrix. Step and transition definitions remain, but every custom-field permission must be recreated and reviewed.<Checkbox label="I understand that rebinding clears the field-permission matrix" checked={rebindAcknowledged} disabled={busy} onChange={(_, data) => setRebindAcknowledged(data.checked === true)} /></MessageBarBody></MessageBar> : null}
      </form>

      <section className="request-type-editor-section">
        <div className="request-type-editor-section__heading request-type-editor-section__heading--actions"><div><h3>Starter roles</h3><p>Only users in these active custom roles may start this workflow later.</p></div>{editable ? <Button type="button" appearance="primary" disabled={busy || !options} onClick={() => void handleStarterRolesSave()}>{operation === 'starterRoles' ? 'Saving' : 'Save starter roles'}</Button> : null}</div>
        {options?.roles.length ? <div className="administration-checkbox-list" role="group" aria-label="Workflow starter roles">{options.roles.map((role) => <Checkbox key={role.id} label={`${role.nameEnglish} (${role.code})`} checked={starterRoleIds.includes(role.id)} disabled={!editable || busy} onChange={(_, data) => setStarterRoleIds((current) => data.checked === true ? [...new Set([...current, role.id])] : current.filter((id) => id !== role.id))} />)}</div> : <p className="administration-dialog__empty-copy">No active custom roles are available.</p>}
      </section>

      <section className="request-type-editor-section">
        <div className="request-type-editor-section__heading"><h3>Fixed system-field behavior</h3><p>These permissions are server-controlled and never enter the editable custom-field matrix.</p></div>
        {options?.systemFields.length ? <div className="workflow-system-fields">{options.systemFields.map((field) => <article key={field.key} className="request-type-system-field"><div><h4>{field.labelEnglish}</h4><p dir="rtl" lang="ar">{field.labelArabic}</p></div><code>{field.key}</code><span>{field.dataType}</span><span>Data: {getCatalogueLabel(options.fieldAccessModes, field.access)}{field.documentAccess ? ` · Documents: ${getCatalogueLabel(options.documentAccessModes, field.documentAccess)}` : ''}{field.canSelectForForwarding ? ' · Forwarding selectable' : ''}</span></article>)}</div> : <p className="administration-dialog__empty-copy">System-field behavior is unavailable until options load.</p>}
      </section>

      <section className="request-type-editor-section">
        <div className="request-type-editor-section__heading request-type-editor-section__heading--actions"><div><h3>Steps and graph coordinates</h3><p>{sortedSteps.length} step{sortedSteps.length === 1 ? '' : 's'}. Coordinates and transition targets make branches and loops explicit.</p></div>{editable ? <Button type="button" appearance="primary" icon={<Add20Regular />} disabled={busy || !options} onClick={(event) => { editorTriggerRef.current = event.currentTarget; setStepDialog('new') }}>Add step</Button> : null}</div>
        {sortedSteps.length === 0 ? <div className="request-type-fields-empty" role="status"><h4>No steps</h4><p>Add a start step and assign active custom roles.</p></div> : <div className="data-table-scroll" tabIndex={0}><table className="data-table workflow-steps-table"><caption className="visually-hidden">Workflow steps and coordinates</caption><thead><tr><th scope="col">Step</th><th scope="col">Roles</th><th scope="col">Position</th><th scope="col">Capabilities</th><th scope="col">Status</th><th scope="col"><span className="visually-hidden">Actions</span></th></tr></thead><tbody>{sortedSteps.map((step) => <tr key={step.id}><td><span className="data-table__primary">{step.nameEnglish}</span><span className="data-table__secondary" dir="rtl" lang="ar">{step.nameArabic}</span><code>{step.key}</code></td><td>{step.roles.map((role) => role.nameEnglish).join(', ') || 'Unassigned'}</td><td>Order {step.sortOrder}<span className="data-table__secondary">X {step.diagramX} · Y {step.diagramY}</span></td><td><details className="workflow-capability-details"><summary>{getCatalogueLabel(options?.commentPolicies ?? [], step.commentPolicy)} comments</summary><ul><li>{step.canEditRequestData ? 'Edit request data' : 'No request-data editing'}</li><li>{step.canOpenDocuments ? 'Open documents' : 'Cannot open documents'}</li><li>{step.canEditDocuments ? 'Edit documents' : 'No document editing'}</li><li>{step.canForwardDocuments ? 'Forward documents' : 'No document forwarding'}</li><li>{step.fieldPermissions.length} custom field permissions</li></ul></details></td><td><div className="workflow-status-stack">{step.isStartStep ? <StatusBadge status="Start" /> : null}<StatusBadge status={step.isActive ? 'Active' : 'Inactive'} /></div></td><td className="data-table__action"><div className="administration-table__actions"><Button type="button" appearance="subtle" size="small" icon={<Grid20Regular />} onClick={(event) => { editorTriggerRef.current = event.currentTarget; setMatrixStep(step) }}>{editable ? 'Matrix' : 'View matrix'}</Button>{editable ? <><Button type="button" appearance="subtle" size="small" icon={<Edit20Regular />} disabled={busy} onClick={(event) => { editorTriggerRef.current = event.currentTarget; setStepDialog(step) }}>Edit</Button><Button type="button" appearance="subtle" size="small" className="administration-danger-button" icon={<Delete20Regular />} disabled={busy} onClick={(event) => { confirmationTriggerRef.current = event.currentTarget; setDeleteTarget({ kind: 'step', item: step }) }}>Delete</Button></> : null}</div></td></tr>)}</tbody></table></div>}
      </section>

      <section className="request-type-editor-section">
        <div className="request-type-editor-section__heading request-type-editor-section__heading--actions"><div><h3>Transitions and actions</h3><p>Targets may be later, earlier, the same step, or terminal. This supports branches and loops without assuming a straight line.</p></div>{editable ? <Button type="button" appearance="primary" icon={<Add20Regular />} disabled={busy || sortedSteps.length === 0 || !options} onClick={(event) => { editorTriggerRef.current = event.currentTarget; setTransitionDialog('new') }}>Add transition</Button> : null}</div>
        {sortedTransitions.length === 0 ? <div className="request-type-fields-empty" role="status"><h4>No transitions</h4><p>Add actions between steps and at least one terminal outcome.</p></div> : <div className="workflow-transition-list">{sortedTransitions.map((transition) => { const source = version.steps.find((step) => step.id === transition.sourceStepId); const target = transition.targetStepId ? version.steps.find((step) => step.id === transition.targetStepId) : null; const loop = transitionCreatesLoop(transition, version.transitions); return <article key={transition.id} className="workflow-transition-card"><div className="workflow-transition-card__route"><span>{source?.nameEnglish ?? 'Missing source'}</span><strong aria-label="routes to">→</strong><span>{target?.nameEnglish ?? `Terminal: ${transition.terminalOutcome ?? 'unset'}`}</span></div><div><h4>{transition.actionLabelEnglish}</h4><p dir="rtl" lang="ar">{transition.actionLabelArabic}</p><code>{transition.key}</code></div><dl><div><dt>Action</dt><dd>{getCatalogueLabel(options?.actionTypes ?? [], transition.actionType)}</dd></div><div><dt>Status</dt><dd>{getCatalogueLabel(options?.resultingStatuses ?? [], transition.resultingStatus)}</dd></div><div><dt>Comment</dt><dd>{transition.requiresComment ? 'Required' : 'Not required'}</dd></div><div><dt>Order</dt><dd>{transition.sortOrder}</dd></div></dl><div className="workflow-transition-card__actions">{loop ? <StatusBadge status={target?.id === source?.id ? 'Self loop' : 'Loop / return'} /> : null}<StatusBadge status={transition.isActive ? 'Active' : 'Inactive'} />{editable ? <><Button type="button" appearance="subtle" size="small" icon={<Edit20Regular />} onClick={(event) => { editorTriggerRef.current = event.currentTarget; setTransitionDialog(transition) }}>Edit</Button><Button type="button" appearance="subtle" size="small" className="administration-danger-button" icon={<Delete20Regular />} onClick={(event) => { confirmationTriggerRef.current = event.currentTarget; setDeleteTarget({ kind: 'transition', item: transition }) }}>Delete</Button></> : null}</div></article> })}</div>}
      </section>

      {stepDialog && options ? <WorkflowStepDialog key={stepDialog === 'new' ? 'new-step' : stepDialog.id} workflowId={workflow.id} version={version} step={stepDialog === 'new' ? null : stepDialog} roles={options.roles} commentPolicies={options.commentPolicies} onClose={closeEditor} onSaved={(saved) => { setStepDialog(null); onReload(saved.id) }} /> : null}
      {transitionDialog && options ? <WorkflowTransitionDialog key={transitionDialog === 'new' ? 'new-transition' : transitionDialog.id} workflowId={workflow.id} version={version} transition={transitionDialog === 'new' ? null : transitionDialog} steps={version.steps} actionTypes={options.actionTypes} resultingStatuses={options.resultingStatuses} terminalOutcomes={options.terminalOutcomes} onClose={closeEditor} onSaved={(saved) => { setTransitionDialog(null); onReload(saved.id) }} /> : null}
      {matrixStep && options ? <WorkflowPermissionMatrixDialog key={`${matrixStep.id}:${matrixStep.rowVersion}`} workflowId={workflow.id} version={version} step={matrixStep} requestFields={version.requestFields} fieldAccessModes={options.fieldAccessModes} documentAccessModes={options.documentAccessModes} readOnly={!editable} onClose={closeEditor} onSaved={(saved) => { setMatrixStep(null); onReload(saved.id) }} /> : null}

      <Dialog open={pendingAction !== null} onOpenChange={(_, data) => !data.open && closeAction()}><DialogSurface className="administration-dialog"><DialogBody><DialogTitle>{pendingAction ? actionCopy[pendingAction].title : ''}</DialogTitle><DialogContent className="administration-dialog__content"><p>{pendingAction ? actionCopy[pendingAction].description : ''}</p>{operationError ? <MessageBar intent="error"><MessageBarBody>{operationError}{conflict ? ' Reload the designer before trying again.' : ''}</MessageBarBody></MessageBar> : null}</DialogContent><DialogActions><Button type="button" appearance="secondary" disabled={operation === 'lifecycle'} onClick={closeAction}>Cancel</Button><Button type="button" appearance="primary" disabled={operation === 'lifecycle'} icon={operation === 'lifecycle' ? <Spinner size="tiny" /> : undefined} onClick={() => void handleLifecycleAction()}>{operation === 'lifecycle' ? 'Working' : pendingAction ? actionCopy[pendingAction].button : 'Continue'}</Button></DialogActions></DialogBody></DialogSurface></Dialog>

      <Dialog open={deleteTarget !== null} onOpenChange={(_, data) => !data.open && closeDelete()}><DialogSurface className="administration-dialog"><DialogBody><DialogTitle>Delete {deleteTarget?.kind}?</DialogTitle><DialogContent className="administration-dialog__content"><p>{getDeleteTargetName(deleteTarget)} will be removed from this draft. Referenced or non-draft graph items cannot be deleted.</p>{operationError ? <MessageBar intent="error"><MessageBarBody>{operationError}{conflict ? ' Reload the designer before trying again.' : ''}</MessageBarBody></MessageBar> : null}</DialogContent><DialogActions><Button type="button" appearance="secondary" disabled={operation === 'delete'} onClick={closeDelete}>Cancel</Button><Button type="button" appearance="primary" disabled={operation === 'delete'} icon={operation === 'delete' ? <Spinner size="tiny" /> : undefined} onClick={() => void handleDelete()}>{operation === 'delete' ? 'Deleting' : 'Delete'}</Button></DialogActions></DialogBody></DialogSurface></Dialog>
    </div>
  )
}
