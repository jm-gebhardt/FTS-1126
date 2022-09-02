module.exports = {
  env: { node: true },
  extends: [
    'eslint:recommended', // this maybe causes errors in defineEmits<{}>() ???
    'plugin:@typescript-eslint/recommended',
    'plugin:@typescript-eslint/eslint-recommended',
    'plugin:vue/vue3-recommended',
    'plugin:prettier/recommended'
  ],
  parser: 'vue-eslint-parser',
  parserOptions: {
    parser: '@typescript-eslint/parser',
    sourceType: 'module'
  },
  plugins: ['prettier'],
  rules: {
    '@typescript-eslint/explicit-function-return-type': 'warn',
    'prettier/prettier': 'error',
    'vue/script-setup-uses-vars': 'error'
  }
}
