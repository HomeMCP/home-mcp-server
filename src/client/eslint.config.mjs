// Adapted from the project-wide EslintFormatter (originally for Vue),
// replacing eslint-plugin-vue with @angular-eslint equivalents.
// Note: inline templates in .ts files are NOT linted here — all templates live in
// separate .html files, which are covered by the last config block below.

import tseslint from 'typescript-eslint';
import angular from '@angular-eslint/eslint-plugin';
import angularTemplate from '@angular-eslint/eslint-plugin-template';
import angularTemplateParser from '@angular-eslint/template-parser';
import stylistic from '@stylistic/eslint-plugin';
import unusedImports from 'eslint-plugin-unused-imports';
import simpleImportSort from 'eslint-plugin-simple-import-sort';

export default [
    // ── Global ignores ────────────────────────────────────────────────────────
    {
        ignores: [
            'node_modules/**',
            'dist/**',
            '.angular/**',
            '**/*.js',
            '**/*.d.ts',
        ],
    },

    // ── TypeScript component/service files ────────────────────────────────────
    {
        files: ['**/*.ts'],
        plugins: {
            '@typescript-eslint': tseslint.plugin,
            '@angular-eslint': angular,
            '@stylistic': stylistic,
            'unused-imports': unusedImports,
            'simple-import-sort': simpleImportSort,
        },
        languageOptions: {
            parser: tseslint.parser,
            sourceType: 'module',
        },
        rules: {
            // ── Imports ───────────────────────────────────────────────────────
            'unused-imports/no-unused-imports': 'warn',
            'simple-import-sort/imports': 'warn',
            'simple-import-sort/exports': 'warn',

            // ── Braces — Allman style, always required ────────────────────────
            'curly': ['warn', 'all'],
            '@stylistic/brace-style': ['warn', 'allman', { allowSingleLine: false }],

            // ── Indentation — 4 spaces ────────────────────────────────────────
            '@stylistic/indent': ['warn', 4, { SwitchCase: 1 }],

            // ── Semicolons ────────────────────────────────────────────────────
            '@stylistic/semi': ['warn', 'always'],

            // ── Quotes ────────────────────────────────────────────────────────
            '@stylistic/quotes': ['warn', 'single', { avoidEscape: true }],

            // ── Trailing commas ───────────────────────────────────────────────
            '@stylistic/comma-dangle': ['warn', 'always-multiline'],

            // ── Whitespace ────────────────────────────────────────────────────
            '@stylistic/eol-last': ['warn', 'always'],
            '@stylistic/no-multiple-empty-lines': ['warn', { max: 1 }],
            '@stylistic/no-trailing-spaces': 'warn',
            '@stylistic/object-curly-spacing': ['warn', 'always'],

            // ── TypeScript-specific ───────────────────────────────────────────
            '@typescript-eslint/no-explicit-any': 'warn',
            '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],

            // ── Angular conventions ───────────────────────────────────────────
            '@angular-eslint/prefer-on-push-component-change-detection': 'warn',
            '@angular-eslint/component-selector': [
                'warn',
                { type: 'element', prefix: 'app', style: 'kebab-case' },
            ],
            '@angular-eslint/directive-selector': [
                'warn',
                { type: 'attribute', prefix: 'app', style: 'camelCase' },
            ],
            '@angular-eslint/no-output-native': 'warn',
        },
    },

    // ── Angular HTML template files ───────────────────────────────────────────
    {
        files: ['**/*.html'],
        ignores: ['**/index.html'],
        plugins: {
            '@angular-eslint/template': angularTemplate,
        },
        languageOptions: {
            parser: angularTemplateParser,
        },
        rules: {
            '@angular-eslint/template/use-track-by-function': 'warn',
            '@angular-eslint/template/no-any': 'warn',
        },
    },
];
