import { Spinner } from '@fluentui/react-components'
import { Outlet } from 'react-router'
import { useCurrentUser } from '../../hooks/useCurrentUser'
import type { ApplicationAccessState } from '../../models'
import { ApplicationAccessMessage } from './ApplicationAccessMessage'

function getAccessMessage(accessState: ApplicationAccessState): {
  readonly title: string
  readonly description: string
} {
  switch (accessState) {
    case 'locked':
      return {
        title: 'Application access locked',
        description:
          'Your access to Independent Approval has been locked. Contact a system administrator for assistance.',
      }
    case 'removed':
      return {
        title: 'Application access removed',
        description:
          'Your Windows account is authenticated, but application access has been removed.',
      }
    case 'inactive':
      return {
        title: 'Application access inactive',
        description:
          'Your application account is currently inactive. Contact a system administrator for assistance.',
      }
    case 'missingAccessPermission':
      return {
        title: 'Access denied',
        description:
          'Your application roles do not currently grant access to Independent Approval.',
      }
    case 'unknown':
      return {
        title: 'Access denied',
        description:
          'Your Windows account is authenticated but has not been added to Independent Approval.',
      }
    case 'granted':
      return {
        title: 'Access denied',
        description: 'Application access could not be confirmed.',
      }
  }
}

export function ApplicationAccessBoundary() {
  const { data: currentUser, loading, error, retry } = useCurrentUser()

  if (loading) {
    return (
      <div className="application-session-loading" role="status">
        <Spinner label="Confirming application access" />
      </div>
    )
  }

  if (error || currentUser === null) {
    return (
      <ApplicationAccessMessage
        fullPage
        variant="error"
        title="Application access is unavailable"
        description={
          error ?? 'Your application session could not be loaded. Please try again.'
        }
        onRetry={retry}
      />
    )
  }

  if (!currentUser.isAuthenticated) {
    return (
      <ApplicationAccessMessage
        fullPage
        title="Windows authentication required"
        description="Refresh the page after signing in with your Windows account."
      />
    )
  }

  if (!currentUser.hasApplicationAccess) {
    const message = getAccessMessage(currentUser.accessState)
    return (
      <ApplicationAccessMessage
        fullPage
        title={message.title}
        description={message.description}
      />
    )
  }

  return <Outlet />
}
