// Configuración ESLint flat (ESM) para el monorepo Laboratorios Univalle.
// Lint NO type-aware: no usa project service ni tsconfig por paquete, por lo
// que no requiere builds previos ni acceso a bases de datos.
// Las reglas de React (hooks/refresh) solo aplican a apps/web.
import js from '@eslint/js';
import prettierConfig from 'eslint-config-prettier';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  {
    name: 'laboratorios-univalle/ignores',
    ignores: [
      '**/bin/**',
      '**/obj/**',
      '**/node_modules/**',
      '**/dist/**',
      '**/coverage/**',
      '**/Migrations/**',
      '**/wwwroot/**',
      '**/tmp/**',
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    name: 'laboratorios-univalle/base',
    files: ['**/*.{js,mjs,cjs,ts,tsx}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.node,
      },
    },
  },
  {
    name: 'laboratorios-univalle/react-web',
    files: ['apps/web/**/*.{js,jsx,ts,tsx}'],
    languageOptions: {
      globals: {
        ...globals.browser,
      },
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      'react-hooks/rules-of-hooks': 'error',
      'react-hooks/exhaustive-deps': 'warn',
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
    },
  },
  prettierConfig,
);
