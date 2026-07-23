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

  if (error.problem.code === 'wallet.already_exists') {
    return 'Bu para biriminde zaten bir cüzdanınız var.'
  }

  if (error.problem.code === 'wallet.not_found') {
    return 'Cüzdan bulunamadı. Lütfen sayfayı yenileyin.'
  }

  if (error.problem.code === 'wallet.transaction_reference_conflict') {
    return 'Bu işlem referansı daha önce kullanılmış.'
  }

  const validationMessage = Object.values(error.problem.errors ?? {}).flat()[0]
  return validationMessage ?? error.message
}
