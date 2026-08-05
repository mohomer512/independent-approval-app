import {
  Button,
  Dropdown,
  Field,
  Input,
  Option,
  Textarea,
} from '@fluentui/react-components'
import {
  ArrowLeft20Regular,
  Attach20Regular,
  CheckmarkCircle24Regular,
  DocumentAdd24Regular,
  Info20Regular,
  Save20Regular,
} from '@fluentui/react-icons'
import { useNavigate } from 'react-router'
import { PageHeader, SectionPanel } from '../../components/common'

export function NewRequestPage() {
  const navigate = useNavigate()

  return (
    <div className="new-request-page page-stack">
      <PageHeader
        eyebrow="Requests"
        title="New Request"
        description="Provide the details needed to start a new approval workflow."
        actions={
          <Button
            type="button"
            appearance="subtle"
            icon={<ArrowLeft20Regular />}
            onClick={() => navigate('/requests')}
          >
            Back to requests
          </Button>
        }
      />

      <div className="new-request-layout">
        <form
          className="request-form"
          aria-label="New approval request"
          onSubmit={(event) => event.preventDefault()}
        >
          <SectionPanel
            title="Request details"
            description="Fields marked with an asterisk are required."
          >
            <div className="form-grid">
              <Field
                className="form-field form-field--wide"
                label="Request title"
                required
                hint="Use a concise title that clearly describes the request."
              >
                <Input
                  name="title"
                  placeholder="For example, Replace finance team laptops"
                  maxLength={120}
                  required
                />
              </Field>

              <Field className="form-field" label="Category" required>
                <Dropdown
                  name="category"
                  placeholder="Select a category"
                  aria-required="true"
                >
                  <Option value="procurement">Procurement</Option>
                  <Option value="finance">Finance</Option>
                  <Option value="human-resources">Human Resources</Option>
                  <Option value="information-technology">
                    Information Technology
                  </Option>
                  <Option value="facilities">Facilities</Option>
                  <Option value="legal">Legal</Option>
                  <Option value="general">General</Option>
                </Dropdown>
              </Field>

              <Field className="form-field" label="Priority" required>
                <Dropdown
                  name="priority"
                  defaultValue="Normal"
                  defaultSelectedOptions={['normal']}
                  aria-required="true"
                >
                  <Option value="low">Low</Option>
                  <Option value="normal">Normal</Option>
                  <Option value="high">High</Option>
                  <Option value="urgent">Urgent</Option>
                </Dropdown>
              </Field>

              <Field
                className="form-field form-field--wide"
                label="Description"
                required
                hint="Include the purpose, scope, and expected outcome."
              >
                <Textarea
                  name="description"
                  placeholder="Describe what you need approved"
                  resize="vertical"
                  rows={5}
                  required
                />
              </Field>
            </div>
          </SectionPanel>

          <SectionPanel
            title="Financial information"
            description="Add an estimated value when the request has a financial impact."
          >
            <div className="form-grid form-grid--financial">
              <Field className="form-field" label="Estimated amount">
                <Input
                  name="amount"
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="0.00"
                />
              </Field>
              <Field className="form-field" label="Currency">
                <Dropdown
                  name="currency"
                  defaultValue="USD"
                  defaultSelectedOptions={['USD']}
                >
                  <Option value="USD">USD</Option>
                  <Option value="EGP">EGP</Option>
                  <Option value="EUR">EUR</Option>
                  <Option value="GBP">GBP</Option>
                </Dropdown>
              </Field>
              <Field
                className="form-field form-field--wide"
                label="Business justification"
                hint="Explain the value, urgency, or risk addressed by this request."
              >
                <Textarea
                  name="justification"
                  placeholder="Add context for reviewers"
                  resize="vertical"
                  rows={4}
                />
              </Field>
            </div>
          </SectionPanel>

          <SectionPanel
            title="Supporting documents"
            description="Attach quotations, specifications, or other relevant files."
          >
            <div className="attachment-dropzone">
              <div className="attachment-dropzone__icon" aria-hidden="true">
                <DocumentAdd24Regular />
              </div>
              <div className="attachment-dropzone__copy">
                <h3>Attach supporting files</h3>
                <p>
                  File upload will be connected to on-premises storage in a later
                  stage.
                </p>
              </div>
              <Button
                type="button"
                appearance="outline"
                icon={<Attach20Regular />}
              >
                Choose files
              </Button>
            </div>
          </SectionPanel>

          <div className="request-form__footer">
            <p className="request-form__notice">
              <Info20Regular aria-hidden="true" />
              No data is submitted in Stage 1. These controls demonstrate the
              future request workflow.
            </p>
            <div className="request-form__actions">
              <Button type="reset" appearance="secondary">
                Clear form
              </Button>
              <Button
                type="button"
                appearance="outline"
                icon={<Save20Regular />}
              >
                Save draft
              </Button>
              <Button
                type="button"
                appearance="primary"
                icon={<CheckmarkCircle24Regular />}
              >
                Submit request
              </Button>
            </div>
          </div>
        </form>

        <aside className="request-guidance" aria-label="Request guidance">
          <div className="request-guidance__icon" aria-hidden="true">
            <Info20Regular />
          </div>
          <h2>Before you submit</h2>
          <ol className="request-guidance__steps">
            <li>Confirm the request category and priority.</li>
            <li>Explain what is needed and why it matters.</li>
            <li>Attach evidence that helps approvers decide.</li>
            <li>Review all details before starting the workflow.</li>
          </ol>
          <p>
            The application will select an approval workflow based on the request
            category and value.
          </p>
        </aside>
      </div>
    </div>
  )
}
