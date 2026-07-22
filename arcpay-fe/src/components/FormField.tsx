import { useId, type InputHTMLAttributes } from 'react'

interface FormFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
  hint?: string
}

export function FormField({ label, error, hint, id, ...inputProps }: FormFieldProps) {
  const generatedId = useId()
  const inputId = id ?? generatedId
  const helperId = `${inputId}-helper`

  return (
    <div className={`field ${error ? 'field-error' : ''}`}>
      <label htmlFor={inputId}>{label}</label>
      <input
        id={inputId}
        aria-invalid={Boolean(error)}
        aria-describedby={error || hint ? helperId : undefined}
        {...inputProps}
      />
      {(error || hint) && (
        <span id={helperId} className="field-helper">
          {error ?? hint}
        </span>
      )}
    </div>
  )
}
