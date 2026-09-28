/**
 * Profile picture HTTP adapter (F1 §14). Uploads are the raw image bytes with
 * `Content-Type: image/jpeg|image/png|image/webp` and the original file name in
 * the `X-Photo-Filename` header (URI-encoded), used only for the extension
 * check. Pictures are served only through these authorized routes.
 */
import {
  Controller,
  Delete,
  Get,
  HttpCode,
  Param,
  Put,
  Req,
  Res,
  UseFilters,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import { AuthSecurityGuard, SkipCsrf } from '../../auth/auth.guard.js';
import {
  AuthAccessDeniedException,
  AuthNotFoundException,
  AuthUnauthorizedException,
  AuthValidationException,
} from '../../auth/auth.exceptions.js';
import type { FastifyReply, FastifyRequest } from '../../auth/auth.fastify.js';
import { AuthHeadersInterceptor } from '../../auth/auth.interceptor.js';
import { CoreExceptionFilter } from '../../core/core.filter.js';
import { ApiCorrelationGuard, GlobalSessionGuard } from '../../core/core.guard.js';
import type { RequestIdentity } from '../../identity/identity.contracts.js';
import {
  UserAuthorizationException,
  UserNotFoundException,
  UserValidationException,
} from '../user.service.js';
import type { ProfilePictureRef } from '../user.types.js';
import { ProfilePhotoService } from './profile-photo.service.js';

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function identityOf(request: FastifyRequest): RequestIdentity {
  if (request.identity === undefined) throw new AuthUnauthorizedException();
  return request.identity;
}

function userIdParam(value: string): string {
  if (!UUID.test(value)) {
    throw new AuthValidationException('id must be a canonical UUID.', {
      id: ['must be a canonical UUID'],
    });
  }
  return value.toLowerCase();
}

function uploadOf(request: FastifyRequest): {
  fileName: string;
  declaredMimeType: string;
  bytes: Buffer;
} {
  if (!Buffer.isBuffer(request.body)) {
    throw new AuthValidationException('The request body must be the image bytes.', {
      photo: ['unsupported_media_type'],
    });
  }
  const rawName = request.headers['x-photo-filename'];
  let fileName: string;
  try {
    fileName = typeof rawName === 'string' ? decodeURIComponent(rawName) : '';
  } catch {
    fileName = '';
  }
  const contentType = request.headers['content-type'];
  return {
    fileName,
    declaredMimeType: typeof contentType === 'string' ? contentType : '',
    bytes: request.body,
  };
}

async function mapErrors<T>(work: () => Promise<T>): Promise<T> {
  try {
    return await work();
  } catch (error) {
    if (error instanceof UserValidationException) {
      throw new AuthValidationException(error.message, error.fieldErrors);
    }
    if (error instanceof UserNotFoundException) throw new AuthNotFoundException(error.message);
    if (error instanceof UserAuthorizationException) throw new AuthAccessDeniedException();
    throw error;
  }
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, GlobalSessionGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class ProfilePhotoController {
  constructor(private readonly photos: ProfilePhotoService) {}

  @Get('users/:id/photo')
  @SkipCsrf()
  async readUser(
    @Req() request: FastifyRequest,
    @Res({ passthrough: true }) reply: FastifyReply,
    @Param('id') id: string,
  ): Promise<Buffer> {
    return this.send(
      reply,
      await mapErrors(() => this.photos.read(identityOf(request), userIdParam(id))),
    );
  }

  @Put('users/:id/photo')
  async replaceUser(
    @Req() request: FastifyRequest,
    @Param('id') id: string,
  ): Promise<{ success: true; data: ProfilePictureRef }> {
    const actor = identityOf(request);
    const data = await mapErrors(() =>
      this.photos.replace(actor, actor.correlationId, userIdParam(id), uploadOf(request)),
    );
    return { success: true, data };
  }

  @Delete('users/:id/photo')
  @HttpCode(204)
  async removeUser(@Req() request: FastifyRequest, @Param('id') id: string): Promise<void> {
    const actor = identityOf(request);
    await mapErrors(() => this.photos.remove(actor, actor.correlationId, userIdParam(id)));
  }

  @Get('profile/photo')
  @SkipCsrf()
  async readOwn(
    @Req() request: FastifyRequest,
    @Res({ passthrough: true }) reply: FastifyReply,
  ): Promise<Buffer> {
    const actor = identityOf(request);
    return this.send(reply, await mapErrors(() => this.photos.read(actor, actor.userId)));
  }

  @Put('profile/photo')
  async replaceOwn(
    @Req() request: FastifyRequest,
  ): Promise<{ success: true; data: ProfilePictureRef }> {
    const actor = identityOf(request);
    const data = await mapErrors(() =>
      this.photos.replace(actor, actor.correlationId, actor.userId, uploadOf(request)),
    );
    return { success: true, data };
  }

  @Delete('profile/photo')
  @HttpCode(204)
  async removeOwn(@Req() request: FastifyRequest): Promise<void> {
    const actor = identityOf(request);
    await mapErrors(() => this.photos.remove(actor, actor.correlationId, actor.userId));
  }

  private send(
    reply: FastifyReply,
    content: { bytes: Buffer; contentType: string; etag: string },
  ): Buffer {
    reply.header('Content-Type', content.contentType);
    reply.header('ETag', content.etag);
    reply.header('Content-Disposition', 'inline; filename="profile-picture"');
    reply.header('Content-Security-Policy', "default-src 'none'; sandbox");
    return content.bytes;
  }
}
