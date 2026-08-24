import { Outlet } from 'react-router'
import { useCurrentUser } from '../../hooks/useCurrentUser'
import { ApplicationAccessMessage } from './ApplicationAccessMessage'

export function RequireSystemAdministrator() {
  const { data: currentUser } = useCurrentUser()

  if (currentUser?.isSystemAdministrator !== true) {
    return (
      <ApplicationAccessMessage
        title="Access denied"
        description="System Administrator access is required to open Administration."
      />
    )
  }

  return <Outlet />
}
