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
import { useEffect, useRef, useState } from 'react'
import type {
  WorkflowDetail,
  WorkflowRequestTypeVersionOption,
} from '../../../models'
import { ApiError, workflowAdminService } from '../../../services'
import { getWorkflowApiErrorMessage } from './workflowUi'

interface CreateWorkflowDialogProps {
  readonly requestTypeVersions: readonly WorkflowRequestTypeVersionOption[]
  readonly optionsLoading: boolean
  readonly optionsError: string | null
  readonly onRetryOptions: () => void
  readonly onClose: () => void
  readonly onSaved: (workflow: WorkflowDetail) => void
}

interface FormErrors {
  code?: string
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

export function CreateWorkflowDialog({
  requestTypeVersions,
  optionsLoading,
  optionsError,
  onRetryOptions,
  onClose,
  onSaved,
}: CreateWorkflowDialogProps) {
  const [code, setCode] = useState('')
  const [requestTypeVersionId, setRequestTypeVersionId] = useState('')
  const [nameEnglish, setNameEnglish] = useState('')
  const [nameArabic, setNameArabic] = useState('')
  const [descriptionEnglish, setDescriptionEnglish] = useState('')
  const [descriptionArabic, setDescriptionArabic] = useState('')
  const [navigationLabelEnglish, setNavigationLabelEnglish] = useState('')
  const [navigationLabelArabic, setNavigationLabelArabic] = useState('')
  const [navigationSlug, setNavigationSlug] = useState('')
  const [navigationOrder, setNavigationOrder] = useState('0')
  const [errors, setErrors] = useState<FormErrors>({})
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

  const validate = (): FormErrors => {
    const next: FormErrors = {}
    if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$/.test(code.trim())) {
      next.code =
        'Enter a stable code using up to 64 letters, numbers, periods, underscores, or hyphens.'
    }
    if (!requestTypeVersionId) {
      next.requestTypeVersionId = 'Select a published request-type version.'
    }
    if (!nameEnglish.trim()) next.nameEnglish = 'Enter the English name.'
    if (!nameArabic.trim()) next.nameArabic = 'Enter the Arabic name.'
    if (!descriptionEnglish.trim()) {
      next.descriptionEnglish = 'Enter the English description.'
    }
    if (!descriptionArabic.trim()) {
      next.descriptionArabic = 'Enter the Arabic description.'
    }
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

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (requestRef.current || optionsLoading || optionsError) return

    const nextErrors = validate()
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) return

    const controller = new AbortController()
    requestRef.current = controller
    setSubmitting(true)
    setSubmissionError(null)
    setConflict(false)

    try {
      const created = await workflowAdminService.createWorkflow(
        {
          code: code.trim(),
          requestTypeVersionId,
          nameEnglish: nameEnglish.trim(),
          nameArabic: nameArabic.trim(),
          descriptionEnglish: descriptionEnglish.trim(),
          descriptionArabic: descriptionArabic.trim(),
          navigationLabelEnglish: navigationLabelEnglish.trim(),
          navigationLabelArabic: navigationLabelArabic.trim(),
          navigationSlug: navigationSlug.trim(),
          navigationOrder: Number(navigationOrder),
        },
        controller.signal,
      )
      if (!controller.signal.aborted && mountedRef.current) onSaved(created)
    } catch (error: unknown) {
      if (!controller.signal.aborted && mountedRef.current) {
        setConflict(error instanceof ApiError && error.status === 409)
        setSubmissionError(
          getWorkflowApiErrorMessage(
            error,
            'The workflow could not be created. Please try again.',
          ),
        )
      }
    } finally {
      if (requestRef.current === controller) requestRef.current = null
      if (mountedRef.current) setSubmitting(false)
    }
  }

  const fieldState = (message?: string) => (message ? 'error' : 'none')

  return (
    <Dialog open onOpenChange={(_, data) => !data.open && !submitting && onClose()}>
      <DialogSurface className="administration-dialog administration-dialog--wide">
        <form aria-busy={submitting} noValidate onSubmit={handleSubmit}>
          <DialogBody>
            <DialogTitle>Create approval workflow</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p className="administration-dialog__introduction">
                Create version 1 as a draft and bind it to an eligible published
                request-type version. Runtime request processing is not enabled here.
              </p>
              {submissionError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>
                    {submissionError}
                    {conflict ? ' Review existing workflows before trying again.' : ''}
                  </MessageBarBody>
                </MessageBar>
              ) : null}
              {optionsLoading ? (
                <Spinner size="small" label="Loading workflow options" />
              ) : optionsError ? (
                <MessageBar intent="error">
                  <MessageBarBody>
                    {optionsError}{' '}
                    <Button type="button" appearance="transparent" onClick={onRetryOptions}>
                      Retry
                    </Button>
                  </MessageBarBody>
                </MessageBar>
              ) : requestTypeVersions.length === 0 ? (
                <MessageBar intent="warning">
                  <MessageBarBody>
                    Publish a request-type version before creating a workflow.
                  </MessageBarBody>
                </MessageBar>
              ) : null}
              <div className="administration-form-grid">
                <Field
                  label="Stable code"
                  required
                  validationState={fieldState(errors.code)}
                  validationMessage={errors.code}
                >
                  <Input
                    autoFocus
                    value={code}
                    maxLength={64}
                    disabled={submitting}
                    onChange={(_, data) => setCode(data.value)}
                  />
                </Field>
                <Field
                  label="Published request-type version"
                  required
                  validationState={fieldState(errors.requestTypeVersionId)}
                  validationMessage={errors.requestTypeVersionId}
                >
                  <select
                    className="request-type-select"
                    aria-label="Published request-type version"
                    value={requestTypeVersionId}
                    disabled={submitting || optionsLoading || !!optionsError}
                    onChange={(event) => setRequestTypeVersionId(event.target.value)}
                  >
                    <option value="">Select a request type</option>
                    {requestTypeVersions.map((option) => (
                      <option
                        key={option.requestTypeVersionId}
                        value={option.requestTypeVersionId}
                      >
                        {option.nameEnglish} ({option.requestTypeCode} · v
                        {option.versionNumber})
                      </option>
                    ))}
                  </select>
                </Field>
                <Field
                  label="English name"
                  required
                  validationState={fieldState(errors.nameEnglish)}
                  validationMessage={errors.nameEnglish}
                >
                  <Input value={nameEnglish} maxLength={200} disabled={submitting} onChange={(_, data) => setNameEnglish(data.value)} />
                </Field>
                <Field
                  label="Arabic name"
                  required
                  validationState={fieldState(errors.nameArabic)}
                  validationMessage={errors.nameArabic}
                >
                  <Input dir="rtl" lang="ar" value={nameArabic} maxLength={200} disabled={submitting} onChange={(_, data) => setNameArabic(data.value)} />
                </Field>
                <Field
                  label="English description"
                  required
                  validationState={fieldState(errors.descriptionEnglish)}
                  validationMessage={errors.descriptionEnglish}
                >
                  <Textarea value={descriptionEnglish} maxLength={2000} resize="vertical" disabled={submitting} onChange={(_, data) => setDescriptionEnglish(data.value)} />
                </Field>
                <Field
                  label="Arabic description"
                  required
                  validationState={fieldState(errors.descriptionArabic)}
                  validationMessage={errors.descriptionArabic}
                >
                  <Textarea dir="rtl" lang="ar" value={descriptionArabic} maxLength={2000} resize="vertical" disabled={submitting} onChange={(_, data) => setDescriptionArabic(data.value)} />
                </Field>
                <Field
                  label="English navigation label"
                  required
                  validationState={fieldState(errors.navigationLabelEnglish)}
                  validationMessage={errors.navigationLabelEnglish}
                >
                  <Input value={navigationLabelEnglish} maxLength={200} disabled={submitting} onChange={(_, data) => setNavigationLabelEnglish(data.value)} />
                </Field>
                <Field
                  label="Arabic navigation label"
                  required
                  validationState={fieldState(errors.navigationLabelArabic)}
                  validationMessage={errors.navigationLabelArabic}
                >
                  <Input dir="rtl" lang="ar" value={navigationLabelArabic} maxLength={200} disabled={submitting} onChange={(_, data) => setNavigationLabelArabic(data.value)} />
                </Field>
                <Field
                  label="Navigation slug"
                  required
                  validationState={fieldState(errors.navigationSlug)}
                  validationMessage={errors.navigationSlug}
                >
                  <Input value={navigationSlug} maxLength={200} placeholder="annual-leave" disabled={submitting} onChange={(_, data) => setNavigationSlug(data.value)} />
                </Field>
                <Field
                  label="Navigation order"
                  required
                  validationState={fieldState(errors.navigationOrder)}
                  validationMessage={errors.navigationOrder}
                >
                  <Input type="number" min={0} max={100000} step={1} value={navigationOrder} disabled={submitting} onChange={(_, data) => setNavigationOrder(data.value)} />
                </Field>
              </div>
            </DialogContent>
            <DialogActions>
              <Button type="button" appearance="secondary" disabled={submitting} onClick={onClose}>Cancel</Button>
              <Button
                type="submit"
                appearance="primary"
                disabled={submitting || optionsLoading || !!optionsError || requestTypeVersions.length === 0}
                icon={submitting ? <Spinner size="tiny" /> : undefined}
              >
                {submitting ? 'Creating' : 'Create draft'}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
