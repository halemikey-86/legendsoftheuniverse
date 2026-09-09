import fs from "node:fs";
import os from "node:os";
import path from "node:path";

const toml = fs.readFileSync(
  path.join(os.homedir(), "AppData/Roaming/xdg.config/.wrangler/config/default.toml"),
  "utf8",
);
const token = toml.match(/oauth_token = "([^"]+)"/)?.[1];
const res = await fetch("https://api.cloudflare.com/client/v4/zones?per_page=50", {
  headers: { Authorization: `Bearer ${token}` },
});
const json = await res.json();
for (const z of json.result ?? []) console.log(z.name, z.status, z.id);
