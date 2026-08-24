import { Button } from '@fluentui/react-components'
import { ArrowLeft20Regular, Wrench24Regular } from '@fluentui/react-icons'
import { useNavigate } from 'react-router'
import { PageHeader, SectionPanel } from '../../components/common'

interface AdministrationFoundationPendingPageProps {
  readonly title: string
  readonly description: string
}

export function AdministrationFoundationPendingPage({
  title,
  description,
}: AdministrationFoundationPendingPageProps) {
  const navigate = useNavigate()

  return (
    <div className="administration-list-page page-stack">
      <PageHeader
        eyebrow="Administration"
        title={title}
        description={description}
        actions={
          <Button
            type="button"
            appearance="subtle"
            icon={<ArrowLeft20Regular />}
            onClick={() => navigate('/administration')}
          >
            Administration
          </Button>
        }
      />
      <SectionPanel title="Foundation pending">
        <div className="administration-resource-state" role="status">
          <Wrench24Regular aria-hidden="true" />
          <div>
            <h3>This protected route is ready</h3>
            <p>The database-backed designer will be added in its later checkpoint.</p>
          </div>
        </div>
      </SectionPanel>
    </div>
  )
}
