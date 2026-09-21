import bcrypt from 'bcryptjs';
import { Inject, Injectable, NotFoundException } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import type {
  CreateManagedUserInput,
  ManagedUserPage,
  ManagedUserQuery,
  ManagedUserRecord,
  ProfileRecord,
  SiteRequestContext,
  UpdateManagedUserInput,
  UpdateProfileInput,
} from '@lu/contracts';
import { GlobalRole, SiteRole } from '@lu/contracts';
import { AUTH_CONFIG, AUTH_PG_POOL } from '../auth/auth.constants.js';
import { AuthConfig } from '../auth/auth.config.js';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { UserRepository } from './user.repository.js';

function administrator(context: SiteRequestContext): void {
  if (context.siteRole !== SiteRole.Administrador && context.globalRole !== GlobalRole.SuperAdmin)
    throw new AuthForbiddenException('SITE_ACCESS_DENIED');
}
function translate(error: unknown): never {
  if (error instanceof CatalogMissingError) throw new NotFoundException();
  if (error instanceof CatalogConflictError)
    throw new AuthValidationException(
      error.message,
      error.field ? { [error.field]: [error.message] } : undefined,
    );
  throw error;
}

@Injectable()
export class UserService {
  constructor(
    @Inject(AUTH_PG_POOL) private readonly pool: IPgPool,
    @Inject(AUTH_CONFIG) private readonly config: AuthConfig,
    private readonly repository: UserRepository,
  ) {}
  async list(context: SiteRequestContext, query: ManagedUserQuery): Promise<ManagedUserPage> {
    administrator(context);
    try {
      return await this.repository.list(this.pool, context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }
  async find(context: SiteRequestContext, id: string): Promise<ManagedUserRecord> {
    administrator(context);
    try {
      const value = await this.repository.find(this.pool, context.siteId, id);
      if (value === null) throw new NotFoundException();
      return value;
    } catch (error) {
      return translate(error);
    }
  }
  async create(
    context: SiteRequestContext,
    input: CreateManagedUserInput,
  ): Promise<ManagedUserRecord> {
    administrator(context);
    try {
      this.config.validate();
      const hash = await bcrypt.hash(input.password, this.config.bcryptCost);
      return await this.repository.create(this.pool, context.siteId, randomUUID(), input, hash);
    } catch (error) {
      return translate(error);
    }
  }
  async update(
    context: SiteRequestContext,
    id: string,
    input: UpdateManagedUserInput,
  ): Promise<ManagedUserRecord> {
    administrator(context);
    if (id === context.userId && (input.status !== 'active' || input.siteRole !== context.siteRole))
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    try {
      this.config.validate();
      const hash = input.password?.trim()
        ? await bcrypt.hash(input.password, this.config.bcryptCost)
        : null;
      return await this.repository.update(this.pool, context.siteId, id, input, hash);
    } catch (error) {
      return translate(error);
    }
  }
  async disable(context: SiteRequestContext, id: string): Promise<void> {
    const value = await this.find(context, id);
    await this.update(context, id, {
      email: value.email,
      fullName: value.fullName,
      status: 'disabled',
      siteRole: value.siteRole,
    });
  }
  async profile(context: SiteRequestContext): Promise<ProfileRecord> {
    try {
      const value = await this.repository.profile(this.pool, context.userId, context.siteId);
      if (value === null) throw new NotFoundException();
      return value;
    } catch (error) {
      return translate(error);
    }
  }
  async updateProfile(
    context: SiteRequestContext,
    input: UpdateProfileInput,
  ): Promise<ProfileRecord> {
    try {
      return await this.repository.updateProfile(this.pool, context.userId, input);
    } catch (error) {
      return translate(error);
    }
  }
}
