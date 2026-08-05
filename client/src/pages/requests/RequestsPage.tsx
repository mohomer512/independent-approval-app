import { Button, Dropdown, Field, Input, Option } from '@fluentui/react-components'
import {
  Add24Regular,
  CheckmarkCircle24Regular,
  Clock24Regular,
  Document24Regular,
  Edit20Regular,
  Filter20Regular,
  MoreHorizontal20Regular,
  Search20Regular,
} from '@fluentui/react-icons'
import { useNavigate } from 'react-router'
import {
  formatCurrency,
  formatDate,
  PageHeader,
  PriorityBadge,
  SectionPanel,
  StatusBadge,
  SummaryCard,
} from '../../components/common'
import { mockDataService } from '../../services'

export function RequestsPage() {
  const navigate = useNavigate()
  const requests = mockDataService.getRequests()
  const pendingCount = requests.filter(
    (request) => request.status === 'Pending',
  ).length
  const approvedCount = requests.filter(
    (request) => request.status === 'Approved',
  ).length
  const draftCount = requests.filter((request) => request.status === 'Draft').length

  return (
    <div className="requests-page page-stack">
      <PageHeader
        eyebrow="Requests"
        title="My Requests"
        description="Create, track, and review approval requests you have submitted."
        actions={
          <Button
            type="button"
            appearance="primary"
            icon={<Add24Regular />}
            onClick={() => navigate('/requests/new')}
          >
            New request
          </Button>
        }
      />

      <section className="page-summary-grid" aria-label="Request summary">
        <SummaryCard
          label="All requests"
          value={requests.length}
          description="Requests created by you"
          icon={<Document24Regular />}
          tone="brand"
        />
        <SummaryCard
          label="Pending"
          value={pendingCount}
          description="Moving through approval"
          icon={<Clock24Regular />}
          tone="warning"
        />
        <SummaryCard
          label="Approved"
          value={approvedCount}
          description="Successfully completed"
          icon={<CheckmarkCircle24Regular />}
          tone="success"
        />
        <SummaryCard
          label="Drafts"
          value={draftCount}
          description="Ready for you to continue"
          icon={<Edit20Regular />}
          tone="neutral"
        />
      </section>

      <SectionPanel
        title="Request history"
        description="Browse your requests and check where each one is in the approval process."
      >
        <div className="table-toolbar" role="search">
          <Field label="Search requests" className="table-toolbar__search">
            <Input
              type="search"
              contentBefore={<Search20Regular />}
              placeholder="Search by number or title"
            />
          </Field>
          <Field label="Status" className="table-toolbar__filter">
            <Dropdown
              defaultValue="All statuses"
              defaultSelectedOptions={['all']}
            >
              <Option value="all">All statuses</Option>
              <Option value="draft">Draft</Option>
              <Option value="pending">Pending</Option>
              <Option value="approved">Approved</Option>
              <Option value="returned">Returned</Option>
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
          <table className="data-table data-table--requests-page">
            <caption className="visually-hidden">
              All approval requests created by Mohammed Omer
            </caption>
            <thead>
              <tr>
                <th scope="col">Request</th>
                <th scope="col">Category</th>
                <th scope="col">Status</th>
                <th scope="col">Priority</th>
                <th scope="col">Current step</th>
                <th scope="col">Amount</th>
                <th scope="col">Submitted</th>
                <th scope="col">
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {requests.map((request) => (
                <tr key={request.id}>
                  <td>
                    <span className="data-table__primary">{request.title}</span>
                    <span className="data-table__secondary">
                      {request.requestNumber}
                    </span>
                  </td>
                  <td>{request.category}</td>
                  <td>
                    <StatusBadge status={request.status} />
                  </td>
                  <td>
                    <PriorityBadge priority={request.priority} />
                  </td>
                  <td>
                    <span className="data-table__primary">{request.currentStep}</span>
                    {request.currentApprover ? (
                      <span className="data-table__secondary">
                        {request.currentApprover}
                      </span>
                    ) : null}
                  </td>
                  <td>{formatCurrency(request.amount, request.currency)}</td>
                  <td>
                    {request.submittedAt ? (
                      <time dateTime={request.submittedAt}>
                        {formatDate(request.submittedAt)}
                      </time>
                    ) : (
                      'Not submitted'
                    )}
                  </td>
                  <td className="data-table__action">
                    <Button
                      type="button"
                      appearance="subtle"
                      size="small"
                      icon={<MoreHorizontal20Regular />}
                      aria-label={`Actions for ${request.requestNumber}`}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="table-footer">
          <span>Showing {requests.length} requests</span>
          <span>Mock data for Stage 1</span>
        </div>
      </SectionPanel>
    </div>
  )
}
