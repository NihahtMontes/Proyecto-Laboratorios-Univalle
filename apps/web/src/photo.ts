import { useEffect, useRef, useState } from 'react';
import type { ProfilePhotoUpload } from '@lu/api-client';
import { PROFILE_PHOTO, type ProfilePhotoMimeType, type ProfilePictureRef } from '@lu/contracts';

/**
 * MIG-001 F5 profile photo: authorized byte routes only (never a public URL).
 * Client checks are UX; the server validates size, extension, MIME and signature.
 */
export function photoFileProblem(file: File): string | null {
  if (file.size === 0) return 'El archivo está vacío.';
  if (file.size > PROFILE_PHOTO.maxBytes) return 'La imagen supera el tamaño máximo de 5 MB.';
  if (!(PROFILE_PHOTO.mimeTypes as readonly string[]).includes(file.type)) {
    return 'Formato no admitido. Use JPEG, PNG o WebP.';
  }
  const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
  if (!(PROFILE_PHOTO.extensions as readonly string[]).includes(extension)) {
    return 'La extensión del archivo debe ser .jpg, .jpeg, .png o .webp.';
  }
  return null;
}

export function photoUpload(file: File): ProfilePhotoUpload {
  return { data: file, fileName: file.name, contentType: file.type as ProfilePhotoMimeType };
}

/** Loads the authorized photo bytes into an object URL; re-fetches when the etag changes. */
export function usePhotoUrl(
  picture: ProfilePictureRef | null,
  load: (() => Promise<Blob>) | undefined,
): string | null {
  const [url, setUrl] = useState<string | null>(null);
  const etag = picture?.etag ?? null;
  const loadRef = useRef(load);
  loadRef.current = load;
  useEffect(() => {
    const loader = loadRef.current;
    if (etag === null || loader === undefined || typeof URL.createObjectURL !== 'function') {
      setUrl(null);
      return;
    }
    let cancelled = false;
    let created: string | null = null;
    void loader()
      .then((blob) => {
        if (cancelled) return;
        created = URL.createObjectURL(blob);
        setUrl(created);
      })
      .catch(() => {
        if (!cancelled) setUrl(null);
      });
    return () => {
      cancelled = true;
      if (created !== null) URL.revokeObjectURL(created);
    };
  }, [etag]);
  return url;
}
