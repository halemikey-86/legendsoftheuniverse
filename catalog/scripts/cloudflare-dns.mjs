#!/usr/bin/env node
/**
 * Create Cloudflare DNS records for WILLBOUND subdomains.
 *
 * Usage:
 *   CLOUDFLARE_API_TOKEN=xxx node scripts/cloudflare-dns.mjs \
 *     --zone haleappsllc.com \
 *     --willbound willbound-catalog \
 *     --api-host my-api.fly.dev
 */

const token = process.env.CLOUDFLARE_API_TOKEN;
if (!token) {
  console.error("CLOUDFLARE_API_TOKEN is required");
  process.exit(1);
}

const args = {};
for (let i = 2; i < process.argv.length; i += 2) {
  const key = process.argv[i];
  if (key?.startsWith("--")) args[key.slice(2)] = process.argv[i + 1];
}

const zoneName = args.zone ?? "haleappsllc.com";
const pagesProject = args.willbound ?? "willbound-catalog";
const apiHost = args["api-host"];

async function cf(path, init = {}) {
  const res = await fetch(`https://api.cloudflare.com/client/v4${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init.headers ?? {}),
    },
  });
  const body = await res.json();
  if (!body.success) {
    throw new Error(JSON.stringify(body.errors ?? body));
  }
  return body.result;
}

async function upsertRecord(zoneId, type, name, content, proxied = true) {
  const existing = await cf(
    `/zones/${zoneId}/dns_records?type=${type}&name=${encodeURIComponent(name)}`,
  );
  const payload = { type, name, content, proxied, ttl: 1 };
  if (existing.length > 0) {
    const updated = await cf(`/zones/${zoneId}/dns_records/${existing[0].id}`, {
      method: "PATCH",
      body: JSON.stringify(payload),
    });
    console.log(`Updated ${name} -> ${content}`);
    return updated;
  }
  const created = await cf(`/zones/${zoneId}/dns_records`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
  console.log(`Created ${name} -> ${content}`);
  return created;
}

const zones = await cf(`/zones?name=${zoneName}`);
const zone = zones[0];
if (!zone) throw new Error(`Zone not found: ${zoneName}`);

await upsertRecord(
  zone.id,
  "CNAME",
  `willbound.${zoneName}`,
  `${pagesProject}.pages.dev`,
);

if (apiHost && apiHost !== "REPLACE_WITH_API_HOSTNAME") {
  await upsertRecord(zone.id, "CNAME", `api.willbound.${zoneName}`, apiHost);
} else {
  console.log("Skip api.willbound — pass --api-host when API is deployed");
}

console.log("Done.");
