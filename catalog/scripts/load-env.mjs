import dotenv from "dotenv";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");

// .env wins over stale shell vars (e.g. old ADMIN_TOKEN from a prior session).
dotenv.config({ path: path.join(root, ".env"), override: true });

export { root };
