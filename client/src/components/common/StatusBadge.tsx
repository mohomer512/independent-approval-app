import { Badge } from '@fluentui/react-components'

interface StatusBadgeProps {
  readonly status: string
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const className = `status-badge status-badge--${status
    .toLowerCase()
    .replaceAll(' ', '-')}`

  switch (status) {
    case 'Approved':
    case 'Completed':
    case 'Current':
    case 'Active':
      return (
        <Badge className={className} appearance="tint" color="success">
          {status}
        </Badge>
      )
    case 'Pending':
    case 'In Review':
      return (
        <Badge className={className} appearance="tint" color="warning">
          {status}
        </Badge>
      )
    case 'Rejected':
    case 'Cancelled':
    case 'Locked':
      return (
        <Badge className={className} appearance="tint" color="danger">
          {status}
        </Badge>
      )
    case 'Returned':
    case 'Escalated':
      return (
        <Badge className={className} appearance="tint" color="important">
          {status}
        </Badge>
      )
    case 'Draft':
    case 'Archived':
    case 'Inactive':
      return (
        <Badge className={className} appearance="tint" color="subtle">
          {status}
        </Badge>
      )
    default:
      return (
        <Badge className={className} appearance="tint" color="informative">
          {status}
        </Badge>
      )
  }
}

interface PriorityBadgeProps {
  readonly priority: string
}

export function PriorityBadge({ priority }: PriorityBadgeProps) {
  if (priority === 'Urgent') {
    return (
      <Badge appearance="tint" color="danger" className="priority-badge">
        {priority}
      </Badge>
    )
  }

  if (priority === 'High') {
    return (
      <Badge appearance="tint" color="important" className="priority-badge">
        {priority}
      </Badge>
    )
  }

  return (
    <Badge appearance="tint" color="subtle" className="priority-badge">
      {priority}
    </Badge>
  )
}
