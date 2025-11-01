// Check if we're in development mode
const isDevelopment =
  window.location.hostname === "localhost" ||
  window.location.hostname === "127.0.0.1";

// Base API URL - empty string means use relative path (works with webpack proxy and nginx)
export const API_BASE_URL = "";

// Full URL for debugging if proxy doesn't work
export const BOXLEO_API_URL = isDevelopment
  ? "http://localhost:5028" // Development: direct to backend
  : ""; // Production: relative path (nginx will proxy)
