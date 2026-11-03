import "@testing-library/jest-dom/vitest";

// Tests run without Next.js loading .env.local; keep the API contract explicit
// for component tests that import the HTTP repository indirectly.
process.env.API_URL ??= "http://localhost:5000";
