import { PROFILE_PHOTO_MAX_BYTES, PROFILE_PHOTO_MIME } from './profile-photo.validator.js';

/** Minimal structural view of the Fastify instance (no fastify type dependency). */
export interface ContentTypeParserHost {
  hasContentTypeParser(contentType: string): boolean;
  addContentTypeParser(
    contentType: string,
    options: { parseAs: 'buffer'; bodyLimit: number },
    parser: (
      request: unknown,
      body: Buffer,
      done: (error: Error | null, body?: Buffer) => void,
    ) => void,
  ): void;
}

/**
 * Accepts raw JPEG/PNG/WEBP bodies up to 5 MiB (F1 §14) as Buffers. Every other
 * content type keeps Fastify's defaults (JSON with the normal body limit).
 */
export function registerProfilePhotoBodyParser(host: ContentTypeParserHost): void {
  for (const mime of Object.values(PROFILE_PHOTO_MIME)) {
    if (host.hasContentTypeParser(mime)) continue;
    host.addContentTypeParser(
      mime,
      { parseAs: 'buffer', bodyLimit: PROFILE_PHOTO_MAX_BYTES },
      (_request, body, done) => done(null, body),
    );
  }
}
