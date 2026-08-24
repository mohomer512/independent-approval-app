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
  Input,
  MessageBar,
  MessageBarBody,
  Spinner,
  Textarea,
} from '@fluentui/react-components'
import { Add20Regular, Delete20Regular } from '@fluentui/react-icons'
import { useEffect, useRef, useState } from 'react'
import type {
  RequestDocumentMode,
  RequestFieldChoiceOption,
  RequestFieldDefaultValue,
  RequestFieldType,
  RequestFieldValidationConfig,
  RequestTypeField,
  RequestTypeVersion,
} from '../../../models'
import {
  ApiError,
  requestTypeAdminService,
} from '../../../services'
import {
  documentModeOptions,
  getRequestTypeApiErrorMessage,
  isChoiceField,
  isDocumentField,
  requestFieldTypeOptions,
} from './requestTypeUi'

interface RequestTypeFieldDialogProps {
  readonly requestTypeId: string
  readonly versionId: string
  readonly versionRowVersion: string
  readonly field: RequestTypeField | null
  readonly onClose: () => void
  readonly onSaved: (version: RequestTypeVersion) => void
}

interface ChoiceOptionDraft extends RequestFieldChoiceOption {
  readonly editorId: string
}

interface ValidationInputs {
  readonly minLength: string
  readonly maxLength: string
  readonly pattern: string
  readonly minimum: string
  readonly maximum: string
  readonly decimalPlaces: string
  readonly minimumSelections: string
  readonly maximumSelections: string
  readonly maximumDocuments: string
  readonly allowedExtensions: string
}

interface FormErrors {
  key?: string
  labelEnglish?: string
  labelArabic?: string
  sortOrder?: string
  defaultValue?: string
  validation?: string
  choices?: string
}

const emptyValidationInputs: ValidationInputs = {
  minLength: '',
  maxLength: '',
  pattern: '',
  minimum: '',
  maximum: '',
  decimalPlaces: '',
  minimumSelections: '',
  maximumSelections: '',
  maximumDocuments: '',
  allowedExtensions: '',
}

function createEditorId(): string {
  return `choice-${Date.now()}-${Math.random().toString(36).slice(2)}`
}

function getInitialValidationInputs(
  fieldType: RequestFieldType,
  validation: RequestFieldValidationConfig | null,
): ValidationInputs {
  if (!validation) {
    return emptyValidationInputs
  }

  return {
    minLength: validation.minLength?.toString() ?? '',
    maxLength: validation.maxLength?.toString() ?? '',
    pattern: validation.pattern ?? '',
    minimum:
      fieldType === 'dateTime' && typeof validation.minimum === 'string'
        ? formatDateTimeInput(validation.minimum)
        : validation.minimum?.toString() ?? '',
    maximum:
      fieldType === 'dateTime' && typeof validation.maximum === 'string'
        ? formatDateTimeInput(validation.maximum)
        : validation.maximum?.toString() ?? '',
    decimalPlaces: validation.decimalPlaces?.toString() ?? '',
    minimumSelections: validation.minimumSelections?.toString() ?? '',
    maximumSelections: validation.maximumSelections?.toString() ?? '',
    maximumDocuments: validation.maximumDocuments?.toString() ?? '',
    allowedExtensions: validation.allowedExtensions?.join(', ') ?? '',
  }
}

function formatDateTimeInput(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value.slice(0, 16)
  }

  const localTime = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return localTime.toISOString().slice(0, 16)
}

function toIsoDateTime(value: string): string {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toISOString()
}

function getInitialDefaultText(
  fieldType: RequestFieldType,
  value: RequestFieldDefaultValue,
): string {
  if (typeof value === 'string' || typeof value === 'number') {
    if (fieldType === 'dateTime' && typeof value === 'string') {
      return formatDateTimeInput(value)
    }

    return String(value)
  }

  return ''
}

function readOptionalNumber(value: string): number | undefined {
  const normalized = value.trim()
  return normalized ? Number(normalized) : undefined
}

function readOptionalInteger(value: string): number | undefined {
  const numberValue = readOptionalNumber(value)
  return numberValue === undefined ? undefined : Math.trunc(numberValue)
}

function removeUndefinedValues(
  values: RequestFieldValidationConfig,
): RequestFieldValidationConfig | null {
  return Object.values(values).some((value) => value !== undefined)
    ? values
    : null
}

function buildValidationConfig(
  fieldType: RequestFieldType,
  inputs: ValidationInputs,
): RequestFieldValidationConfig | null {
  if (fieldType === 'shortText' || fieldType === 'longText') {
    return removeUndefinedValues({
      minLength: readOptionalInteger(inputs.minLength),
      maxLength: readOptionalInteger(inputs.maxLength),
      pattern: inputs.pattern.trim() || undefined,
    })
  }

  if (fieldType === 'integer') {
    return removeUndefinedValues({
      minimum: readOptionalInteger(inputs.minimum),
      maximum: readOptionalInteger(inputs.maximum),
    })
  }

  if (fieldType === 'decimal') {
    return removeUndefinedValues({
      minimum: readOptionalNumber(inputs.minimum),
      maximum: readOptionalNumber(inputs.maximum),
      decimalPlaces: readOptionalInteger(inputs.decimalPlaces),
    })
  }

  if (fieldType === 'date' || fieldType === 'dateTime') {
    return removeUndefinedValues({
      minimum:
        fieldType === 'dateTime' && inputs.minimum.trim()
          ? toIsoDateTime(inputs.minimum)
          : inputs.minimum.trim() || undefined,
      maximum:
        fieldType === 'dateTime' && inputs.maximum.trim()
          ? toIsoDateTime(inputs.maximum)
          : inputs.maximum.trim() || undefined,
    })
  }

  if (fieldType === 'multipleChoice') {
    return removeUndefinedValues({
      minimumSelections: readOptionalInteger(inputs.minimumSelections),
      maximumSelections: readOptionalInteger(inputs.maximumSelections),
    })
  }

  if (isDocumentField(fieldType)) {
    const extensions = inputs.allowedExtensions
      .split(',')
      .map((extension) => extension.trim())
      .filter(Boolean)

    return removeUndefinedValues({
      maximumDocuments: readOptionalInteger(inputs.maximumDocuments),
      allowedExtensions: extensions.length > 0 ? extensions : undefined,
    })
  }

  return null
}

function parseDefaultValue(
  fieldType: RequestFieldType,
  defaultText: string,
  defaultBoolean: string,
  multipleDefaults: readonly string[],
): RequestFieldDefaultValue {
  if (fieldType === 'yesNo') {
    if (defaultBoolean === 'true') {
      return true
    }

    if (defaultBoolean === 'false') {
      return false
    }

    return null
  }

  if (fieldType === 'multipleChoice') {
    return multipleDefaults.length > 0 ? multipleDefaults : null
  }

  const value = defaultText.trim()
  if (!value) {
    return null
  }

  if (fieldType === 'integer' || fieldType === 'decimal') {
    return Number(value)
  }

  if (
    fieldType === 'activeDirectoryUser' ||
    fieldType === 'applicationRole' ||
    isDocumentField(fieldType)
  ) {
    return null
  }

  if (fieldType === 'dateTime') {
    return toIsoDateTime(value)
  }

  return value
}

function validateNumericPair(
  minimum: string,
  maximum: string,
  wholeNumbers: boolean,
): string | undefined {
  const values = [minimum, maximum].filter((value) => value.trim())

  if (
    values.some((value) => {
      const parsed = Number(value)
      return !Number.isFinite(parsed) || (wholeNumbers && !Number.isInteger(parsed))
    })
  ) {
    return wholeNumbers
      ? 'Enter whole numbers for the validation limits.'
      : 'Enter valid numbers for the validation limits.'
  }

  if (
    minimum.trim() &&
    maximum.trim() &&
    Number(minimum) > Number(maximum)
  ) {
    return 'The minimum cannot be greater than the maximum.'
  }

  return undefined
}

export function RequestTypeFieldDialog({
  requestTypeId,
  versionId,
  versionRowVersion,
  field,
  onClose,
  onSaved,
}: RequestTypeFieldDialogProps) {
  const [key, setKey] = useState(field?.key ?? '')
  const [labelEnglish, setLabelEnglish] = useState(field?.labelEnglish ?? '')
  const [labelArabic, setLabelArabic] = useState(field?.labelArabic ?? '')
  const [helpTextEnglish, setHelpTextEnglish] = useState(
    field?.helpTextEnglish ?? '',
  )
  const [helpTextArabic, setHelpTextArabic] = useState(
    field?.helpTextArabic ?? '',
  )
  const [fieldType, setFieldType] = useState<RequestFieldType>(
    field?.fieldType ?? 'shortText',
  )
  const [isRequired, setIsRequired] = useState(field?.isRequired ?? false)
  const [isActive, setIsActive] = useState(field?.isActive ?? true)
  const [sortOrder, setSortOrder] = useState(
    field?.sortOrder.toString() ?? '0',
  )
  const [documentMode, setDocumentMode] = useState<RequestDocumentMode>(
    field?.documentMode ?? 'uploadOnly',
  )
  const [defaultText, setDefaultText] = useState(
    getInitialDefaultText(field?.fieldType ?? 'shortText', field?.defaultValue ?? null),
  )
  const [defaultBoolean, setDefaultBoolean] = useState(
    typeof field?.defaultValue === 'boolean'
      ? String(field.defaultValue)
      : 'unset',
  )
  const [multipleDefaults, setMultipleDefaults] = useState<readonly string[]>(
    Array.isArray(field?.defaultValue)
      ? field.defaultValue.filter(
          (value): value is string => typeof value === 'string',
        )
      : [],
  )
  const [validation, setValidation] = useState<ValidationInputs>(() =>
    getInitialValidationInputs(
      field?.fieldType ?? 'shortText',
      field?.validationConfig ?? null,
    ),
  )
  const [choices, setChoices] = useState<readonly ChoiceOptionDraft[]>(() =>
    (field?.choiceConfig?.options ?? []).map((option) => ({
      ...option,
      editorId: createEditorId(),
    })),
  )
  const [errors, setErrors] = useState<FormErrors>({})
  const [submissionError, setSubmissionError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const requestRef = useRef<AbortController | null>(null)
  const mountedRef = useRef(true)
  const editing = field !== null

  useEffect(() => {
    mountedRef.current = true

    return () => {
      mountedRef.current = false
      requestRef.current?.abort()
      requestRef.current = null
    }
  }, [])

  const updateValidation = (name: keyof ValidationInputs, value: string) => {
    setValidation((current) => ({ ...current, [name]: value }))
  }

  const updateChoice = (
    editorId: string,
    values: Partial<RequestFieldChoiceOption>,
  ) => {
    setChoices((current) =>
      current.map((choice) =>
        choice.editorId === editorId ? { ...choice, ...values } : choice,
      ),
    )
  }

  const validate = (): FormErrors => {
    const nextErrors: FormErrors = {}

    if (!editing && !/^[a-z][a-z0-9_]{0,63}$/.test(key.trim())) {
      nextErrors.key =
        'Start with a lowercase letter and use up to 64 lowercase letters, numbers, or underscores.'
    }

    if (!labelEnglish.trim()) {
      nextErrors.labelEnglish = 'Enter the English field label.'
    }

    if (!labelArabic.trim()) {
      nextErrors.labelArabic = 'Enter the Arabic field label.'
    }

    if (
      !Number.isInteger(Number(sortOrder)) ||
      Number(sortOrder) < 0 ||
      Number(sortOrder) > 100000
    ) {
      nextErrors.sortOrder = 'Enter a whole number from 0 to 100,000.'
    }

    if (defaultText.trim()) {
      const parsed = Number(defaultText)
      if (fieldType === 'integer' && !Number.isInteger(parsed)) {
        nextErrors.defaultValue = 'Enter a whole-number default value.'
      } else if (fieldType === 'decimal' && !Number.isFinite(parsed)) {
        nextErrors.defaultValue = 'Enter a valid decimal default value.'
      }
    }

    if (fieldType === 'shortText' || fieldType === 'longText') {
      nextErrors.validation = validateNumericPair(
        validation.minLength,
        validation.maxLength,
        true,
      )
    } else if (fieldType === 'integer') {
      nextErrors.validation = validateNumericPair(
        validation.minimum,
        validation.maximum,
        true,
      )
    } else if (fieldType === 'decimal') {
      nextErrors.validation = validateNumericPair(
        validation.minimum,
        validation.maximum,
        false,
      )
      if (
        validation.decimalPlaces.trim() &&
        (!Number.isInteger(Number(validation.decimalPlaces)) ||
          Number(validation.decimalPlaces) < 0 ||
          Number(validation.decimalPlaces) > 8)
      ) {
        nextErrors.validation =
          'Decimal places must be a whole number from 0 to 8.'
      }
    } else if (fieldType === 'date' || fieldType === 'dateTime') {
      if (
        validation.minimum &&
        validation.maximum &&
        validation.minimum > validation.maximum
      ) {
        nextErrors.validation = 'The minimum date cannot be after the maximum date.'
      }
    } else if (fieldType === 'multipleChoice') {
      nextErrors.validation = validateNumericPair(
        validation.minimumSelections,
        validation.maximumSelections,
        true,
      )
    } else if (
      isDocumentField(fieldType) &&
      validation.maximumDocuments.trim() &&
      (!Number.isInteger(Number(validation.maximumDocuments)) ||
        Number(validation.maximumDocuments) < 1 ||
        Number(validation.maximumDocuments) > 100)
    ) {
      nextErrors.validation = 'Maximum documents must be a whole number from 1 to 100.'
    } else if (isDocumentField(fieldType) && validation.allowedExtensions.trim()) {
      const extensions = validation.allowedExtensions
        .split(',')
        .map((extension) => extension.trim())
        .filter(Boolean)
      const normalizedExtensions = extensions.map((extension) =>
        extension.toLowerCase(),
      )

      if (
        extensions.length > 20 ||
        extensions.some((extension) => !/^\.[A-Za-z0-9]{1,15}$/.test(extension)) ||
        new Set(normalizedExtensions).size !== normalizedExtensions.length
      ) {
        nextErrors.validation =
          'Enter up to 20 unique extensions in a format such as .pdf.'
      }
    }

    if (isChoiceField(fieldType)) {
      const normalizedChoices = choices.map((choice) => ({
        ...choice,
        value: choice.value.trim(),
        labelEnglish: choice.labelEnglish.trim(),
        labelArabic: choice.labelArabic.trim(),
      }))
      const values = normalizedChoices.map((choice) => choice.value.toLowerCase())
      const orders = normalizedChoices.map((choice) => choice.sortOrder)

      if (normalizedChoices.length === 0 || normalizedChoices.length > 200) {
        nextErrors.choices = 'Add between 1 and 200 choices.'
      } else if (
        normalizedChoices.some(
          (choice) =>
            !choice.value ||
            choice.value.length > 128 ||
            !choice.labelEnglish ||
            !choice.labelArabic ||
            !Number.isInteger(choice.sortOrder) ||
            choice.sortOrder < 0 ||
            choice.sortOrder > 100000,
        )
      ) {
        nextErrors.choices =
          'Every choice needs a value, both labels, and a valid whole-number order.'
      } else if (new Set(values).size !== values.length) {
        nextErrors.choices = 'Choice values must be unique.'
      } else if (new Set(orders).size !== orders.length) {
        nextErrors.choices = 'Choice sort orders must be unique.'
      } else if (!normalizedChoices.some((choice) => choice.isActive)) {
        nextErrors.choices = 'Keep at least one choice active.'
      }

      const activeValues = new Set(
        normalizedChoices
          .filter((choice) => choice.isActive)
          .map((choice) => choice.value),
      )
      if (
        fieldType === 'singleChoice' &&
        defaultText &&
        !activeValues.has(defaultText)
      ) {
        nextErrors.defaultValue = 'Select an active choice as the default.'
      } else if (
        fieldType === 'multipleChoice' &&
        multipleDefaults.some((value) => !activeValues.has(value))
      ) {
        nextErrors.defaultValue = 'Every default must be an active choice.'
      } else if (fieldType === 'multipleChoice') {
        const minimumSelections = readOptionalInteger(
          validation.minimumSelections,
        )
        const maximumSelections = readOptionalInteger(
          validation.maximumSelections,
        )
        if (
          (minimumSelections !== undefined &&
            multipleDefaults.length < minimumSelections) ||
          (maximumSelections !== undefined &&
            multipleDefaults.length > maximumSelections)
        ) {
          nextErrors.defaultValue =
            'The selected defaults must satisfy the selection limits.'
        }
      }
    }

    return nextErrors
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (requestRef.current) {
      return
    }

    const nextErrors = validate()
    setErrors(nextErrors)
    if (Object.values(nextErrors).some(Boolean)) {
      return
    }

    const choiceOptions = isChoiceField(fieldType)
      ? choices.map((choice) => ({
          value: choice.value.trim(),
          labelEnglish: choice.labelEnglish.trim(),
          labelArabic: choice.labelArabic.trim(),
          sortOrder: choice.sortOrder,
          isActive: choice.isActive,
        }))
      : null
    const values = {
      labelEnglish: labelEnglish.trim(),
      labelArabic: labelArabic.trim(),
      helpTextEnglish: helpTextEnglish.trim(),
      helpTextArabic: helpTextArabic.trim(),
      fieldType,
      isRequired,
      defaultValue: parseDefaultValue(
        fieldType,
        defaultText,
        defaultBoolean,
        multipleDefaults,
      ),
      validationConfig: buildValidationConfig(fieldType, validation),
      choiceConfig: choiceOptions ? { options: choiceOptions } : null,
      sortOrder: Number(sortOrder),
      isActive,
      documentMode: isDocumentField(fieldType) ? documentMode : null,
    }
    const controller = new AbortController()
    requestRef.current = controller
    setSubmitting(true)
    setSubmissionError(null)
    setConflict(false)

    try {
      const savedVersion = editing
        ? await requestTypeAdminService.updateField(
            requestTypeId,
            versionId,
            field.id,
            {
              ...values,
              versionRowVersion,
              fieldRowVersion: field.rowVersion,
            },
            controller.signal,
          )
        : await requestTypeAdminService.addField(
            requestTypeId,
            versionId,
            { ...values, key: key.trim(), versionRowVersion },
            controller.signal,
          )

      if (!controller.signal.aborted && mountedRef.current) {
        onSaved(savedVersion)
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted && mountedRef.current) {
        setConflict(error instanceof ApiError && error.status === 409)
        setSubmissionError(
          getRequestTypeApiErrorMessage(
            error,
            'The custom field could not be saved. Please try again.',
          ),
        )
      }
    } finally {
      if (requestRef.current === controller) {
        requestRef.current = null
        if (mountedRef.current) {
          setSubmitting(false)
        }
      }
    }
  }

  const renderDefaultEditor = () => {
    if (fieldType === 'yesNo') {
      return (
        <Field label="Default value">
          <select
            className="request-type-select"
            aria-label="Default value"
            value={defaultBoolean}
            disabled={submitting}
            onChange={(event) => setDefaultBoolean(event.target.value)}
          >
            <option value="unset">No default</option>
            <option value="true">Yes</option>
            <option value="false">No</option>
          </select>
        </Field>
      )
    }

    if (fieldType === 'singleChoice') {
      return (
        <Field
          label="Default choice"
          validationState={errors.defaultValue ? 'error' : 'none'}
          validationMessage={errors.defaultValue}
        >
          <select
            className="request-type-select"
            aria-label="Default choice"
            value={defaultText}
            disabled={submitting}
            onChange={(event) => setDefaultText(event.target.value)}
          >
            <option value="">No default</option>
            {choices
              .filter((choice) => choice.isActive && choice.value.trim())
              .map((choice) => (
                <option key={choice.editorId} value={choice.value.trim()}>
                  {choice.labelEnglish || choice.value}
                </option>
              ))}
          </select>
        </Field>
      )
    }

    if (fieldType === 'multipleChoice') {
      return (
        <fieldset className="request-type-default-choices">
          <legend>Default choices</legend>
          {choices.filter((choice) => choice.isActive).length === 0 ? (
            <p>Add an active choice before selecting defaults.</p>
          ) : (
            choices
              .filter((choice) => choice.isActive)
              .map((choice) => (
                <Checkbox
                  key={choice.editorId}
                  label={choice.labelEnglish || choice.value || 'Unnamed choice'}
                  checked={multipleDefaults.includes(choice.value.trim())}
                  disabled={submitting || !choice.value.trim()}
                  onChange={(_, data) => {
                    const value = choice.value.trim()
                    setMultipleDefaults((current) =>
                      data.checked === true
                        ? [...new Set([...current, value])]
                        : current.filter((item) => item !== value),
                    )
                  }}
                />
              ))
          )}
          {errors.defaultValue ? (
            <span className="request-type-form-error" role="alert">
              {errors.defaultValue}
            </span>
          ) : null}
        </fieldset>
      )
    }

    if (
      fieldType === 'activeDirectoryUser' ||
      fieldType === 'applicationRole' ||
      isDocumentField(fieldType)
    ) {
      return (
        <p className="request-type-form-note">
          This field type does not accept a static default value.
        </p>
      )
    }

    const inputType =
      fieldType === 'integer' || fieldType === 'decimal'
        ? 'number'
        : fieldType === 'date'
          ? 'date'
          : fieldType === 'dateTime'
            ? 'datetime-local'
            : 'text'

    return (
      <Field
        label="Default value"
        validationState={errors.defaultValue ? 'error' : 'none'}
        validationMessage={errors.defaultValue}
      >
        <Input
          type={inputType}
          step={fieldType === 'integer' ? 1 : fieldType === 'decimal' ? 'any' : undefined}
          value={defaultText}
          disabled={submitting}
          onChange={(_, data) => setDefaultText(data.value)}
        />
      </Field>
    )
  }

  const renderValidationEditor = () => {
    const numberInput = (
      label: string,
      name: keyof ValidationInputs,
      minimum?: number,
    ) => (
      <Field label={label}>
        <Input
          type="number"
          min={minimum}
          step={1}
          value={validation[name]}
          disabled={submitting}
          onChange={(_, data) => updateValidation(name, data.value)}
        />
      </Field>
    )

    if (fieldType === 'shortText' || fieldType === 'longText') {
      return (
        <>
          {numberInput('Minimum length', 'minLength', 0)}
          {numberInput('Maximum length', 'maxLength', 1)}
          <Field label="Pattern" hint="Optional regular expression.">
            <Input
              value={validation.pattern}
              maxLength={512}
              disabled={submitting}
              onChange={(_, data) => updateValidation('pattern', data.value)}
            />
          </Field>
        </>
      )
    }

    if (fieldType === 'integer' || fieldType === 'decimal') {
      return (
        <>
          <Field label="Minimum">
            <Input
              type="number"
              step={fieldType === 'integer' ? 1 : 'any'}
              value={validation.minimum}
              disabled={submitting}
              onChange={(_, data) => updateValidation('minimum', data.value)}
            />
          </Field>
          <Field label="Maximum">
            <Input
              type="number"
              step={fieldType === 'integer' ? 1 : 'any'}
              value={validation.maximum}
              disabled={submitting}
              onChange={(_, data) => updateValidation('maximum', data.value)}
            />
          </Field>
          {fieldType === 'decimal'
            ? numberInput('Decimal places', 'decimalPlaces', 0)
            : null}
        </>
      )
    }

    if (fieldType === 'date' || fieldType === 'dateTime') {
      const dateType = fieldType === 'date' ? 'date' : 'datetime-local'
      return (
        <>
          <Field label="Earliest value">
            <Input
              type={dateType}
              value={validation.minimum}
              disabled={submitting}
              onChange={(_, data) => updateValidation('minimum', data.value)}
            />
          </Field>
          <Field label="Latest value">
            <Input
              type={dateType}
              value={validation.maximum}
              disabled={submitting}
              onChange={(_, data) => updateValidation('maximum', data.value)}
            />
          </Field>
        </>
      )
    }

    if (fieldType === 'multipleChoice') {
      return (
        <>
          {numberInput('Minimum selections', 'minimumSelections', 0)}
          {numberInput('Maximum selections', 'maximumSelections', 1)}
        </>
      )
    }

    if (isDocumentField(fieldType)) {
      return (
        <>
          {numberInput('Maximum documents', 'maximumDocuments', 1)}
          <Field
            label="Allowed extensions"
            hint="Comma-separated, for example .pdf, .docx."
          >
            <Input
              value={validation.allowedExtensions}
              disabled={submitting}
              onChange={(_, data) =>
                updateValidation('allowedExtensions', data.value)
              }
            />
          </Field>
        </>
      )
    }

    return (
      <p className="request-type-form-note">
        This field type has no configurable validation rules.
      </p>
    )
  }

  return (
    <Dialog
      open
      onOpenChange={(_, data) => {
        if (!data.open && !submitting) {
          onClose()
        }
      }}
    >
      <DialogSurface className="administration-dialog request-type-field-dialog">
        <form aria-busy={submitting} noValidate onSubmit={handleSubmit}>
          <DialogBody>
            <DialogTitle>{editing ? 'Edit custom field' : 'Add custom field'}</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p className="administration-dialog__introduction">
                Custom fields belong only to this draft version. Protected system
                fields are managed by the application and cannot be added here.
              </p>

              {submissionError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>
                    {submissionError}
                    {conflict
                      ? ' Reload the designer before trying again.'
                      : ''}
                  </MessageBarBody>
                </MessageBar>
              ) : null}

              <section className="request-type-editor-section">
                <div className="request-type-editor-section__heading">
                  <h3>Identity and labels</h3>
                  <p>The key is stable after the field is created.</p>
                </div>
                <div className="administration-form-grid">
                  <Field
                    label="Stable key"
                    required
                    validationState={errors.key ? 'error' : 'none'}
                    validationMessage={errors.key}
                  >
                    <Input
                      autoFocus={!editing}
                      value={key}
                      maxLength={64}
                      readOnly={editing}
                      disabled={submitting}
                      onChange={(_, data) => setKey(data.value)}
                    />
                  </Field>
                  <Field label="Field type" required>
                    <select
                      className="request-type-select"
                      aria-label="Field type"
                      value={fieldType}
                      disabled={submitting}
                      onChange={(event) => {
                        setFieldType(event.target.value as RequestFieldType)
                        setDefaultText('')
                        setDefaultBoolean('unset')
                        setMultipleDefaults([])
                        setValidation(emptyValidationInputs)
                        setErrors({})
                      }}
                    >
                      {requestFieldTypeOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </Field>
                  <Field
                    label="English label"
                    required
                    validationState={errors.labelEnglish ? 'error' : 'none'}
                    validationMessage={errors.labelEnglish}
                  >
                    <Input
                      value={labelEnglish}
                      maxLength={200}
                      disabled={submitting}
                      onChange={(_, data) => setLabelEnglish(data.value)}
                    />
                  </Field>
                  <Field
                    label="Arabic label"
                    required
                    validationState={errors.labelArabic ? 'error' : 'none'}
                    validationMessage={errors.labelArabic}
                  >
                    <Input
                      dir="rtl"
                      lang="ar"
                      value={labelArabic}
                      maxLength={200}
                      disabled={submitting}
                      onChange={(_, data) => setLabelArabic(data.value)}
                    />
                  </Field>
                  <Field label="English help text">
                    <Textarea
                      value={helpTextEnglish}
                      maxLength={1000}
                      resize="vertical"
                      disabled={submitting}
                      onChange={(_, data) => setHelpTextEnglish(data.value)}
                    />
                  </Field>
                  <Field label="Arabic help text">
                    <Textarea
                      dir="rtl"
                      lang="ar"
                      value={helpTextArabic}
                      maxLength={1000}
                      resize="vertical"
                      disabled={submitting}
                      onChange={(_, data) => setHelpTextArabic(data.value)}
                    />
                  </Field>
                </div>
              </section>

              <section className="request-type-editor-section">
                <div className="request-type-editor-section__heading">
                  <h3>Behavior</h3>
                  <p>Define ordering, availability, and document behavior.</p>
                </div>
                <div className="administration-form-grid">
                  <Field
                    label="Sort order"
                    required
                    validationState={errors.sortOrder ? 'error' : 'none'}
                    validationMessage={errors.sortOrder}
                  >
                    <Input
                      type="number"
                      min={0}
                      max={100000}
                      step={1}
                      value={sortOrder}
                      disabled={submitting}
                      onChange={(_, data) => setSortOrder(data.value)}
                    />
                  </Field>
                  {isDocumentField(fieldType) ? (
                    <Field label="Document mode" required>
                      <select
                        className="request-type-select"
                        aria-label="Document mode"
                        value={documentMode}
                        disabled={submitting}
                        onChange={(event) =>
                          setDocumentMode(event.target.value as RequestDocumentMode)
                        }
                      >
                        {documentModeOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </Field>
                  ) : null}
                  <div className="request-type-checkboxes">
                    <Checkbox
                      label="Required"
                      checked={isRequired}
                      disabled={submitting}
                      onChange={(_, data) => setIsRequired(data.checked === true)}
                    />
                    <Checkbox
                      label="Active"
                      checked={isActive}
                      disabled={submitting}
                      onChange={(_, data) => setIsActive(data.checked === true)}
                    />
                  </div>
                </div>
              </section>

              {isChoiceField(fieldType) ? (
                <section className="request-type-editor-section">
                  <div className="request-type-editor-section__heading request-type-editor-section__heading--actions">
                    <div>
                      <h3>Choice options</h3>
                      <p>Values and sort orders must be unique.</p>
                    </div>
                    <Button
                      type="button"
                      appearance="outline"
                      icon={<Add20Regular />}
                      disabled={submitting}
                      onClick={() =>
                        setChoices((current) => [
                          ...current,
                          {
                            editorId: createEditorId(),
                            value: '',
                            labelEnglish: '',
                            labelArabic: '',
                            sortOrder: current.length,
                            isActive: true,
                          },
                        ])
                      }
                    >
                      Add choice
                    </Button>
                  </div>
                  {errors.choices ? (
                    <MessageBar intent="error" politeness="assertive">
                      <MessageBarBody>{errors.choices}</MessageBarBody>
                    </MessageBar>
                  ) : null}
                  <div className="request-type-choice-list">
                    {choices.length === 0 ? (
                      <p className="request-type-form-note">No choices added yet.</p>
                    ) : (
                      choices.map((choice, index) => (
                        <fieldset
                          className="request-type-choice-option"
                          key={choice.editorId}
                        >
                          <legend>Choice {index + 1}</legend>
                          <Field label="Value" required>
                            <Input
                              value={choice.value}
                              maxLength={128}
                              disabled={submitting}
                              onChange={(_, data) =>
                                updateChoice(choice.editorId, { value: data.value })
                              }
                            />
                          </Field>
                          <Field label="English label" required>
                            <Input
                              value={choice.labelEnglish}
                              maxLength={200}
                              disabled={submitting}
                              onChange={(_, data) =>
                                updateChoice(choice.editorId, {
                                  labelEnglish: data.value,
                                })
                              }
                            />
                          </Field>
                          <Field label="Arabic label" required>
                            <Input
                              dir="rtl"
                              lang="ar"
                              value={choice.labelArabic}
                              maxLength={200}
                              disabled={submitting}
                              onChange={(_, data) =>
                                updateChoice(choice.editorId, {
                                  labelArabic: data.value,
                                })
                              }
                            />
                          </Field>
                          <Field label="Sort order" required>
                            <Input
                              type="number"
                              min={0}
                              max={100000}
                              step={1}
                              value={String(choice.sortOrder)}
                              disabled={submitting}
                              onChange={(_, data) =>
                                updateChoice(choice.editorId, {
                                  sortOrder: Number(data.value),
                                })
                              }
                            />
                          </Field>
                          <Checkbox
                            label="Active"
                            checked={choice.isActive}
                            disabled={submitting}
                            onChange={(_, data) =>
                              updateChoice(choice.editorId, {
                                isActive: data.checked === true,
                              })
                            }
                          />
                          <Button
                            type="button"
                            appearance="subtle"
                            icon={<Delete20Regular />}
                            disabled={submitting}
                            aria-label={`Remove choice ${index + 1}`}
                            onClick={() => {
                              setChoices((current) =>
                                current.filter(
                                  (item) => item.editorId !== choice.editorId,
                                ),
                              )
                              setMultipleDefaults((current) =>
                                current.filter((value) => value !== choice.value),
                              )
                            }}
                          >
                            Remove
                          </Button>
                        </fieldset>
                      ))
                    )}
                  </div>
                </section>
              ) : null}

              <section className="request-type-editor-section">
                <div className="request-type-editor-section__heading">
                  <h3>Default and validation</h3>
                  <p>Only rules supported by the selected field type are sent.</p>
                </div>
                <div className="administration-form-grid">
                  {renderDefaultEditor()}
                  {renderValidationEditor()}
                </div>
                {errors.validation ? (
                  <span className="request-type-form-error" role="alert">
                    {errors.validation}
                  </span>
                ) : null}
              </section>
            </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                disabled={submitting}
                onClick={onClose}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                appearance="primary"
                disabled={submitting}
                icon={submitting ? <Spinner size="tiny" /> : undefined}
              >
                {submitting ? 'Saving' : editing ? 'Save field' : 'Add field'}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
