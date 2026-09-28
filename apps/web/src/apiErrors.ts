import { ApiClientError } from '@lu/api-client';
import type { PasswordPolicyViolation, ProfilePhotoViolation } from '@lu/contracts';

/** MIG-001 F5: one mapping from the F4 typed error model to user-facing Spanish text. */

const PASSWORD_VIOLATIONS: Record<PasswordPolicyViolation, string> = {
  too_short: 'debe tener al menos 12 caracteres',
  too_long: 'no debe superar 72 bytes',
  missing_uppercase: 'debe incluir una mayúscula',
  missing_lowercase: 'debe incluir una minúscula',
  missing_digit: 'debe incluir un número',
  missing_symbol: 'debe incluir un símbolo',
  insufficient_distinct: 'debe usar al menos 4 caracteres distintos',
};

const PHOTO_VIOLATIONS: Record<ProfilePhotoViolation, string> = {
  empty: 'el archivo está vacío',
  too_large: 'la imagen supera 5 MB',
  invalid_file_name: 'el nombre del archivo no es válido',
  unsupported_extension: 'solo se admiten JPG, PNG o WebP',
  unsupported_media_type: 'solo se admiten imágenes JPEG, PNG o WebP',
  signature_mismatch: 'el contenido no corresponde a una imagen válida',
  type_mismatch: 'el tipo declarado no coincide con el contenido',
};

export function passwordViolationText(violations: readonly string[]): string {
  return violations
    .map((violation) => PASSWORD_VIOLATIONS[violation as PasswordPolicyViolation] ?? violation)
    .join(', ');
}

function fieldMessage(field: string, messages: readonly string[]): string {
  if (field === 'photo') {
    return messages
      .map((message) => PHOTO_VIOLATIONS[message as ProfilePhotoViolation] ?? message)
      .join(', ');
  }
  if (field === 'newPassword') return `Contraseña: ${passwordViolationText(messages)}`;
  return `${field}: ${messages.join(', ')}`;
}

export function isApiError(error: unknown): error is ApiClientError {
  return error instanceof ApiClientError;
}

export function isSessionFailure(error: unknown): boolean {
  return isApiError(error) && error.kind === 'authentication';
}

/** User-facing text for a failed call; never exposes stack traces or raw bodies. */
export function describeApiError(error: unknown, fallback: string): string {
  if (!isApiError(error)) return fallback;
  switch (error.kind) {
    case 'validation': {
      const fields = Object.entries(error.fieldErrors);
      if (fields.length === 0) return error.message || fallback;
      return fields.map(([field, messages]) => fieldMessage(field, messages)).join(' · ');
    }
    case 'authentication':
      return 'Su sesión expiró o fue revocada. Inicie sesión nuevamente.';
    case 'authorization':
      if (error.code === 'SITE_ACCESS_DENIED') {
        return 'Esta acción requiere una sede activa en la que tenga el rol Administrador.';
      }
      if (error.code === 'CSRF_INVALID' || error.code === 'ORIGIN_DENIED') {
        return 'La verificación de seguridad de la solicitud falló. Recargue la página.';
      }
      return 'No tiene permisos para realizar esta acción sobre este registro.';
    case 'not_found':
      return 'El registro solicitado no existe o no está disponible.';
    case 'conflict':
      return error.message
        ? `Conflicto: ${error.message}`
        : 'La operación entra en conflicto con el estado actual del registro.';
    case 'payload_too_large':
      return 'El archivo supera el tamaño máximo permitido (5 MB).';
    case 'unsupported_media_type':
      return 'Formato no admitido. Use JPEG, PNG o WebP.';
    case 'rate_limited':
      return error.retryAfterSeconds !== null
        ? `Demasiados intentos. Intente nuevamente en ${error.retryAfterSeconds} segundos.`
        : 'Demasiados intentos. Intente más tarde.';
    case 'unavailable':
      return 'El servicio no está disponible temporalmente. Intente nuevamente.';
    default:
      return fallback;
  }
}
