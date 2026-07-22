import { ApiError } from './api'

export function getErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Beklenmeyen bir hata oluştu. Lütfen yeniden deneyin.'
  }

  if (error.problem.code === 'customer.invalid_credentials') {
    return 'E-posta adresi veya parola hatalı.'
  }

  if (error.problem.code === 'customer.duplicate_email') {
    return 'Bu e-posta adresiyle daha önce hesap oluşturulmuş.'
  }

  const validationMessage = Object.values(error.problem.errors ?? {}).flat()[0]
  return validationMessage ?? error.message
}
