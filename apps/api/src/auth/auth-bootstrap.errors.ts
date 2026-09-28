export class AuthBootstrapError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'AuthBootstrapError';
  }
}
