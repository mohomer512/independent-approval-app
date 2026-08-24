import { Button, Card } from '@fluentui/react-components'
import {
  Add24Regular,
  ArrowRight20Regular,
  CheckmarkCircle24Regular,
  ClipboardTaskListLtr24Regular,
  Clock24Regular,
  Document24Regular,
  DocumentAdd24Regular,
  Eye20Regular,
  Folder24Regular,
} from '@fluentui/react-icons'
import type { ReactElement } from 'react'
import { useNavigate } from 'react-router'
import {
  formatCurrency,
  formatDate,
  formatDateTime,
  PageHeader,
  PriorityBadge,
  SectionPanel,
  StatusBadge,
  SummaryCard,
} from '../../components/common'
import type {
  DashboardMetricId,
  QuickActionIconName,
} from '../../models'
import { useCurrentUser } from '../../hooks/useCurrentUser'
import { mockDataService } from '../../services'

function getMetricIcon(metricId: DashboardMetricId): ReactElement {
  switch (metricId) {
    case 'my-requests':
      return <Document24Regular />
    case 'pending-tasks':
      return <Clock24Regular />
    case 'documents':
      return <Folder24Regular />
    case 'completed':
      return <CheckmarkCircle24Regular />
  }
}

function getActionIcon(iconName: QuickActionIconName): ReactElement {
  switch (iconName) {
    case 'add':
      return <Add24Regular />
    case 'tasks':
      return <ClipboardTaskListLtr24Regular />
    case 'documents':
      return <DocumentAdd24Regular />
  }
}

export function DashboardPage() {
  const navigate = useNavigate()
  const { data: currentUser } = useCurrentUser()
  const dashboard = mockDataService.getDashboardData()
  const displayName =
    currentUser?.displayName.trim() ||
    currentUser?.userName ||
    currentUser?.accountName ||
    'User'

  return (
    <div className="dashboard-page page-stack">
      <PageHeader
        eyebrow="Overview"
        title="Dashboard"
        description="Monitor your requests, approval work, and documents from one place."
      />

      <Card className="welcome-panel" role="region" aria-label="Welcome summary">
        <div className="welcome-panel__content">
          <p className="welcome-panel__eyebrow">Tuesday, 4 August 2026</p>
          <h2 className="welcome-panel__title">
            Welcome, {displayName}
          </h2>
          <p className="welcome-panel__description">
            You have {dashboard.pendingTasks.length} approval tasks waiting for
            your attention. Review urgent items first to keep workflows moving.
          </p>
        </div>
        <Button
          type="button"
          appearance="primary"
          icon={<ClipboardTaskListLtr24Regular />}
          onClick={() => navigate('/tasks')}
        >
          Review my tasks
        </Button>
      </Card>

      <section className="dashboard-metrics" aria-label="Approval summary">
        {dashboard.metrics.map((metric) => (
          <SummaryCard
            key={metric.id}
            label={metric.label}
            value={metric.value}
            description={metric.description}
            icon={getMetricIcon(metric.id)}
            tone={metric.tone}
          />
        ))}
      </section>

      <div className="dashboard-grid">
        <SectionPanel
          className="dashboard-grid__requests"
          title="Recent requests"
          description="Your latest approval requests and their current progress."
          action={
            <Button
              type="button"
              appearance="subtle"
              icon={<ArrowRight20Regular />}
              iconPosition="after"
              onClick={() => navigate('/requests')}
            >
              View all
            </Button>
          }
        >
          <div className="data-table-scroll" tabIndex={0}>
            <table className="data-table data-table--requests">
              <caption className="visually-hidden">
                Five most recently updated approval requests
              </caption>
              <thead>
                <tr>
                  <th scope="col">Request</th>
                  <th scope="col">Status</th>
                  <th scope="col">Amount</th>
                  <th scope="col">Updated</th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {dashboard.recentRequests.map((request) => (
                  <tr key={request.id}>
                    <td>
                      <span className="data-table__primary">{request.title}</span>
                      <span className="data-table__secondary">
                        {request.requestNumber} · {request.category}
                      </span>
                    </td>
                    <td>
                      <StatusBadge status={request.status} />
                    </td>
                    <td>{formatCurrency(request.amount, request.currency)}</td>
                    <td>
                      <time dateTime={request.updatedAt}>
                        {formatDate(request.updatedAt)}
                      </time>
                    </td>
                    <td className="data-table__action">
                      <Button
                        type="button"
                        appearance="subtle"
                        size="small"
                        icon={<Eye20Regular />}
                        aria-label={`View ${request.requestNumber}`}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </SectionPanel>

        <SectionPanel
          className="dashboard-grid__tasks"
          title="Pending tasks"
          description="Approvals assigned to you."
          action={
            <Button
              type="button"
              appearance="subtle"
              icon={<ArrowRight20Regular />}
              iconPosition="after"
              onClick={() => navigate('/tasks')}
            >
              View all
            </Button>
          }
        >
          <div className="task-list">
            {dashboard.pendingTasks.map((task) => (
              <article className="task-list-item" key={task.id}>
                <div className="task-list-item__header">
                  <span className="task-list-item__number">
                    {task.requestNumber}
                  </span>
                  <PriorityBadge priority={task.priority} />
                </div>
                <h3 className="task-list-item__title">{task.title}</h3>
                <p className="task-list-item__meta">
                  {task.stepName} · {task.requesterName}
                </p>
                <div className="task-list-item__footer">
                  <span className="task-list-item__due">
                    <Clock24Regular aria-hidden="true" />
                    Due <time dateTime={task.dueAt}>{formatDateTime(task.dueAt)}</time>
                  </span>
                  <Button
                    type="button"
                    appearance="outline"
                    size="small"
                    onClick={() => navigate('/tasks')}
                  >
                    {task.action}
                  </Button>
                </div>
              </article>
            ))}
          </div>
        </SectionPanel>
      </div>

      <SectionPanel
        className="quick-actions"
        title="Quick actions"
        description="Start the tasks you use most often."
      >
        <div className="quick-actions__grid">
          {dashboard.quickActions.map((action) => (
            <Button
              key={action.id}
              type="button"
              className="quick-action"
              appearance={action.appearance}
              icon={getActionIcon(action.icon)}
              onClick={() => navigate(action.route)}
            >
              <span className="quick-action__copy">
                <span className="quick-action__label">{action.label}</span>
                <span className="quick-action__description">
                  {action.description}
                </span>
              </span>
            </Button>
          ))}
        </div>
      </SectionPanel>
    </div>
  )
}
