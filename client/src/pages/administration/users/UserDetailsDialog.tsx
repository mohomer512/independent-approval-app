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
  MessageBar,
  MessageBarBody,
  Spinner,
  Textarea,
} from '@fluentui/react-components'
import { ArrowClockwise20Regular } from '@fluentui/react-icons'
import { useEffect, useMemo, useRef, useState } from 'react'
import { AdministrationResourceState } from '../../../components/administration/AdministrationResourceState'
import { StatusBadge } from '../../../components/common'
import { useAdminUser } from '../../../hooks/useAdminUser'
import type { ApplicationUser, RoleReference } from '../../../models'
import {
  ApiError,
  adminUserService,
  getSafeApiErrorMessage,
} from '../../../services'

type UserAccessAction = 'lock' | 'unlock' | 'remove' | 'restore'

interface UserDetailsDialogProps {
  readonly userId: string
  readonly availableRoles: readonly RoleReference[]
  readonly rolesLoading: boolean
  readonly rolesError: string | null
  readonly onRetryRoles: () => void
  readonly onClose: () => void
  readonly onChanged: () => void
}

function getUserStatus(user: ApplicationUser): string {
  if (user.isRemoved) {
    return 'Removed'
  }

  if (user.isLocked) {
    return 'Locked'
  }

  return user.isActive ? 'Active' : 'Inactive'
}

function formatOptionalDate(value: string | null): string {
  if (!value) {
    return 'Not recorded'
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'Not recorded' : date.toLocaleString()
}

function getActionLabel(action: UserAccessAction): string {
  switch (action) {
    case 'lock':
      return 'Lock access'
    case 'unlock':
      return 'Unlock access'
    case 'remove':
      return 'Remove access'
    case 'restore':
      return 'Restore access'
  }
}

function getActionDescription(action: UserAccessAction): string {
  switch (action) {
    case 'lock':
      return 'The user will be unable to access the application until unlocked.'
    case 'unlock':
      return 'The user will regain access when their active roles grant application access.'
    case 'remove':
      return 'Application access will be soft-removed. Historical references will be retained.'
    case 'restore':
      return 'The removed application account will be restored without changing Active Directory.'
  }
}

export function UserDetailsDialog({
  userId,
  availableRoles,
  rolesLoading,
  rolesError,
  onRetryRoles,
  onClose,
  onChanged,
}: UserDetailsDialogProps) {
  const { data, loading, error, retry } = useAdminUser(userId)
  const [updatedUser, setUpdatedUser] = useState<ApplicationUser | null>(null)
  const [selectedRoleIds, setSelectedRoleIds] = useState<
    readonly string[] | null
  >(null)
  const [pendingAction, setPendingAction] = useState<UserAccessAction | null>(
    null,
  )
  const [lockReason, setLockReason] = useState('')
  const [validationError, setValidationError] = useState<string | null>(null)
  const [mutationError, setMutationError] = useState<string | null>(null)
  const [mutationConflict, setMutationConflict] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const activeMutationRef = useRef<AbortController | null>(null)
  const isMountedRef = useRef(true)
  const user = updatedUser ?? data

  useEffect(() => {
    isMountedRef.current = true

    return () => {
      isMountedRef.current = false
      activeMutationRef.current?.abort()
      activeMutationRef.current = null
    }
  }, [])

  const roleOptions = useMemo(() => {
    const rolesById = new Map(availableRoles.map((role) => [role.id, role]))

    user?.roles.forEach((role) => {
      if (!rolesById.has(role.id)) {
        rolesById.set(role.id, role)
      }
    })

    return [...rolesById.values()].sort((left, right) =>
      left.nameEnglish.localeCompare(right.nameEnglish),
    )
  }, [availableRoles, user])

  const effectiveRoleIds =
    selectedRoleIds ?? user?.roles.map((role) => role.id) ?? []

  const resetMutationMessages = () => {
    setMutationError(null)
    setMutationConflict(false)
    setSuccessMessage(null)
  }

  const handleMutationFailure = (mutationFailure: unknown) => {
    if (!isMountedRef.current) {
      return
    }

    setMutationConflict(
      mutationFailure instanceof ApiError && mutationFailure.status === 409,
    )
    setMutationError(
      getSafeApiErrorMessage(
        mutationFailure,
        'The user could not be updated. Please try again.',
      ),
    )
  }

  const completeMutation = (
    nextUser: ApplicationUser,
    confirmationMessage: string,
  ) => {
    if (!isMountedRef.current) {
      return
    }

    setUpdatedUser(nextUser)
    setSelectedRoleIds(nextUser.roles.map((role) => role.id))
    setPendingAction(null)
    setLockReason('')
    setValidationError(null)
    setMutationError(null)
    setMutationConflict(false)
    setSuccessMessage(confirmationMessage)
    onChanged()
  }

  const runMutation = async (
    mutation: (signal: AbortSignal) => Promise<ApplicationUser>,
    confirmationMessage: string,
  ) => {
    if (activeMutationRef.current !== null) {
      return
    }

    const controller = new AbortController()
    activeMutationRef.current = controller
    resetMutationMessages()
    setSubmitting(true)

    try {
      const nextUser = await mutation(controller.signal)

      if (!controller.signal.aborted) {
        completeMutation(nextUser, confirmationMessage)
      }
    } catch (mutationFailure: unknown) {
      if (!controller.signal.aborted) {
        handleMutationFailure(mutationFailure)
      }
    } finally {
      if (activeMutationRef.current === controller) {
        activeMutationRef.current = null

        if (isMountedRef.current) {
          setSubmitting(false)
        }
      }
    }
  }

  const handleRoleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!user || user.isProtectedSystemAdministrator || user.isRemoved) {
      return
    }

    void runMutation(
      (signal) =>
        adminUserService.updateRoles(
          user.id,
          { roleIds: effectiveRoleIds, rowVersion: user.rowVersion },
          signal,
        ),
      'Role assignments were updated.',
    )
  }

  const handleAccessAction = () => {
    if (!user || !pendingAction || user.isProtectedSystemAdministrator) {
      return
    }

    if (pendingAction === 'lock' && lockReason.trim().length === 0) {
      setValidationError('Enter a reason for locking application access.')
      return
    }

    const rowVersionRequest = { rowVersion: user.rowVersion }
    const action = pendingAction
    const mutation = (signal: AbortSignal) => {
      switch (action) {
        case 'lock':
          return adminUserService.lock(
            user.id,
            { reason: lockReason.trim(), rowVersion: user.rowVersion },
            signal,
          )
        case 'unlock':
          return adminUserService.unlock(user.id, rowVersionRequest, signal)
        case 'remove':
          return adminUserService.remove(user.id, rowVersionRequest, signal)
        case 'restore':
          return adminUserService.restore(user.id, rowVersionRequest, signal)
      }
    }

    void runMutation(mutation, `${getActionLabel(action)} completed.`)
  }

  const reloadDetails = () => {
    setUpdatedUser(null)
    setSelectedRoleIds(null)
    setPendingAction(null)
    resetMutationMessages()
    retry()
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
        <form
          aria-busy={submitting}
          onSubmit={handleRoleSubmit}
          noValidate
        >
          <DialogBody>
            <DialogTitle>User details</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <AdministrationResourceState
                loading={loading}
                error={error}
                loadingLabel="Loading user details"
                onRetry={retry}
              >
                {user ? (
                  <>
                    <div className="administration-detail-heading">
                      <div>
                        <h3>{user.displayName}</h3>
                        <p>{user.accountName}</p>
                      </div>
                      <div className="administration-detail-heading__actions">
                        <StatusBadge status={getUserStatus(user)} />
                        {!user.isProtectedSystemAdministrator ? (
                          <Button
                            type="button"
                            appearance="subtle"
                            icon={<ArrowClockwise20Regular />}
                            disabled={submitting}
                            onClick={() =>
                              void runMutation(
                                (signal) =>
                                  adminUserService.refreshDirectoryProfile(
                                    user.id,
                                    { rowVersion: user.rowVersion },
                                    signal,
                                  ),
                                'Directory profile was refreshed.',
                              )
                            }
                          >
                            {submitting ? 'Updating' : 'Refresh from directory'}
                          </Button>
                        ) : null}
                      </div>
                    </div>

                    {user.isProtectedSystemAdministrator ? (
                      <MessageBar intent="info">
                        <MessageBarBody>
                          This protected bootstrap administrator cannot be locked,
                          removed, or assigned away through Administration.
                        </MessageBarBody>
                      </MessageBar>
                    ) : null}

                    {successMessage ? (
                      <MessageBar intent="success" politeness="polite">
                        <MessageBarBody>{successMessage}</MessageBarBody>
                      </MessageBar>
                    ) : null}

                    {mutationError ? (
                      <MessageBar intent="error" politeness="assertive">
                        <MessageBarBody>
                          {mutationError}
                          {mutationConflict ? (
                            <Button
                              type="button"
                              appearance="transparent"
                              icon={<ArrowClockwise20Regular />}
                              onClick={reloadDetails}
                            >
                              Reload current details
                            </Button>
                          ) : null}
                        </MessageBarBody>
                      </MessageBar>
                    ) : null}

                    <dl className="administration-detail-grid">
                      <div>
                        <dt>Email</dt>
                        <dd>{user.email ?? 'Not available'}</dd>
                      </div>
                      <div>
                        <dt>User principal name</dt>
                        <dd>{user.userPrincipalName ?? 'Not available'}</dd>
                      </div>
                      <div>
                        <dt>Last successful access</dt>
                        <dd>{formatOptionalDate(user.lastSuccessfulAccessAtUtc)}</dd>
                      </div>
                      <div>
                        <dt>Created</dt>
                        <dd>{formatOptionalDate(user.createdAtUtc)}</dd>
                      </div>
                      {user.lockReason ? (
                        <div className="administration-detail-grid__wide">
                          <dt>Lock reason</dt>
                          <dd>{user.lockReason}</dd>
                        </div>
                      ) : null}
                    </dl>

                    <section className="administration-dialog__section">
                      <h3>Custom role assignments</h3>
                      <p>
                        Roles grant only catalogue permissions. System Administrator
                        access is not a custom role.
                      </p>

                      {rolesLoading ? (
                        <Spinner size="small" label="Loading available roles" />
                      ) : rolesError ? (
                        <MessageBar intent="error">
                          <MessageBarBody>
                            {rolesError}
                            <Button
                              type="button"
                              appearance="transparent"
                              onClick={onRetryRoles}
                            >
                              Retry
                            </Button>
                          </MessageBarBody>
                        </MessageBar>
                      ) : roleOptions.length === 0 ? (
                        <p className="administration-dialog__empty-copy">
                          No active custom roles are available.
                        </p>
                      ) : (
                        <div
                          className="administration-checkbox-list"
                          role="group"
                          aria-label="Custom role assignments"
                        >
                          {roleOptions.map((role) => {
                            const checked = effectiveRoleIds.includes(role.id)

                            return (
                              <Checkbox
                                key={role.id}
                                checked={checked}
                                disabled={
                                  submitting ||
                                  user.isProtectedSystemAdministrator ||
                                  user.isRemoved ||
                                  (role.isArchived && !checked)
                                }
                                label={`${role.nameEnglish} (${role.code})${
                                  role.isArchived ? ' — archived' : ''
                                }`}
                                onChange={(_, checkboxData) => {
                                  resetMutationMessages()
                                  setSelectedRoleIds((currentSelection) => {
                                    const nextSelection = new Set(
                                      currentSelection ??
                                        user.roles.map(
                                          (assignedRole) => assignedRole.id,
                                        ),
                                    )

                                    if (checkboxData.checked === true) {
                                      nextSelection.add(role.id)
                                    } else {
                                      nextSelection.delete(role.id)
                                    }

                                    return [...nextSelection]
                                  })
                                }}
                              />
                            )
                          })}
                        </div>
                      )}
                    </section>

                    {!user.isProtectedSystemAdministrator ? (
                      <section className="administration-dialog__section">
                        <h3>Application access</h3>
                        <div className="administration-action-row">
                          {user.isRemoved ? (
                            <Button
                              type="button"
                              appearance="outline"
                              disabled={submitting}
                              onClick={() => setPendingAction('restore')}
                            >
                              Restore access
                            </Button>
                          ) : (
                            <>
                              <Button
                                type="button"
                                appearance="outline"
                                disabled={submitting}
                                onClick={() =>
                                  setPendingAction(
                                    user.isLocked ? 'unlock' : 'lock',
                                  )
                                }
                              >
                                {user.isLocked ? 'Unlock access' : 'Lock access'}
                              </Button>
                              <Button
                                type="button"
                                appearance="outline"
                                className="administration-danger-button"
                                disabled={submitting}
                                onClick={() => setPendingAction('remove')}
                              >
                                Remove access
                              </Button>
                            </>
                          )}
                        </div>

                        {pendingAction ? (
                          <div
                            className="administration-action-confirmation"
                            role="region"
                            aria-live="polite"
                            aria-label={`Confirm ${getActionLabel(pendingAction)}`}
                          >
                            <h4>{getActionLabel(pendingAction)}?</h4>
                            <p>{getActionDescription(pendingAction)}</p>
                            {pendingAction === 'lock' ? (
                              <Field
                                label="Lock reason"
                                required
                                validationMessage={validationError ?? undefined}
                                validationState={
                                  validationError ? 'error' : 'none'
                                }
                              >
                                <Textarea
                                  value={lockReason}
                                  rows={3}
                                  maxLength={500}
                                  disabled={submitting}
                                  onChange={(_, textareaData) => {
                                    setLockReason(textareaData.value)
                                    setValidationError(null)
                                  }}
                                />
                              </Field>
                            ) : null}
                            <div className="administration-action-row">
                              <Button
                                type="button"
                                appearance="secondary"
                                disabled={submitting}
                                onClick={() => {
                                  setPendingAction(null)
                                  setLockReason('')
                                  setValidationError(null)
                                }}
                              >
                                Cancel
                              </Button>
                              <Button
                                type="button"
                                appearance="primary"
                                disabled={submitting}
                                onClick={handleAccessAction}
                              >
                                {submitting
                                  ? 'Updating'
                                  : getActionLabel(pendingAction)}
                              </Button>
                            </div>
                          </div>
                        ) : null}
                      </section>
                    ) : null}
                  </>
                ) : null}
              </AdministrationResourceState>
            </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                disabled={submitting}
                onClick={onClose}
              >
                Close
              </Button>
              {user && !user.isProtectedSystemAdministrator && !user.isRemoved ? (
                <Button
                  type="submit"
                  appearance="primary"
                  disabled={
                    submitting ||
                    rolesLoading ||
                    rolesError !== null ||
                    pendingAction !== null
                  }
                >
                  {submitting ? 'Saving' : 'Save role assignments'}
                </Button>
              ) : null}
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
