// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

// NgRx and other global state libraries are Rejected (frontend-standards.md):
// signals are the default state primitive for a two-user desktop app. Banned
// repo-wide here because a .NET architecture test cannot see npm packages.
const ngrxBan = {
  group: ['@ngrx', '@ngrx/*'],
  message:
    'NgRx is rejected (frontend-standards.md): signals are the default state primitive. Use a signal store.',
};

// PrimeNG is an implementation detail of the VetFlow UI Kit
// (ADR-0012, STD-FE-003): nothing outside the kit may import it.
const primengBan = {
  group: ['primeng', 'primeng/*', '@primeuix/*', 'primeicons/*'],
  message:
    'PrimeNG is an implementation detail of the VetFlow UI Kit (ADR-0012, STD-FE-003). Import a Vf* component instead.',
};

module.exports = defineConfig([
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: ['app', 'vf'],
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: ['app', 'vf'],
          style: 'kebab-case',
        },
      ],
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
      '@typescript-eslint/no-explicit-any': 'error',
      'no-console': 'error',
      // The NgRx ban applies everywhere, including the UI kit.
      'no-restricted-imports': ['error', { patterns: [ngrxBan] }],
    },
  },
  {
    // Outside the UI kit, PrimeNG is banned too (the NgRx ban is repeated here
    // because a later flat-config block replaces the rule rather than merging it).
    files: ['src/app/**/*.ts'],
    ignores: ['src/app/shared/ui-kit/**'],
    rules: {
      'no-restricted-imports': ['error', { patterns: [ngrxBan, primengBan] }],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
]);
