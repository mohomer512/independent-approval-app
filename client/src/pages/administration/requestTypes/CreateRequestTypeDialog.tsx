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
import type { RequestTypeDetail } from '../../../models'
import {
  ApiError,
  requestTypeAdminService,
} from '../../../services'
import { getRequestTypeApiErrorMessage } from './requestTypeUi'

interface CreateRequestTypeDialogProps {
  readonly onClose: () => void
  readonly onSaved: (requestType: RequestTypeDetail) => void
}

interface FormErrors {
  code?: string
  nameEnglish?: string
  nameArabic?: string
  descriptionEnglish?: string
  descriptionArabic?: string
  requestPrefix?: string
  navigationSlug?: string
  navigationOrder?: string
}

export function CreateRequestTypeDialog({
  onClose,
  onSaved,
}: CreateRequestTypeDialogProps) {
  const [code, setCode] = useState('')
  const [nameEnglish, setNameEnglish] = useState('')
  const [nameArabic, setNameArabic] = useState('')
  const [descriptionEnglish, setDescriptionEnglish] = useState('')
  const [descriptionArabic, setDescriptionArabic] = useState('')
  const [requestPrefix, setRequestPrefix] = useState('')
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
    const nextErrors: FormErrors = {}

    if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$/.test(code.trim())) {
      nextErrors.code =
        'Enter a stable code using letters, numbers, periods, underscores, or hyphens.'
    }

    if (!nameEnglish.trim()) {
      nextErrors.nameEnglish = 'Enter the English request type name.'
    }

    if (!nameArabic.trim()) {
      nextErrors.nameArabic = 'Enter the Arabic request type name.'
    }

    if (!descriptionEnglish.trim()) {
      nextErrors.descriptionEnglish = 'Enter the English description.'
    }

    if (!descriptionArabic.trim()) {
      nextErrors.descriptionArabic = 'Enter the Arabic description.'
    }

    if (!/^[A-Za-z][A-Za-z0-9]{0,11}$/.test(requestPrefix.trim())) {
      nextErrors.requestPrefix =
        'Start with a letter and use up to 12 letters or numbers.'
    }

    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(navigationSlug.trim())) {
      nextErrors.navigationSlug =
        'Use lowercase letters, numbers, and single hyphens.'
    }

    const order = Number(navigationOrder)
    if (!Number.isInteger(order) || order < 0 || order > 100000) {
      nextErrors.navigationOrder = 'Enter a whole number from 0 to 100,000.'
    }

    return nextErrors
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (requestRef.current) {
      return
    }

    const nextErrors = validate()
    setErrors(nextErrors)

    if (Object.keys(nextErrors).length > 0) {
      return
    }

    const controller = new AbortController()
    requestRef.current = controller
    setSubmitting(true)
    setSubmissionError(null)
    setConflict(false)

    try {
      const created = await requestTypeAdminService.createRequestType(
        {
          code: code.trim(),
          nameEnglish: nameEnglish.trim(),
          nameArabic: nameArabic.trim(),
          descriptionEnglish: descriptionEnglish.trim(),
          descriptionArabic: descriptionArabic.trim(),
          requestPrefix: requestPrefix.trim().toUpperCase(),
          navigationSlug: navigationSlug.trim(),
          navigationOrder: Number(navigationOrder),
        },
        controller.signal,
      )

      if (!controller.signal.aborted && mountedRef.current) {
        onSaved(created)
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted && mountedRef.current) {
        setConflict(error instanceof ApiError && error.status === 409)
        setSubmissionError(
          getRequestTypeApiErrorMessage(
            error,
            'The request type could not be created. Please try again.',
          ),
        )
      }
    } finally {
      if (requestRef.current === controller) {
        requestRef.current = null
        if (mountedRef.current) {
          setSubmitting(false)
        }
      }
    }
  }

  return (
    <Dialog
      open
      onOpenChange={(_, data) => {
        if (!data.open && !submitting) {
          onClose()
        }
      }}
    >
      <DialogSurface className="administration-dialog administration-dialog--wide">
        <form aria-busy={submitting} noValidate onSubmit={handleSubmit}>
          <DialogBody>
            <DialogTitle>Create request type</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p className="administration-dialog__introduction">
                This creates version 1 as a draft. Its code is permanent after
                creation; metadata and custom fields remain editable until publish.
              </p>

              {submissionError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>
                    {submissionError}
                    {conflict
                      ? ' Review the existing request types before trying again.'
                      : ''}
                  </MessageBarBody>
                </MessageBar>
              ) : null}

              <div className="administration-form-grid">
                <Field
                  label="Stable code"
                  required
                  validationState={errors.code ? 'error' : 'none'}
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
                  label="Request-number prefix"
                  required
                  hint="Used in numbers such as LEAVE-2026-000001."
                  validationState={errors.requestPrefix ? 'error' : 'none'}
                  validationMessage={errors.requestPrefix}
                >
                  <Input
                    value={requestPrefix}
                    maxLength={12}
                    disabled={submitting}
                    onChange={(_, data) => setRequestPrefix(data.value)}
                  />
                </Field>
                <Field
                  label="English name"
                  required
                  validationState={errors.nameEnglish ? 'error' : 'none'}
                  validationMessage={errors.nameEnglish}
                >
                  <Input
                    value={nameEnglish}
                    maxLength={200}
                    disabled={submitting}
                    onChange={(_, data) => setNameEnglish(data.value)}
                  />
                </Field>
                <Field
                  label="Arabic name"
                  required
                  validationState={errors.nameArabic ? 'error' : 'none'}
                  validationMessage={errors.nameArabic}
                >
                  <Input
                    dir="rtl"
                    lang="ar"
                    value={nameArabic}
                    maxLength={200}
                    disabled={submitting}
                    onChange={(_, data) => setNameArabic(data.value)}
                  />
                </Field>
                <Field
                  label="English description"
                  required
                  validationState={errors.descriptionEnglish ? 'error' : 'none'}
                  validationMessage={errors.descriptionEnglish}
                >
                  <Textarea
                    value={descriptionEnglish}
                    maxLength={2000}
                    resize="vertical"
                    disabled={submitting}
                    onChange={(_, data) => setDescriptionEnglish(data.value)}
                  />
                </Field>
                <Field
                  label="Arabic description"
                  required
                  validationState={errors.descriptionArabic ? 'error' : 'none'}
                  validationMessage={errors.descriptionArabic}
                >
                  <Textarea
                    dir="rtl"
                    lang="ar"
                    value={descriptionArabic}
                    maxLength={2000}
                    resize="vertical"
                    disabled={submitting}
                    onChange={(_, data) => setDescriptionArabic(data.value)}
                  />
                </Field>
                <Field
                  label="Navigation slug"
                  required
                  validationState={errors.navigationSlug ? 'error' : 'none'}
                  validationMessage={errors.navigationSlug}
                >
                  <Input
                    value={navigationSlug}
                    maxLength={100}
                    disabled={submitting}
                    placeholder="annual-leave"
                    onChange={(_, data) => setNavigationSlug(data.value)}
                  />
                </Field>
                <Field
                  label="Navigation order"
                  required
                  validationState={errors.navigationOrder ? 'error' : 'none'}
                  validationMessage={errors.navigationOrder}
                >
                  <Input
                    type="number"
                    min={0}
                    max={100000}
                    step={1}
                    value={navigationOrder}
                    disabled={submitting}
                    onChange={(_, data) => setNavigationOrder(data.value)}
                  />
                </Field>
              </div>
            </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                disabled={submitting}
                onClick={onClose}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                appearance="primary"
                disabled={submitting}
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
