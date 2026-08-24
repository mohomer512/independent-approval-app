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
  WorkflowActionType,
  WorkflowCatalogueOption,
  WorkflowResultingStatus,
  WorkflowStep,
  WorkflowTerminalOutcome,
  WorkflowTransition,
  WorkflowVersion,
} from '../../../models'
import { ApiError, workflowAdminService } from '../../../services'
import { getWorkflowApiErrorMessage } from './workflowUi'

interface WorkflowTransitionDialogProps {
  readonly workflowId: string
  readonly version: WorkflowVersion
  readonly transition: WorkflowTransition | null
  readonly steps: readonly WorkflowStep[]
  readonly actionTypes: readonly WorkflowCatalogueOption<WorkflowActionType>[]
  readonly resultingStatuses: readonly WorkflowCatalogueOption<WorkflowResultingStatus>[]
  readonly terminalOutcomes: readonly WorkflowCatalogueOption<WorkflowTerminalOutcome>[]
  readonly onClose: () => void
  readonly onSaved: (version: WorkflowVersion) => void
}

interface TransitionErrors {
  key?: string
  sourceStepId?: string
  target?: string
  actionLabelEnglish?: string
  actionLabelArabic?: string
  sortOrder?: string
}

const terminalTargetValue = '__terminal__'

export function WorkflowTransitionDialog({
  workflowId,
  version,
  transition,
  steps,
  actionTypes,
  resultingStatuses,
  terminalOutcomes,
  onClose,
  onSaved,
}: WorkflowTransitionDialogProps) {
  const [key, setKey] = useState(transition?.key ?? '')
  const [sourceStepId, setSourceStepId] = useState(transition?.sourceStepId ?? '')
  const [targetValue, setTargetValue] = useState(
    transition ? transition.targetStepId ?? terminalTargetValue : '',
  )
  const [actionLabelEnglish, setActionLabelEnglish] = useState(
    transition?.actionLabelEnglish ?? '',
  )
  const [actionLabelArabic, setActionLabelArabic] = useState(
    transition?.actionLabelArabic ?? '',
  )
  const [actionType, setActionType] = useState<WorkflowActionType>(
    transition?.actionType ?? actionTypes[0]?.value ?? 'submit',
  )
  const [resultingStatus, setResultingStatus] =
    useState<WorkflowResultingStatus>(
      transition?.resultingStatus ?? resultingStatuses[0]?.value ?? 'submitted',
    )
  const [requiresComment, setRequiresComment] = useState(
    transition?.requiresComment ?? false,
  )
  const [terminalOutcome, setTerminalOutcome] =
    useState<WorkflowTerminalOutcome>(
      transition?.terminalOutcome ?? terminalOutcomes[0]?.value ?? 'completed',
    )
  const [sortOrder, setSortOrder] = useState(
    String(transition?.sortOrder ?? 0),
  )
  const [isActive, setIsActive] = useState(transition?.isActive ?? true)
  const [errors, setErrors] = useState<TransitionErrors>({})
  const [submissionError, setSubmissionError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const requestRef = useRef<AbortController | null>(null)
  const mountedRef = useRef(true)
  const editing = transition !== null
  const terminal = targetValue === terminalTargetValue

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      requestRef.current?.abort()
      requestRef.current = null
    }
  }, [])

  const validate = (): TransitionErrors => {
    const next: TransitionErrors = {}
    if (!editing && !/^[a-z][a-z0-9_]{0,63}$/.test(key.trim())) {
      next.key =
        'Start with a lowercase letter and use up to 64 lowercase letters, numbers, or underscores.'
    }
    if (!sourceStepId) next.sourceStepId = 'Select a source step.'
    if (!targetValue) next.target = 'Select a target step or a terminal outcome.'
    if (!actionLabelEnglish.trim()) {
      next.actionLabelEnglish = 'Enter the English action label.'
    }
    if (!actionLabelArabic.trim()) {
      next.actionLabelArabic = 'Enter the Arabic action label.'
    }
    const order = Number(sortOrder)
    if (!Number.isInteger(order) || order < 0 || order > 100000) {
      next.sortOrder = 'Enter a whole number from 0 to 100,000.'
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
      sourceStepId,
      targetStepId: terminal ? null : targetValue,
      actionLabelEnglish: actionLabelEnglish.trim(),
      actionLabelArabic: actionLabelArabic.trim(),
      actionType,
      resultingStatus,
      requiresComment,
      terminalOutcome: terminal ? terminalOutcome : null,
      sortOrder: Number(sortOrder),
      isActive,
    }

    try {
      const saved = editing
        ? await workflowAdminService.updateTransition(
            workflowId,
            version.id,
            transition.id,
            {
              ...values,
              versionRowVersion: version.rowVersion,
              transitionRowVersion: transition.rowVersion,
            },
            controller.signal,
          )
        : await workflowAdminService.addTransition(
            workflowId,
            version.id,
            {
              ...values,
              key: key.trim(),
              versionRowVersion: version.rowVersion,
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
            'The transition could not be saved. Please try again.',
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
            <DialogTitle>{editing ? 'Edit transition' : 'Add transition'}</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p className="administration-dialog__introduction">
                Select any step as the target, including the source or an earlier step, to represent loops and returns. Terminal actions have no target step.
              </p>
              {submissionError ? <MessageBar intent="error" politeness="assertive"><MessageBarBody>{submissionError}{conflict ? ' Reload the designer before trying again.' : ''}</MessageBarBody></MessageBar> : null}
              <div className="administration-form-grid">
                <Field label="Stable key" required validationState={errors.key ? 'error' : 'none'} validationMessage={errors.key}>
                  <Input autoFocus={!editing} value={key} maxLength={64} readOnly={editing} disabled={submitting} onChange={(_, data) => setKey(data.value)} />
                </Field>
                <Field label="Action type" required>
                  <select className="request-type-select" aria-label="Transition action type" value={actionType} disabled={submitting} onChange={(event) => setActionType(event.target.value as WorkflowActionType)}>
                    {actionTypes.map((option) => <option key={option.value} value={option.value}>{option.labelEnglish}</option>)}
                  </select>
                </Field>
                <Field label="Source step" required validationState={errors.sourceStepId ? 'error' : 'none'} validationMessage={errors.sourceStepId}>
                  <select className="request-type-select" aria-label="Transition source step" value={sourceStepId} disabled={submitting} onChange={(event) => setSourceStepId(event.target.value)}>
                    <option value="">Select source</option>
                    {steps.map((step) => <option key={step.id} value={step.id}>{step.nameEnglish} ({step.key})</option>)}
                  </select>
                </Field>
                <Field label="Target" required validationState={errors.target ? 'error' : 'none'} validationMessage={errors.target}>
                  <select className="request-type-select" aria-label="Transition target" value={targetValue} disabled={submitting} onChange={(event) => { const value = event.target.value; setTargetValue(value); if (value === terminalTargetValue) setResultingStatus(terminalOutcome) }}>
                    <option value="">Select target</option>
                    {steps.map((step) => <option key={step.id} value={step.id}>{step.nameEnglish} ({step.key}){step.id === sourceStepId ? ' — self-loop' : ''}</option>)}
                    <option value={terminalTargetValue}>Terminal outcome</option>
                  </select>
                </Field>
                <Field label="English action label" required validationState={errors.actionLabelEnglish ? 'error' : 'none'} validationMessage={errors.actionLabelEnglish}>
                  <Input value={actionLabelEnglish} maxLength={200} disabled={submitting} onChange={(_, data) => setActionLabelEnglish(data.value)} />
                </Field>
                <Field label="Arabic action label" required validationState={errors.actionLabelArabic ? 'error' : 'none'} validationMessage={errors.actionLabelArabic}>
                  <Input dir="rtl" lang="ar" value={actionLabelArabic} maxLength={200} disabled={submitting} onChange={(_, data) => setActionLabelArabic(data.value)} />
                </Field>
                <Field label="Resulting status" required>
                  <select className="request-type-select" aria-label="Transition resulting status" value={resultingStatus} disabled={submitting || terminal} onChange={(event) => setResultingStatus(event.target.value as WorkflowResultingStatus)}>
                    {resultingStatuses.map((option) => <option key={option.value} value={option.value}>{option.labelEnglish}</option>)}
                  </select>
                </Field>
                {terminal ? (
                  <Field label="Terminal outcome" required>
                    <select className="request-type-select" aria-label="Transition terminal outcome" value={terminalOutcome} disabled={submitting} onChange={(event) => { const outcome = event.target.value as WorkflowTerminalOutcome; setTerminalOutcome(outcome); setResultingStatus(outcome) }}>
                      {terminalOutcomes.map((option) => <option key={option.value} value={option.value}>{option.labelEnglish}</option>)}
                    </select>
                  </Field>
                ) : null}
                <Field label="Sort order" required validationState={errors.sortOrder ? 'error' : 'none'} validationMessage={errors.sortOrder}>
                  <Input type="number" min={0} max={100000} step={1} value={sortOrder} disabled={submitting} onChange={(_, data) => setSortOrder(data.value)} />
                </Field>
              </div>
              <div className="workflow-capability-grid" role="group" aria-label="Transition behavior">
                <Checkbox label="Comment required" checked={requiresComment} disabled={submitting} onChange={(_, data) => setRequiresComment(data.checked === true)} />
                <Checkbox label="Active" checked={isActive} disabled={submitting} onChange={(_, data) => setIsActive(data.checked === true)} />
              </div>
            </DialogContent>
            <DialogActions>
              <Button type="button" appearance="secondary" disabled={submitting} onClick={onClose}>Cancel</Button>
              <Button type="submit" appearance="primary" disabled={submitting || steps.length === 0 || actionTypes.length === 0 || resultingStatuses.length === 0} icon={submitting ? <Spinner size="tiny" /> : undefined}>{submitting ? 'Saving' : editing ? 'Save transition' : 'Add transition'}</Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
