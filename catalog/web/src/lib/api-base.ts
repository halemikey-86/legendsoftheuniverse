const PRODUCTION_API = "https://willbound-catalog-api.fly.dev";

/** Resolve catalog API origin — env at build time, with sane fallbacks for hosted UI. */
export function resolveApiBase(): string {
  const env = (import.meta.env.VITE_API_BASE ?? "").trim().replace(/\/$/, "");
  if (env && !env.includes("localhost") && !env.includes("127.0.0.1")) {
    return env;
  }

  if (typeof window !== "undefined") {
    const { hostname, protocol } = window.location;
    if (protocol === "https:" || protocol === "http:") {
      if (
        hostname.endsWith(".pages.dev") ||
        hostname === "willbound.haleappsllc.com" ||
        hostname.endsWith(".willbound.haleappsllc.com")
      ) {
        return PRODUCTION_API;
      }
    }
  }

  return env;
}

export const API_BASE = resolveApiBase();
