module.exports = {
  extends: ["./react.js", "next/core-web-vitals"],
  rules: {
    // Next.js specific
    "@next/next/no-html-link-for-pages": "off",
    "@next/next/no-img-element": "warn",
    "@next/next/no-page-custom-font": "warn",
    
    // Performance
    "@next/next/no-sync-scripts": "error",
    "@next/next/no-css-tags": "error",
    
    // SEO
    "@next/next/next-script-for-ga": "warn",
  },
  overrides: [
    {
      files: ["**/*.config.js", "**/*.config.ts"],
      env: {
        node: true,
      },
    },
    {
      files: ["**/pages/api/**/*.ts", "**/app/api/**/*.ts"],
      rules: {
        "@typescript-eslint/no-explicit-any": "off",
      },
    },
  ],
};