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
} from '@fluentui/react-components'
import { useEffect, useRef, useState } from 'react'
import type {
  WorkflowCatalogueOption,
  WorkflowCommentPolicy,
  WorkflowRoleOption,
  WorkflowStep,
  WorkflowVersion,
} from '../../../models'
import { ApiError, workflowAdminService } from '../../../services'
import { getWorkflowApiErrorMessage } from './workflowUi'

interface WorkflowStepDialogProps {
  readonly workflowId: string
  readonly version: WorkflowVersion
  readonly step: WorkflowStep | null
  readonly roles: readonly WorkflowRoleOption[]
  readonly commentPolicies: readonly WorkflowCatalogueOption<WorkflowCommentPolicy>[]
  readonly onClose: () => void
  readonly onSaved: (version: WorkflowVersion) => void
}

interface StepErrors {
  key?: string
  nameEnglish?: string
  nameArabic?: string
  sortOrder?: string
  diagramX?: string
  diagramY?: string
}

function sameIds(left: readonly string[], right: readonly string[]): boolean {
  return [...left].sort().join('|') === [...right].sort().join('|')
}

export function WorkflowStepDialog({
  workflowId,
  version,
  step,
  roles,
  commentPolicies,
  onClose,
  onSaved,
}: WorkflowStepDialogProps) {
  const [key, setKey] = useState(step?.key ?? '')
  const [nameEnglish, setNameEnglish] = useState(step?.nameEnglish ?? '')
  const [nameArabic, setNameArabic] = useState(step?.nameArabic ?? '')
  const [sortOrder, setSortOrder] = useState(String(step?.sortOrder ?? 0))
  const [diagramX, setDiagramX] = useState(String(step?.diagramX ?? 0))
  const [diagramY, setDiagramY] = useState(String(step?.diagramY ?? 0))
  const [isStartStep, setIsStartStep] = useState(step?.isStartStep ?? false)
  const [isActive, setIsActive] = useState(step?.isActive ?? true)
  const [canEditRequestData, setCanEditRequestData] = useState(
    step?.canEditRequestData ?? false,
  )
  const [canOpenDocuments, setCanOpenDocuments] = useState(
    step?.canOpenDocuments ?? false,
  )
  const [canEditDocuments, setCanEditDocuments] = useState(
    step?.canEditDocuments ?? false,
  )
  const [canForwardDocuments, setCanForwardDocuments] = useState(
    step?.canForwardDocuments ?? false,
  )
  const [commentPolicy, setCommentPolicy] = useState<WorkflowCommentPolicy>(
    step?.commentPolicy ?? commentPolicies[0]?.value ?? 'none',
  )
  const [roleIds, setRoleIds] = useState<readonly string[]>(
    step?.roles.map((role) => role.id) ?? [],
  )
  const [errors, setErrors] = useState<StepErrors>({})
  const [submissionError, setSubmissionError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const requestRef = useRef<AbortController | null>(null)
  const mountedRef = useRef(true)
  const editing = step !== null

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      requestRef.current?.abort()
      requestRef.current = null
    }
  }, [])

  const validate = (): StepErrors => {
    const next: StepErrors = {}
    if (!editing && !/^[a-z][a-z0-9_]{0,63}$/.test(key.trim())) {
      next.key =
        'Start with a lowercase letter and use up to 64 lowercase letters, numbers, or underscores.'
    }
    if (!nameEnglish.trim()) next.nameEnglish = 'Enter the English step name.'
    if (!nameArabic.trim()) next.nameArabic = 'Enter the Arabic step name.'
    const order = Number(sortOrder)
    if (!Number.isInteger(order) || order < 0 || order > 100000) {
      next.sortOrder = 'Enter a whole number from 0 to 100,000.'
    }
    if (
      !Number.isFinite(Number(diagramX)) ||
      Math.abs(Number(diagramX)) > 100000
    ) {
      next.diagramX = 'Enter an X coordinate from -100,000 to 100,000.'
    }
    if (
      !Number.isFinite(Number(diagramY)) ||
      Math.abs(Number(diagramY)) > 100000
    ) {
      next.diagramY = 'Enter a Y coordinate from -100,000 to 100,000.'
    }
    return next
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (requestRef.current) return
    const nextErrors = validate()
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) return

    const controller = new AbortController()
    requestRef.current = controller
    setSubmitting(true)
    setSubmissionError(null)
    setConflict(false)
    const values = {
      nameEnglish: nameEnglish.trim(),
      nameArabic: nameArabic.trim(),
      sortOrder: Number(sortOrder),
      diagramX: Number(diagramX),
      diagramY: Number(diagramY),
      isStartStep,
      isActive,
      canEditRequestData,
      canOpenDocuments,
      canEditDocuments,
      canForwardDocuments,
      commentPolicy,
    }

    try {
      let saved: WorkflowVersion
      if (editing) {
        saved = await workflowAdminService.updateStep(
          workflowId,
          version.id,
          step.id,
          {
            ...values,
            versionRowVersion: version.rowVersion,
            stepRowVersion: step.rowVersion,
          },
          controller.signal,
        )

        const existingRoleIds = step.roles.map((role) => role.id)
        if (!sameIds(existingRoleIds, roleIds)) {
          const updatedStep = saved.steps.find((item) => item.id === step.id)
          if (!updatedStep) throw new Error('Updated workflow step is unavailable.')
          saved = await workflowAdminService.updateStepRoles(
            workflowId,
            saved.id,
            updatedStep.id,
            {
              roleIds,
              versionRowVersion: saved.rowVersion,
              stepRowVersion: updatedStep.rowVersion,
            },
            controller.signal,
          )
        }
      } else {
        saved = await workflowAdminService.addStep(
          workflowId,
          version.id,
          {
            ...values,
            key: key.trim(),
            roleIds,
            versionRowVersion: version.rowVersion,
          },
          controller.signal,
        )
      }

      if (!controller.signal.aborted && mountedRef.current) onSaved(saved)
    } catch (error: unknown) {
      if (!controller.signal.aborted && mountedRef.current) {
        setConflict(error instanceof ApiError && error.status === 409)
        setSubmissionError(
          getWorkflowApiErrorMessage(
            error,
            'The workflow step could not be saved. Please try again.',
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
      <DialogSurface className="administration-dialog administration-dialog--wide">
        <form aria-busy={submitting} noValidate onSubmit={handleSubmit}>
          <DialogBody>
            <DialogTitle>{editing ? 'Edit workflow step' : 'Add workflow step'}</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p className="administration-dialog__introduction">
                Coordinates keep the graph understandable without coupling the definition to a fragile visual canvas. Return transitions may point to earlier coordinates.
              </p>
              {submissionError ? (
                <MessageBar intent="error" politeness="assertive"><MessageBarBody>{submissionError}{conflict ? ' Reload the designer before trying again.' : ''}</MessageBarBody></MessageBar>
              ) : null}
              <div className="administration-form-grid">
                <Field label="Stable key" required validationState={errors.key ? 'error' : 'none'} validationMessage={errors.key}>
                  <Input autoFocus={!editing} value={key} maxLength={64} readOnly={editing} disabled={submitting} onChange={(_, data) => setKey(data.value)} />
                </Field>
                <Field label="Comment policy" required>
                  <select className="request-type-select" aria-label="Step comment policy" value={commentPolicy} disabled={submitting} onChange={(event) => setCommentPolicy(event.target.value as WorkflowCommentPolicy)}>
                    {commentPolicies.map((option) => <option key={option.value} value={option.value}>{option.labelEnglish}</option>)}
                  </select>
                </Field>
                <Field label="English name" required validationState={errors.nameEnglish ? 'error' : 'none'} validationMessage={errors.nameEnglish}>
                  <Input value={nameEnglish} maxLength={200} disabled={submitting} onChange={(_, data) => setNameEnglish(data.value)} />
                </Field>
                <Field label="Arabic name" required validationState={errors.nameArabic ? 'error' : 'none'} validationMessage={errors.nameArabic}>
                  <Input dir="rtl" lang="ar" value={nameArabic} maxLength={200} disabled={submitting} onChange={(_, data) => setNameArabic(data.value)} />
                </Field>
                <Field label="Sort order" required validationState={errors.sortOrder ? 'error' : 'none'} validationMessage={errors.sortOrder}>
                  <Input type="number" min={0} max={100000} step={1} value={sortOrder} disabled={submitting} onChange={(_, data) => setSortOrder(data.value)} />
                </Field>
                <Field label="Diagram X" required validationState={errors.diagramX ? 'error' : 'none'} validationMessage={errors.diagramX}>
                  <Input type="number" min={-100000} max={100000} step="any" value={diagramX} disabled={submitting} onChange={(_, data) => setDiagramX(data.value)} />
                </Field>
                <Field label="Diagram Y" required validationState={errors.diagramY ? 'error' : 'none'} validationMessage={errors.diagramY}>
                  <Input type="number" min={-100000} max={100000} step="any" value={diagramY} disabled={submitting} onChange={(_, data) => setDiagramY(data.value)} />
                </Field>
              </div>
              <section className="administration-dialog__section">
                <h3>Step behavior</h3>
                <div className="workflow-capability-grid" role="group" aria-label="Step capabilities">
                  <Checkbox label="Start step" checked={isStartStep} disabled={submitting} onChange={(_, data) => setIsStartStep(data.checked === true)} />
                  <Checkbox label="Active" checked={isActive} disabled={submitting} onChange={(_, data) => setIsActive(data.checked === true)} />
                  <Checkbox label="Can edit request data" checked={canEditRequestData} disabled={submitting} onChange={(_, data) => setCanEditRequestData(data.checked === true)} />
                  <Checkbox label="Can open documents" checked={canOpenDocuments} disabled={submitting} onChange={(_, data) => setCanOpenDocuments(data.checked === true)} />
                  <Checkbox label="Can edit documents" checked={canEditDocuments} disabled={submitting} onChange={(_, data) => setCanEditDocuments(data.checked === true)} />
                  <Checkbox label="Can forward documents" checked={canForwardDocuments} disabled={submitting} onChange={(_, data) => setCanForwardDocuments(data.checked === true)} />
                </div>
              </section>
              <section className="administration-dialog__section">
                <h3>Assigned roles</h3>
                {roles.length === 0 ? <p className="administration-dialog__empty-copy">No active custom roles are available.</p> : (
                  <div className="administration-checkbox-list" role="group" aria-label="Assigned step roles">
                    {roles.map((role) => (
                      <Checkbox key={role.id} label={`${role.nameEnglish} (${role.code})`} checked={roleIds.includes(role.id)} disabled={submitting} onChange={(_, data) => setRoleIds((current) => data.checked === true ? [...new Set([...current, role.id])] : current.filter((id) => id !== role.id))} />
                    ))}
                  </div>
                )}
              </section>
            </DialogContent>
            <DialogActions>
              <Button type="button" appearance="secondary" disabled={submitting} onClick={onClose}>Cancel</Button>
              <Button type="submit" appearance="primary" disabled={submitting} icon={submitting ? <Spinner size="tiny" /> : undefined}>{submitting ? 'Saving' : editing ? 'Save step' : 'Add step'}</Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
