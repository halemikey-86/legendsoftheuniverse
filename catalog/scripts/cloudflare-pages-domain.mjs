#!/usr/bin/env node
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

const configPath = path.join(
  os.homedir(),
  "AppData/Roaming/xdg.config/.wrangler/config/default.toml",
);
const toml = fs.readFileSync(configPath, "utf8");
const token = toml.match(/oauth_token = "([^"]+)"/)?.[1];
if (!token) throw new Error("Wrangler oauth token not found — run wrangler login");

const accountId = "40057b2bfbd5fe0cd86179110049fbb1";
const project = "willbound-catalog";
const domain = process.argv[2] ?? "willbound.haleappsllc.com";

async function cf(apiPath, method = "GET", body) {
  const res = await fetch(`https://api.cloudflare.com/client/v4${apiPath}`, {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const json = await res.json();
  if (!json.success) throw new Error(JSON.stringify(json.errors ?? json));
  return json.result;
}

const zones = await cf("/zones?name=haleappsllc.com");
console.log("Zone:", zones[0]?.name, zones[0]?.status ?? "not found");

try {
  const result = await cf(`/accounts/${accountId}/pages/projects/${project}/domains`, "POST", {
    name: domain,
  });
  console.log("Custom domain added:", result.name, result.status);
} catch (err) {
  console.error("Domain:", err.message);
}

const domains = await cf(`/accounts/${accountId}/pages/projects/${project}/domains`);
console.log(
  "Pages domains:",
  domains.map((d) => `${d.name} (${d.status})`).join(", "),
);
