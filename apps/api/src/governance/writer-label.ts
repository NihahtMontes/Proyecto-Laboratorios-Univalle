/**
 * Etiqueta interna de gobierno. Nunca se acepta desde DTOs, headers, payloads
 * ni se utiliza como mecanismo de autorización.
 */
export enum WriterLabel {
  LEGACY_SQLSERVER = 'LEGACY_SQLSERVER',
  NEST_SQLSERVER = 'NEST_SQLSERVER',
  NEST_POSTGRES = 'NEST_POSTGRES',
}
