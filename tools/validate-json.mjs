import { readFileSync } from "node:fs";
import { relative, resolve } from "node:path";
import { argv, cwd, exit } from "node:process";

const files = argv.slice(2);

if (files.length === 0) {
  console.error("Usage: node tools/validate-json.mjs <file> [file...]");
  exit(2);
}

let failures = 0;

for (const file of files) {
  const path = resolve(cwd(), file);
  try {
    JSON.parse(readFileSync(path, "utf8"));
    console.log(`OK ${relative(cwd(), path)}`);
  } catch (error) {
    failures++;
    console.error(`FAIL ${relative(cwd(), path)}: ${error.message}`);
  }
}

exit(failures === 0 ? 0 : 1);
