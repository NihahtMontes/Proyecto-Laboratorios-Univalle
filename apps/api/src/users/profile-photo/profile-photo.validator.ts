/**
 * Profile picture validation (F1 §14): at most 5 MiB, JPG/JPEG/PNG/WEBP only,
 * and the file-name extension, the declared MIME type and the magic signature
 * must all agree. File names are never used as storage paths; any path
 * component is rejected outright.
 */
export const PROFILE_PHOTO_MAX_BYTES = 5 * 1024 * 1024;

export type ProfilePhotoFormat = 'jpeg' | 'png' | 'webp';

export const PROFILE_PHOTO_MIME: Readonly<Record<ProfilePhotoFormat, string>> = {
  jpeg: 'image/jpeg',
  png: 'image/png',
  webp: 'image/webp',
};

const EXTENSIONS: Readonly<Record<string, ProfilePhotoFormat>> = {
  jpg: 'jpeg',
  jpeg: 'jpeg',
  png: 'png',
  webp: 'webp',
};

export type ProfilePhotoViolation =
  | 'empty'
  | 'too_large'
  | 'invalid_file_name'
  | 'unsupported_extension'
  | 'unsupported_media_type'
  | 'signature_mismatch'
  | 'type_mismatch';

export interface ProfilePhotoUpload {
  readonly fileName: string;
  readonly declaredMimeType: string;
  readonly bytes: Buffer;
}

export type ProfilePhotoValidation =
  | { readonly ok: true; readonly format: ProfilePhotoFormat; readonly extension: string }
  | { readonly ok: false; readonly violation: ProfilePhotoViolation };

/** Detects the format from the file signature, independent of any client claim. */
export function detectProfilePhotoFormat(bytes: Buffer): ProfilePhotoFormat | null {
  if (bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) {
    return 'jpeg';
  }
  if (
    bytes.length >= 8 &&
    bytes.subarray(0, 8).equals(Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]))
  ) {
    return 'png';
  }
  if (
    bytes.length >= 12 &&
    bytes.subarray(0, 4).toString('latin1') === 'RIFF' &&
    bytes.subarray(8, 12).toString('latin1') === 'WEBP'
  ) {
    return 'webp';
  }
  return null;
}

export function validateProfilePhoto(upload: ProfilePhotoUpload): ProfilePhotoValidation {
  if (upload.bytes.length === 0) return { ok: false, violation: 'empty' };
  if (upload.bytes.length > PROFILE_PHOTO_MAX_BYTES) return { ok: false, violation: 'too_large' };

  const name = upload.fileName;
  if (
    name.length === 0 ||
    name.length > 255 ||
    // eslint-disable-next-line no-control-regex -- intentional control-character guard for the file-name extension check (rejects embedded NULs and other C0 control bytes)
    /[/\\\u0000-\u001f]/.test(name) ||
    name === '.' ||
    name === '..'
  ) {
    return { ok: false, violation: 'invalid_file_name' };
  }
  const dot = name.lastIndexOf('.');
  const extension = dot > 0 ? name.slice(dot + 1).toLowerCase() : '';
  const byExtension = EXTENSIONS[extension];
  if (byExtension === undefined) return { ok: false, violation: 'unsupported_extension' };

  const declared = upload.declaredMimeType.split(';')[0]?.trim().toLowerCase() ?? '';
  const byMime = (Object.keys(PROFILE_PHOTO_MIME) as ProfilePhotoFormat[]).find(
    (format) => PROFILE_PHOTO_MIME[format] === declared,
  );
  if (byMime === undefined) return { ok: false, violation: 'unsupported_media_type' };

  const bySignature = detectProfilePhotoFormat(upload.bytes);
  if (bySignature === null) return { ok: false, violation: 'signature_mismatch' };
  if (bySignature !== byMime || bySignature !== byExtension) {
    return { ok: false, violation: 'type_mismatch' };
  }
  return { ok: true, format: bySignature, extension };
}
