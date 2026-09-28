import { useState, type ChangeEvent, type ReactElement } from 'react';
import type { ProfilePhotoUpload } from '@lu/api-client';
import { PROFILE_PHOTO, type ProfilePictureRef } from '@lu/contracts';
import { describeApiError } from './apiErrors';
import { photoFileProblem, photoUpload, usePhotoUrl } from './photo';

export function ProfilePhoto({
  picture,
  initials,
  label,
  load,
  upload,
  remove,
  onChanged,
}: {
  readonly picture: ProfilePictureRef | null;
  readonly initials: string;
  readonly label: string;
  readonly load?: () => Promise<Blob>;
  readonly upload?: (upload: ProfilePhotoUpload) => Promise<ProfilePictureRef>;
  readonly remove?: () => Promise<void>;
  readonly onChanged?: (picture: ProfilePictureRef | null) => void;
}): ReactElement {
  const url = usePhotoUrl(picture, load);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  async function choose(event: ChangeEvent<HTMLInputElement>): Promise<void> {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file || !upload) return;
    setError(null);
    setFeedback(null);
    const problem = photoFileProblem(file);
    if (problem !== null) {
      setError(problem);
      return;
    }
    setBusy(true);
    try {
      const ref = await upload(photoUpload(file));
      onChanged?.(ref);
      setFeedback('Foto de perfil actualizada.');
    } catch (caught) {
      setError(describeApiError(caught, 'No fue posible subir la foto de perfil.'));
    } finally {
      setBusy(false);
    }
  }

  async function clear(): Promise<void> {
    if (!remove || !window.confirm('¿Confirmas quitar la foto de perfil?')) return;
    setError(null);
    setFeedback(null);
    setBusy(true);
    try {
      await remove();
      onChanged?.(null);
      setFeedback('Foto de perfil eliminada.');
    } catch (caught) {
      setError(describeApiError(caught, 'No fue posible quitar la foto de perfil.'));
    } finally {
      setBusy(false);
    }
  }

  // Details.cshtml avatar: 150px photo (cache-busted by etag) or bg-info initials.
  return (
    <div className="text-center">
      {url ? (
        <img
          className="rounded-circle shadow user-detail-avatar"
          src={url}
          alt={`Foto de ${label}`}
        />
      ) : (
        <span
          className="rounded-circle bg-info text-white font-weight-bold shadow user-detail-avatar user-detail-initials"
          role="img"
          aria-label={`Iniciales de ${label}`}
        >
          {initials || 'US'}
        </span>
      )}
      {upload || (remove && picture) ? (
        <div className="photo-controls">
          {upload ? (
            <label className="btn btn-link btn-sm text-info font-weight-bold mb-0">
              <i className="fas fa-camera mr-1" aria-hidden="true" />
              {busy ? 'Procesando…' : picture ? 'Cambiar foto' : 'Subir foto'}
              <input
                type="file"
                className="sr-only"
                aria-label="Seleccionar foto de perfil"
                accept={PROFILE_PHOTO.mimeTypes.join(',')}
                disabled={busy}
                onChange={(event) => void choose(event)}
              />
            </label>
          ) : null}
          {remove && picture ? (
            <button
              type="button"
              className="btn btn-link btn-sm text-danger font-weight-bold"
              disabled={busy}
              onClick={() => void clear()}
            >
              <i className="fas fa-times mr-1" aria-hidden="true" /> Quitar foto
            </button>
          ) : null}
        </div>
      ) : null}
      {upload ? (
        <small className="text-muted d-block" style={{ fontSize: '0.7rem' }}>
          JPEG, PNG o WebP · máximo 5 MB
        </small>
      ) : null}
      {feedback ? (
        <div className="alert alert-success shadow-sm border-0 small mt-2 mb-0" role="status">
          {feedback}
        </div>
      ) : null}
      {error ? (
        <div className="alert alert-danger shadow-sm border-0 small mt-2 mb-0" role="alert">
          {error}
        </div>
      ) : null}
    </div>
  );
}
