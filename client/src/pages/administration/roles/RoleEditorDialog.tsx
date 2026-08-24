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
import { useEffect, useRef, useState } from 'react'
import type {
  ApplicationRole,
  PermissionDefinition,
} from '../../../models'
import {
  ApiError,
  adminRoleService,
  getSafeApiErrorMessage,
} from '../../../services'

interface RoleEditorDialogProps {
  readonly role: ApplicationRole | null
  readonly permissions: readonly PermissionDefinition[]
  readonly permissionsLoading: boolean
  readonly permissionsError: string | null
  readonly onRetryPermissions: () => void
  readonly onClose: () => void
  readonly onSaved: (role: ApplicationRole) => void
}

interface RoleFormErrors {
  code?: string
  nameEnglish?: string
  nameArabic?: string
}

export function RoleEditorDialog({
  role,
  permissions,
  permissionsLoading,
  permissionsError,
  onRetryPermissions,
  onClose,
  onSaved,
}: RoleEditorDialogProps) {
  const [code, setCode] = useState(role?.code ?? '')
  const [nameEnglish, setNameEnglish] = useState(role?.nameEnglish ?? '')
  const [nameArabic, setNameArabic] = useState(role?.nameArabic ?? '')
  const [descriptionEnglish, setDescriptionEnglish] = useState(
    role?.descriptionEnglish ?? '',
  )
  const [descriptionArabic, setDescriptionArabic] = useState(
    role?.descriptionArabic ?? '',
  )
  const [isActive, setIsActive] = useState(role?.isActive ?? true)
  const [permissionCodes, setPermissionCodes] = useState<readonly string[]>(
    role?.permissionCodes ?? [],
  )
  const [validationErrors, setValidationErrors] = useState<RoleFormErrors>({})
  const [submissionError, setSubmissionError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const activeRequestRef = useRef<AbortController | null>(null)
  const isMountedRef = useRef(true)
  const editing = role !== null
  const archived = role !== null && role.isArchived

  useEffect(() => {
    isMountedRef.current = true

    return () => {
      isMountedRef.current = false
      activeRequestRef.current?.abort()
      activeRequestRef.current = null
    }
  }, [])

  const validate = (): RoleFormErrors => {
    const errors: RoleFormErrors = {}
    const normalizedCode = code.trim()

    if (!editing && normalizedCode.length === 0) {
      errors.code = 'Enter a stable role code.'
    } else if (
      !editing &&
      !/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(normalizedCode)
    ) {
      errors.code =
        'Use letters, numbers, periods, underscores, or hyphens only.'
    }

    if (nameEnglish.trim().length === 0) {
      errors.nameEnglish = 'Enter the English role name.'
    }

    if (nameArabic.trim().length === 0) {
      errors.nameArabic = 'Enter the Arabic role name.'
    }

    return errors
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (activeRequestRef.current !== null || archived) {
      return
    }

    const errors = validate()
    setValidationErrors(errors)

    if (Object.keys(errors).length > 0) {
      return
    }

    const controller = new AbortController()
    activeRequestRef.current = controller
    setSubmissionError(null)
    setConflict(false)
    setSubmitting(true)

    try {
      const savedRole = editing
        ? await adminRoleService.updateRole(
            role.id,
            {
              nameEnglish: nameEnglish.trim(),
              nameArabic: nameArabic.trim(),
              descriptionEnglish: descriptionEnglish.trim(),
              descriptionArabic: descriptionArabic.trim(),
              isActive,
              permissionCodes,
              rowVersion: role.rowVersion,
            },
            controller.signal,
          )
        : await adminRoleService.createRole(
            {
              code: code.trim(),
              nameEnglish: nameEnglish.trim(),
              nameArabic: nameArabic.trim(),
              descriptionEnglish: descriptionEnglish.trim(),
              descriptionArabic: descriptionArabic.trim(),
              permissionCodes,
            },
            controller.signal,
          )

      if (!controller.signal.aborted && isMountedRef.current) {
        onSaved(savedRole)
      }
    } catch (saveError: unknown) {
      if (!controller.signal.aborted && isMountedRef.current) {
        setConflict(saveError instanceof ApiError && saveError.status === 409)
        setSubmissionError(
          getSafeApiErrorMessage(
            saveError,
            'The role could not be saved. Please try again.',
          ),
        )
      }
    } finally {
      if (activeRequestRef.current === controller) {
        activeRequestRef.current = null

        if (isMountedRef.current) {
          setSubmitting(false)
        }
      }
    }
  }

  return (
    <Dialog
      open
      onOpenChange={(_, dialogData) => {
        if (!dialogData.open && !submitting) {
          onClose()
        }
      }}
    >
      <DialogSurface className="administration-dialog administration-dialog--wide">
        <form aria-busy={submitting} noValidate onSubmit={handleSubmit}>
          <DialogBody>
            <DialogTitle>{editing ? 'Edit role' : 'Create role'}</DialogTitle>
            <DialogContent className="administration-dialog__content">
              {archived ? (
                <MessageBar intent="warning">
                  <MessageBarBody>
                    Archived roles are read-only and cannot receive new assignments.
                  </MessageBarBody>
                </MessageBar>
              ) : null}

              {submissionError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>
                    {submissionError}
                    {conflict ? (
                      <span className="administration-conflict-copy">
                        Close this form and reload the current role before editing
                        again.
                      </span>
                    ) : null}
                  </MessageBarBody>
                </MessageBar>
              ) : null}

              <div className="administration-form-grid">
                <Field
                  label="Role code"
                  required
                  hint={
                    editing
                      ? 'The normalized role code is immutable.'
                      : 'A stable code that cannot be changed later.'
                  }
                  validationMessage={validationErrors.code}
                  validationState={validationErrors.code ? 'error' : 'none'}
                >
                  <Input
                    value={code}
                    disabled={editing || submitting || archived}
                    maxLength={100}
                    onChange={(_, inputData) => {
                      setCode(inputData.value)
                      setValidationErrors((currentErrors) => ({
                        ...currentErrors,
                        code: undefined,
                      }))
                    }}
                  />
                </Field>
                <div />
                <Field
                  label="English name"
                  required
                  validationMessage={validationErrors.nameEnglish}
                  validationState={
                    validationErrors.nameEnglish ? 'error' : 'none'
                  }
                >
                  <Input
                    value={nameEnglish}
                    disabled={submitting || archived}
                    maxLength={200}
                    onChange={(_, inputData) => {
                      setNameEnglish(inputData.value)
                      setValidationErrors((currentErrors) => ({
                        ...currentErrors,
                        nameEnglish: undefined,
                      }))
                    }}
                  />
                </Field>
                <Field
                  label="Arabic name"
                  required
                  validationMessage={validationErrors.nameArabic}
                  validationState={
                    validationErrors.nameArabic ? 'error' : 'none'
                  }
                >
                  <Input
                    value={nameArabic}
                    dir="rtl"
                    disabled={submitting || archived}
                    maxLength={200}
                    onChange={(_, inputData) => {
                      setNameArabic(inputData.value)
                      setValidationErrors((currentErrors) => ({
                        ...currentErrors,
                        nameArabic: undefined,
                      }))
                    }}
                  />
                </Field>
                <Field label="English description">
                  <Textarea
                    value={descriptionEnglish}
                    disabled={submitting || archived}
                    rows={3}
                    maxLength={1000}
                    onChange={(_, textareaData) =>
                      setDescriptionEnglish(textareaData.value)
                    }
                  />
                </Field>
                <Field label="Arabic description">
                  <Textarea
                    value={descriptionArabic}
                    dir="rtl"
                    disabled={submitting || archived}
                    rows={3}
                    maxLength={1000}
                    onChange={(_, textareaData) =>
                      setDescriptionArabic(textareaData.value)
                    }
                  />
                </Field>
              </div>

              {editing ? (
                <Checkbox
                  checked={isActive}
                  disabled={submitting || archived}
                  label="Role is active"
                  onChange={(_, checkboxData) =>
                    setIsActive(checkboxData.checked === true)
                  }
                />
              ) : null}

              <section className="administration-dialog__section">
                <h3>Permissions</h3>
                <p>
                  Select from the fixed application catalogue. System
                  Administrator access is not available here.
                </p>
                {permissionsLoading ? (
                  <Spinner size="small" label="Loading permission catalogue" />
                ) : permissionsError ? (
                  <MessageBar intent="error">
                    <MessageBarBody>
                      {permissionsError}
                      <Button
                        type="button"
                        appearance="transparent"
                        onClick={onRetryPermissions}
                      >
                        Retry
                      </Button>
                    </MessageBarBody>
                  </MessageBar>
                ) : permissions.length === 0 ? (
                  <p className="administration-dialog__empty-copy">
                    No assignable permissions are available.
                  </p>
                ) : (
                  <div
                    className="administration-permission-list"
                    role="group"
                    aria-label="Role permissions"
                  >
                    {permissions.map((permission) => (
                      <div
                        className="administration-permission-option"
                        key={permission.code}
                      >
                        <Checkbox
                          checked={permissionCodes.includes(permission.code)}
                          disabled={submitting || archived}
                          label={permission.nameEnglish}
                          onChange={(_, checkboxData) => {
                            setSubmissionError(null)
                            setPermissionCodes((currentCodes) => {
                              if (checkboxData.checked === true) {
                                return currentCodes.includes(permission.code)
                                  ? currentCodes
                                  : [...currentCodes, permission.code]
                              }

                              return currentCodes.filter(
                                (codeValue) => codeValue !== permission.code,
                              )
                            })
                          }}
                        />
                        <span>{permission.code}</span>
                        <p>{permission.descriptionEnglish}</p>
                        <p dir="rtl" lang="ar">
                          {permission.nameArabic} —{' '}
                          {permission.descriptionArabic}
                        </p>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                disabled={submitting}
                onClick={onClose}
              >
                {archived ? 'Close' : 'Cancel'}
              </Button>
              {!archived ? (
                <Button
                  type="submit"
                  appearance="primary"
                  disabled={
                    submitting || permissionsLoading || permissionsError !== null
                  }
                  icon={submitting ? <Spinner size="tiny" /> : undefined}
                >
                  {submitting ? 'Saving' : editing ? 'Save changes' : 'Create role'}
                </Button>
              ) : null}
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
