module.exports = {
  root: true,
  parser: '@typescript-eslint/parser',
  parserOptions: {
    ecmaVersion: 2020,
    sourceType: 'module'
  },
  plugins: ['@typescript-eslint'],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended'
  ],
  env: {
    node: true,
    es2020: true,
    jest: true
  },
  rules: {
    '@typescript-eslint/no-explicit-any': 'off',
    '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
    '@typescript-eslint/explicit-function-return-type': 'off',
    '@typescript-eslint/no-non-null-assertion': 'warn',
    'no-console': 'off',
    'prefer-const': 'error',
    'no-var': 'error',
    // Downgraded to warnings until pre-existing implementation issues are cleaned up.
    // TODO(Agent 5): fix lexical declarations in case blocks and require() imports.
    'no-case-declarations': 'warn',
    '@typescript-eslint/no-var-requires': 'warn'
  },
  ignorePatterns: ['dist/', 'coverage/', 'node_modules/', '*.js', 'tests/']
};
