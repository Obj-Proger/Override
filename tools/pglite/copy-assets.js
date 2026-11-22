// Copies the files PGlite loads at run time (WebAssembly binaries, the file system image and other data)
// next to the bundle. The image is also saved as pglite-fs.bin: the development server does not serve
// .data files, so the site loads the image itself under that name and passes it to PGlite.

import { copyFileSync, existsSync, mkdirSync, readdirSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const source = join(here, "node_modules", "@electric-sql", "pglite", "dist");
const target = join(here, "..", "..", "src", "Override.Web", "wwwroot", "lib", "pglite");
const code = /\.(js|cjs|mjs|ts|cts|mts|map)$/;

mkdirSync(target, { recursive: true });

for (const file of readdirSync(source)) {
    if (statSync(join(source, file)).isFile() && !code.test(file)) {
        copyFileSync(join(source, file), join(target, file));
        console.log(`copied ${file}`);
    }
}

const image = join(source, "pglite.data");
if (existsSync(image)) {
    copyFileSync(image, join(target, "pglite-fs.bin"));
    console.log("copied pglite.data as pglite-fs.bin");
} else {
    console.log("pglite.data is not in this version of PGlite: it will load its files by itself.");
}
