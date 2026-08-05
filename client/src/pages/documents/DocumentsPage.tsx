import { Button, Dropdown, Field, Input, Option } from '@fluentui/react-components'
import {
  ArrowDownload20Regular,
  ArrowUpload24Regular,
  Attach20Regular,
  Document24Regular,
  Folder24Regular,
  Search20Regular,
} from '@fluentui/react-icons'
import {
  formatDate,
  formatFileSize,
  PageHeader,
  SectionPanel,
  StatusBadge,
  SummaryCard,
} from '../../components/common'
import { mockDataService } from '../../services'

export function DocumentsPage() {
  const documents = mockDataService.getDocuments()
  const documentMetric = mockDataService
    .getDashboardMetrics()
    .find((metric) => metric.id === 'documents')
  const currentCount = documents.filter(
    (document) => document.status === 'Current',
  ).length
  const linkedCount = documents.filter(
    (document) => document.linkedRequestId !== null,
  ).length
  const templateCount = documents.filter(
    (document) => document.category === 'Template',
  ).length

  return (
    <div className="documents-page page-stack">
      <PageHeader
        eyebrow="Content"
        title="Document Library"
        description="Find controlled documents and files linked to approval requests."
        actions={
          <Button
            type="button"
            appearance="primary"
            icon={<ArrowUpload24Regular />}
          >
            Upload document
          </Button>
        }
      />

      <section className="page-summary-grid" aria-label="Document summary">
        <SummaryCard
          label="All documents"
          value={documentMetric?.value ?? documents.length}
          description="Stored in the library"
          icon={<Folder24Regular />}
          tone="brand"
        />
        <SummaryCard
          label="Current in this view"
          value={currentCount}
          description="Latest controlled versions"
          icon={<Document24Regular />}
          tone="success"
        />
        <SummaryCard
          label="Linked to requests"
          value={linkedCount}
          description="Supporting approval records"
          icon={<Attach20Regular />}
          tone="informative"
        />
        <SummaryCard
          label="Templates"
          value={templateCount}
          description="Reusable request documents"
          icon={<Document24Regular />}
          tone="neutral"
        />
      </section>

      <SectionPanel
        title="Library contents"
        description="Stage 1 displays representative metadata for locally managed files."
      >
        <div className="table-toolbar" role="search">
          <Field label="Search documents" className="table-toolbar__search">
            <Input
              type="search"
              contentBefore={<Search20Regular />}
              placeholder="Search by document name"
            />
          </Field>
          <Field label="Category" className="table-toolbar__filter">
            <Dropdown
              defaultValue="All categories"
              defaultSelectedOptions={['all']}
            >
              <Option value="all">All categories</Option>
              <Option value="policy">Policy</Option>
              <Option value="contract">Contract</Option>
              <Option value="supporting-document">Supporting document</Option>
              <Option value="template">Template</Option>
            </Dropdown>
          </Field>
          <Field label="Document status" className="table-toolbar__filter">
            <Dropdown
              defaultValue="All statuses"
              defaultSelectedOptions={['all']}
            >
              <Option value="all">All statuses</Option>
              <Option value="current">Current</Option>
              <Option value="superseded">Superseded</Option>
              <Option value="archived">Archived</Option>
            </Dropdown>
          </Field>
        </div>

        <div className="data-table-scroll" tabIndex={0}>
          <table className="data-table data-table--documents">
            <caption className="visually-hidden">
              Documents available in the approval library
            </caption>
            <thead>
              <tr>
                <th scope="col">Document</th>
                <th scope="col">Category</th>
                <th scope="col">Version</th>
                <th scope="col">Status</th>
                <th scope="col">Owner</th>
                <th scope="col">Linked request</th>
                <th scope="col">Updated</th>
                <th scope="col">
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {documents.map((document) => (
                <tr key={document.id}>
                  <td>
                    <span className="document-name">
                      <span className="document-name__extension" aria-hidden="true">
                        {document.fileExtension.toUpperCase()}
                      </span>
                      <span>
                        <span className="data-table__primary">{document.name}</span>
                        <span className="data-table__secondary">
                          {formatFileSize(document.sizeBytes)}
                        </span>
                      </span>
                    </span>
                  </td>
                  <td>{document.category}</td>
                  <td>{document.version}</td>
                  <td>
                    <StatusBadge status={document.status} />
                  </td>
                  <td>{document.ownerName}</td>
                  <td>{document.linkedRequestNumber ?? '—'}</td>
                  <td>
                    <time dateTime={document.updatedAt}>
                      {formatDate(document.updatedAt)}
                    </time>
                  </td>
                  <td className="data-table__action">
                    <Button
                      type="button"
                      appearance="subtle"
                      size="small"
                      icon={<ArrowDownload20Regular />}
                      aria-label={`Download ${document.name}`}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="table-footer">
          <span>Showing {documents.length} representative documents</span>
          <span>File operations are planned for a later stage</span>
        </div>
      </SectionPanel>
    </div>
  )
}
