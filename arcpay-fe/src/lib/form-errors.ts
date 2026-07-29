import { ApiError } from './api'

export function getErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Beklenmeyen bir hata oluştu. Lütfen yeniden deneyin.'
  }

  const code = error.problem.code?.toLowerCase()

  if (code === 'customer.invalid_credentials') {
    return 'E-posta adresi veya parola hatalı.'
  }

  if (code === 'customer.duplicateemail' || code === 'customer.duplicate_email') {
    return 'Bu e-posta adresiyle daha önce hesap oluşturulmuş.'
  }

  if (code === 'customer.duplicatephone' || code === 'customer.duplicate_phone') {
    return 'Bu telefon numarasıyla daha önce hesap oluşturulmuş.'
  }

  if (code === 'customer.notfound' || code === 'customer.not_found') {
    return 'Bu müşteri numarası, e-posta veya telefonla eşleşen alıcı bulunamadı.'
  }

  if (code === 'wallet.already_exists') {
    return 'Bu para biriminde zaten bir cüzdanınız var.'
  }

  if (code === 'wallet.not_found') {
    return 'Cüzdan bulunamadı. Lütfen sayfayı yenileyin.'
  }

  if (code === 'wallet.transaction_reference_conflict') {
    return 'Bu işlem referansı daha önce kullanılmış.'
  }

  if (code === 'wallet.balance_must_be_zero') {
    return 'Cüzdanı kapatmadan önce bakiyeyi sıfırlamalısınız.'
  }

  if (code === 'wallet.insufficient_funds') {
    return 'Bu işlem için cüzdan bakiyesi yetersiz.'
  }

  if (code === 'investment.purchase_compensated') {
    return 'Portföy kaydı tamamlanamadı; tahsil edilen tutar cüzdanınıza otomatik olarak iade edildi.'
  }

  if (code === 'investment.compensation_failed') {
    return 'İade otomatik tamamlanamadı. İşlem inceleme kuyruğuna alındı.'
  }

  if (code === 'investment.quote_unavailable') {
    return 'Piyasa fiyatı şu anda alınamıyor. Lütfen kısa süre sonra yeniden deneyin.'
  }

  const validationMessage = Object.values(error.problem.errors ?? {}).flat()[0]
  return validationMessage ?? error.message
}
