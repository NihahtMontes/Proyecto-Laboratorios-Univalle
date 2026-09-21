// Config Jest ESM nativo (NestJS 12 publica ESM puro; Jest no puede requirearlo).
// Los tests se ejecutan via `node --experimental-vm-modules node_modules/jest/bin/jest.js`
// (ver package.json scripts.test/smoke), requisito de Jest 30 para ESM en Node 24.
export default {
  testEnvironment: 'node',
  roots: ['<rootDir>/test'],
  testMatch: ['**/*.e2e-spec.ts'],
  // ts-jest compila .ts como ESM (useESM) y Jest los trata como módulos ESM.
  extensionsToTreatAsEsm: ['.ts'],
  transform: {
    '^.+\\.ts$': ['ts-jest', { useESM: true }],
  },
  moduleNameMapper: {
    '^@lu/contracts$': '<rootDir>/../../packages/contracts/src/index.ts',
    // Imports relativos con extension .js (convencion NodeNext/ESM) -> resuelven al .ts.
    '^(\\.{1,2}/.*)\\.js$': '$1',
  },
  moduleFileExtensions: ['ts', 'js', 'json'],
  collectCoverageFrom: ['src/**/*.ts'],
  coverageDirectory: './coverage',
};
