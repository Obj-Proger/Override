// SQL tasks of the site: PostgreSQL in the browser through PGlite.
// One database serves all runs. Each run happens in a transaction that is rolled back, so tasks never see
// each other's tables and every run starts from the task's setup.
// Built into src/Override.Web/wwwroot/lib/pglite/sql.js with `npm run build`; the site loads it on demand.

import { PGlite } from "@electric-sql/pglite";

class Rollback extends Error {
    constructor(result) {
        super("rollback");
        this.result = result;
    }
}

let database;

// The file system image is passed in when it was copied as pglite-fs.bin; otherwise PGlite loads its
// files by itself from next to this module.
function open() {
    database ??= (async () => {
        const image = await fetch(new URL("./pglite-fs.bin", import.meta.url));
        return image.ok ? PGlite.create({ fsBundle: await image.blob() }) : PGlite.create();
    })().catch((error) => {
        database = undefined;
        throw error;
    });

    return database;
}

/** Values as text, so the site compares them the same way whatever their type. */
function show(value) {
    if (value === null || value === undefined)
        return "NULL";

    if (value instanceof Date)
        return value.toISOString();

    if (typeof value === "object")
        return JSON.stringify(value);

    return String(value);
}

/**
 * Runs the setup and then the query, and returns the result of the last statement of the query:
 * { ok: true, columns, rows } with rows as arrays of text, or { ok: false, error }.
 */
export async function run(setup, query) {
    const db = await open();

    try {
        await db.transaction(async (tx) => {
            if (setup)
                await tx.exec(setup);

            const results = await tx.exec(query);
            const last = results.length > 0 ? results[results.length - 1] : { fields: [], rows: [] };
            const columns = last.fields.map((field) => field.name);
            const rows = last.rows.map((row) => columns.map((column) => show(row[column])));
            throw new Rollback({ ok: true, error: null, columns, rows });
        });
    } catch (error) {
        if (error instanceof Rollback)
            return error.result;

        return { ok: false, error: error?.message ?? String(error), columns: [], rows: [] };
    }

    return { ok: false, error: "The query was not run.", columns: [], rows: [] };
}
