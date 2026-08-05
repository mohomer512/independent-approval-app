import { Button, Dropdown, Field, Input, Option } from '@fluentui/react-components'
import {
  Alert24Regular,
  Calendar20Regular,
  CheckmarkCircle24Regular,
  ClipboardTaskListLtr24Regular,
  Eye20Regular,
  Filter20Regular,
  Search20Regular,
} from '@fluentui/react-icons'
import {
  formatDateTime,
  PageHeader,
  PriorityBadge,
  SectionPanel,
  StatusBadge,
  SummaryCard,
} from '../../components/common'
import { mockDataService } from '../../services'

export function TasksPage() {
  const tasks = mockDataService.getTasks()
  const pendingTasks = mockDataService.getPendingTasks()
  const urgentCount = tasks.filter((task) => task.priority === 'Urgent').length
  const dueTodayCount = tasks.filter((task) =>
    task.dueAt.startsWith('2026-08-04'),
  ).length
  const completedCount = tasks.filter(
    (task) => task.status === 'Completed',
  ).length

  return (
    <div className="tasks-page page-stack">
      <PageHeader
        eyebrow="Approvals"
        title="My Tasks"
        description="Review approval work assigned to you and act before each due date."
      />

      <section className="page-summary-grid" aria-label="Task summary">
        <SummaryCard
          label="Pending tasks"
          value={pendingTasks.length}
          description="Waiting for your action"
          icon={<ClipboardTaskListLtr24Regular />}
          tone="brand"
        />
        <SummaryCard
          label="Urgent"
          value={urgentCount}
          description="Prioritize these tasks"
          icon={<Alert24Regular />}
          tone="warning"
        />
        <SummaryCard
          label="Due today"
          value={dueTodayCount}
          description="Due by end of day"
          icon={<Calendar20Regular />}
          tone="informative"
        />
        <SummaryCard
          label="Completed"
          value={completedCount}
          description="Completed in this view"
          icon={<CheckmarkCircle24Regular />}
          tone="success"
        />
      </section>

      <SectionPanel
        title="Approval queue"
        description="Tasks are ordered by priority and due date."
      >
        <div className="table-toolbar" role="search">
          <Field label="Search tasks" className="table-toolbar__search">
            <Input
              type="search"
              contentBefore={<Search20Regular />}
              placeholder="Search by request or requester"
            />
          </Field>
          <Field label="Task status" className="table-toolbar__filter">
            <Dropdown
              defaultValue="Open tasks"
              defaultSelectedOptions={['open']}
            >
              <Option value="open">Open tasks</Option>
              <Option value="pending">Pending</Option>
              <Option value="in-review">In review</Option>
              <Option value="completed">Completed</Option>
              <Option value="all">All tasks</Option>
            </Dropdown>
          </Field>
          <Button
            type="button"
            appearance="outline"
            icon={<Filter20Regular />}
            className="table-toolbar__button"
          >
            More filters
          </Button>
        </div>

        <div className="data-table-scroll" tabIndex={0}>
          <table className="data-table data-table--tasks">
            <caption className="visually-hidden">
              Approval tasks assigned to Mohammed Omer
            </caption>
            <thead>
              <tr>
                <th scope="col">Task</th>
                <th scope="col">Requester</th>
                <th scope="col">Approval step</th>
                <th scope="col">Priority</th>
                <th scope="col">Status</th>
                <th scope="col">Received</th>
                <th scope="col">Due</th>
                <th scope="col">
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {tasks.map((task) => (
                <tr key={task.id}>
                  <td>
                    <span className="data-table__primary">{task.title}</span>
                    <span className="data-table__secondary">
                      {task.requestNumber} · {task.action}
                    </span>
                  </td>
                  <td>{task.requesterName}</td>
                  <td>{task.stepName}</td>
                  <td>
                    <PriorityBadge priority={task.priority} />
                  </td>
                  <td>
                    <StatusBadge status={task.status} />
                  </td>
                  <td>
                    <time dateTime={task.receivedAt}>
                      {formatDateTime(task.receivedAt)}
                    </time>
                  </td>
                  <td>
                    <time dateTime={task.dueAt}>{formatDateTime(task.dueAt)}</time>
                  </td>
                  <td className="data-table__action">
                    <Button
                      type="button"
                      appearance="primary"
                      size="small"
                      icon={<Eye20Regular />}
                      aria-label={`${task.action} task for ${task.requestNumber}`}
                    >
                      {task.action}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="table-footer">
          <span>{pendingTasks.length} tasks require attention</span>
          <span>Sorted by due date</span>
        </div>
      </SectionPanel>
    </div>
  )
}
